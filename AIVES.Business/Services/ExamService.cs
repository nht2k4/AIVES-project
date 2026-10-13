using AIVES.Business.Common;
using AIVES.Business.DTOs;
using AIVES.Business.Interfaces;
using AIVES.Business.Models;
using AIVES.DataAccess.Entities;
using AIVES.DataAccess.Repositories.Interfaces;

namespace AIVES.Business.Services;

// TV3 - Ass2 - việc 3.4. Xem AIVES.Tests/Business/ExamTests.cs (ExamServiceTests) và AIVES_PhanCong_Ass2_Ass3.md.
public class ExamService : IExamService
{
    private readonly IExamRepository _exams;
    private readonly IQuestionRepository _questions;
    private readonly IAccountRepository _accounts;
    private readonly ISubjectRepository _subjects;
    private readonly IAssignmentService _assignmentService;

    public ExamService(IExamRepository exams, IQuestionRepository questions, IAccountRepository accounts,
        ISubjectRepository subjects, IAssignmentService assignmentService)
    {
        _exams = exams;
        _questions = questions;
        _accounts = accounts;
        _subjects = subjects;
        _assignmentService = assignmentService;
    }

    // null = mọi môn; danh sách rỗng = không môn nào
    private async Task<List<int>?> AllowedSubjectIdsAsync(int currentUserId)
    {
        var user = await _accounts.GetByIdAsync(currentUserId);
        if (user is null || !user.IsActive) return new List<int>();

        if (user.Role == Roles.Admin) return null;
        if (user.Role == Roles.Lecturer)
            return (await _subjects.GetByLecturerAsync(currentUserId)).Select(s => s.Id).ToList();

        return new List<int>();
    }

    public async Task<List<ExamListItemDto>> GetListAsync(int currentUserId)
    {
        var ids = await AllowedSubjectIdsAsync(currentUserId);
        var exams = await _exams.GetListAsync(ids);

        return exams
            .Select(e => new ExamListItemDto(
                e.Id, e.Title, e.Subject.Code, e.Subject.Name,
                e.StartTime, e.ExamParticipants.Count, e.Status))
            .ToList();
    }

    public async Task<List<SubjectDto>> GetSubjectsForExamAsync(int currentUserId)
    {
        var user = await _accounts.GetByIdAsync(currentUserId);
        if (user is null || !user.IsActive) return new List<SubjectDto>();

        IEnumerable<Subject> subjects;
        if (user.Role == Roles.Admin)
            subjects = await _subjects.GetAllWithAssignmentsAsync();
        else if (user.Role == Roles.Lecturer)
            subjects = await _subjects.GetByLecturerAsync(currentUserId);
        else
            return new List<SubjectDto>();

        return subjects.Where(s => s.IsActive).OrderBy(s => s.Code).Select(s => s.ToDto()).ToList();
    }

    public async Task<ServiceResult<int>> CreateAsync(int currentUserId, CreateExamRequest request)
    {
        // 1. Tên phiên thi
        var title = request.Title?.Trim() ?? string.Empty;
        if (title.Length == 0)
            return ServiceResult<int>.Fail("Tên phiên thi không được để trống.");
        if (title.Length > 150)
            return ServiceResult<int>.Fail("Tên phiên thi tối đa 150 ký tự.");

        // 2. Giờ bắt đầu không ở quá khứ (cho lệch 1 phút)
        if (request.StartTime < DateTime.Now.AddMinutes(-1))
            return ServiceResult<int>.Fail("Giờ bắt đầu không được nằm trong quá khứ.");

        // 3. Các con số (giống CHECK trong database)
        if (request.SlotMinutes < 1 || request.SlotMinutes > 120)
            return ServiceResult<int>.Fail("Khung thời gian mỗi thí sinh phải từ 1 đến 120 phút.");
        if (request.MainQuestionCount < 1 || request.MainQuestionCount > 20)
            return ServiceResult<int>.Fail("Số câu hỏi chính phải từ 1 đến 20.");
        if (request.MaxFollowUps < 0 || request.MaxFollowUps > 40)
            return ServiceResult<int>.Fail("Tổng số câu đào sâu mỗi thí sinh phải từ 0 đến 40.");
        if (request.MaxFollowUpsPerQuestion < 0 || request.MaxFollowUpsPerQuestion > 5)
            return ServiceResult<int>.Fail("Số lượt hỏi xoáy mỗi câu phải từ 0 đến 5.");
        if (request.AnswerSeconds < 10 || request.AnswerSeconds > 600)
            return ServiceResult<int>.Fail("Thời gian trả lời mỗi câu phải từ 10 đến 600 giây.");

        // 4. Danh sách sinh viên
        var parsed = ParseStudents(request.StudentList);
        if (!parsed.Succeeded)
            return ServiceResult<int>.Fail(parsed.Error!);
        var students = parsed.Data!;

        // 5. Môn: tồn tại -> quyền -> đang mở
        var subject = await _subjects.GetByIdAsync(request.SubjectId);
        if (subject is null)
            return ServiceResult<int>.NotFound("Không tìm thấy môn học.");
        if (!await _assignmentService.CanAccessSubjectAsync(currentUserId, request.SubjectId))
            return ServiceResult<int>.Forbidden("Bạn chưa được phân công môn học này.");
        if (!subject.IsActive)
            return ServiceResult<int>.Fail($"Môn {subject.Code} đã ngừng hoạt động, không thể tạo phiên thi.");

        // 6. Ngân hàng câu hỏi đủ câu đang dùng
        var pool = (await _questions.GetBySubjectAsync(request.SubjectId, true)).Select(q => q.Id).ToList();
        if (pool.Count < request.MainQuestionCount)
            return ServiceResult<int>.Fail(
                $"Ngân hàng câu hỏi của môn {subject.Code} chỉ có {pool.Count} câu đang dùng, cần ít nhất {request.MainQuestionCount} câu.");

        var sets = QuestionAllocator.Allocate(pool, students.Count, request.MainQuestionCount, new Random());

        var session = new ExamSession
        {
            SubjectId = request.SubjectId,
            Title = title,
            StartTime = request.StartTime,
            SlotMinutes = request.SlotMinutes,
            MainQuestionCount = request.MainQuestionCount,
            MaxFollowUps = request.MaxFollowUps,
            MaxFollowUpsPerQuestion = request.MaxFollowUpsPerQuestion,
            AnswerSeconds = request.AnswerSeconds,
            Status = ExamStatuses.Open,
            CreatedBy = currentUserId,
            CreatedAt = DateTime.UtcNow
        };

        for (var i = 0; i < students.Count; i++)
        {
            var start = request.StartTime.AddMinutes(i * request.SlotMinutes);
            var participant = new ExamParticipant
            {
                StudentCode = students[i].Code,
                FullName = students[i].Name,
                SlotStart = start,
                SlotEnd = start.AddMinutes(request.SlotMinutes),
                Status = ParticipantStatuses.Scheduled
            };
            AddQuestionSet(participant, sets[i]);
            session.ExamParticipants.Add(participant);
        }

        await _exams.AddAsync(session);
        return ServiceResult<int>.Ok(session.Id);
    }

    public async Task<ServiceResult<ExamDetailDto>> GetDetailAsync(int currentUserId, int examId)
    {
        var exam = await _exams.GetDetailAsync(examId);
        if (exam is null)
            return ServiceResult<ExamDetailDto>.NotFound("Không tìm thấy phiên thi.");
        if (!await _assignmentService.CanAccessSubjectAsync(currentUserId, exam.SubjectId))
            return ServiceResult<ExamDetailDto>.Forbidden("Bạn chưa được phân công môn học của phiên thi này.");

        var participants = exam.ExamParticipants
            .OrderBy(p => p.SlotStart)
            .Select(p => new ParticipantDto(
                p.Id, p.StudentCode, p.FullName, p.SlotStart, p.SlotEnd, p.Status,
                p.ParticipantQuestions.OrderBy(q => q.OrderNo).Select(q => q.Question.Content).ToList()))
            .ToList();

        return ServiceResult<ExamDetailDto>.Ok(new ExamDetailDto(
            exam.Id, exam.SubjectId, exam.Subject.Code, exam.Subject.Name, exam.Title,
            exam.StartTime, exam.SlotMinutes, exam.MainQuestionCount,
            exam.MaxFollowUps, exam.MaxFollowUpsPerQuestion, exam.AnswerSeconds, exam.Status,
            participants));
    }

    public async Task<ServiceResult> SetOpenAsync(int currentUserId, int examId, bool open)
    {
        var exam = await _exams.GetDetailAsync(examId);
        if (exam is null)
            return ServiceResult.NotFound("Không tìm thấy phiên thi.");
        if (!await _assignmentService.CanAccessSubjectAsync(currentUserId, exam.SubjectId))
            return ServiceResult.Forbidden("Bạn chưa được phân công môn học của phiên thi này.");

        exam.Status = open ? ExamStatuses.Open : ExamStatuses.Closed;
        await _exams.SaveAsync();
        return ServiceResult.Ok();
    }

    public async Task<ServiceResult> ReallocateAsync(int currentUserId, int examId)
    {
        var exam = await _exams.GetDetailAsync(examId);
        if (exam is null)
            return ServiceResult.NotFound("Không tìm thấy phiên thi.");
        if (!await _assignmentService.CanAccessSubjectAsync(currentUserId, exam.SubjectId))
            return ServiceResult.Forbidden("Bạn chưa được phân công môn học của phiên thi này.");

        if (exam.ExamParticipants.Any(p => p.Status != ParticipantStatuses.Scheduled))
            return ServiceResult.Fail("Đã có thí sinh bắt đầu thi, không thể chọn lại bộ câu hỏi.");

        var pool = (await _questions.GetBySubjectAsync(exam.SubjectId, true)).Select(q => q.Id).ToList();
        if (pool.Count < exam.MainQuestionCount)
            return ServiceResult.Fail(
                $"Ngân hàng câu hỏi chỉ có {pool.Count} câu đang dùng, cần ít nhất {exam.MainQuestionCount} câu.");

        var people = exam.ExamParticipants.OrderBy(p => p.SlotStart).ToList();
        var sets = QuestionAllocator.Allocate(pool, people.Count, exam.MainQuestionCount, new Random());

        // Bộ cũ và bộ mới có thể trùng khóa (thí sinh, câu hỏi): xóa bộ cũ và lưu trước, rồi mới thêm bộ mới
        foreach (var p in people)
            p.ParticipantQuestions.Clear();
        await _exams.SaveAsync();

        for (var i = 0; i < people.Count; i++)
            AddQuestionSet(people[i], sets[i]);
        await _exams.SaveAsync();

        return ServiceResult.Ok();
    }

    private static void AddQuestionSet(ExamParticipant participant, int[] questionIds)
    {
        for (var i = 0; i < questionIds.Length; i++)
            participant.ParticipantQuestions.Add(new ParticipantQuestion { QuestionId = questionIds[i], OrderNo = i + 1 });
    }

    // Mỗi dòng một sinh viên; dấu ngăn đầu tiên gặp là ';' ',' tab hoặc khoảng trắng
    private static ServiceResult<List<(string Code, string Name)>> ParseStudents(string? text)
    {
        var result = new List<(string Code, string Name)>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var lines = (text ?? string.Empty).Split('\n');

        for (var i = 0; i < lines.Length; i++)
        {
            var line = lines[i].Trim();
            if (line.Length == 0) continue;

            var idx = line.IndexOfAny(new[] { ';', ',', '\t', ' ' });
            if (idx <= 0)
                return ServiceResult<List<(string, string)>>.Fail($"Dòng {i + 1}: thiếu họ tên. Mỗi dòng có dạng \"MSSV; Họ tên\".");

            var code = line[..idx];
            var name = line[idx..].TrimStart(' ', ';', ',', '\t').Trim();

            if (name.Length == 0)
                return ServiceResult<List<(string, string)>>.Fail($"Dòng {i + 1}: thiếu họ tên.");
            if (code.Length > 30)
                return ServiceResult<List<(string, string)>>.Fail($"Dòng {i + 1}: mã sinh viên tối đa 30 ký tự.");
            if (name.Length > 100)
                return ServiceResult<List<(string, string)>>.Fail($"Dòng {i + 1}: họ tên tối đa 100 ký tự.");
            if (!seen.Add(code))
                return ServiceResult<List<(string, string)>>.Fail($"Dòng {i + 1}: mã sinh viên {code} bị trùng trong danh sách.");

            result.Add((code, name));
        }

        if (result.Count == 0)
            return ServiceResult<List<(string, string)>>.Fail("Danh sách sinh viên không được để trống.");
        if (result.Count > 200)
            return ServiceResult<List<(string, string)>>.Fail("Danh sách sinh viên tối đa 200 người.");

        return ServiceResult<List<(string, string)>>.Ok(result);
    }
}