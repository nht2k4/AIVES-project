using AIVES.Business.Common;
using AIVES.Business.DTOs;
using AIVES.Business.Interfaces;
using AIVES.Business.Models;
using AIVES.DataAccess.Entities;
using AIVES.DataAccess.Repositories.Interfaces;

namespace AIVES.Business.Services;

// TV3 - Ass3 - việc 3.3. Máy trạng thái của một buổi phỏng vấn. Xem AIVES.Tests/Business/InterviewTests.cs và AIVES_PhanCong_Ass2_Ass3.md.

public class InterviewService : IInterviewService
{
    // Thời gian dành cho việc AI đọc câu hỏi (TTS): đồng hồ phía trình duyệt chỉ chạy sau khi đọc xong
    private const int ReadingGraceSeconds = 15;
    private const int MaxAnswerLength = 4000;
    private const int MaxRecordingBytes = 10 * 1024 * 1024; // khoảng 10 phút giọng nói Opus, dư cho một câu trả lời

    private readonly IInterviewRepository _interviews;
    private readonly IExamRepository _exams;
    private readonly IVoiceService _voices;
    private readonly IAccountRepository _accounts;
    private readonly IAssignmentService _assignmentService;
    private readonly ILanguageConfigService _language;
    private readonly IFollowUpGenerator _followUps;

    public InterviewService(IInterviewRepository interviews, IExamRepository exams, IAccountRepository accounts, IVoiceService voices, IAssignmentService assignmentService,
        ILanguageConfigService language, IFollowUpGenerator followUps)
    {
        _interviews = interviews;
        _exams = exams;
        _voices = voices;
        _accounts = accounts;
        _assignmentService = assignmentService;
        _language = language;
        _followUps = followUps;
    }

    public async Task<ServiceResult<Stream>> SpeakAsync(int currentUserId, int turnId)
    {
        var turn = await _interviews.GetTurnAsync(turnId);
        var p = turn is null ? null : await _interviews.GetParticipantAsync(turn.ParticipantId);
        if (turn is null || p is null)
            return ServiceResult<Stream>.NotFound("Không tìm thấy lượt hỏi.");
        if (!await IsCandidateAsync(currentUserId, p))
            return ServiceResult<Stream>.Forbidden("Chỉ chính sinh viên dự thi mới được làm bài. Giảng viên và quản trị viên chỉ theo dõi và xem biên bản.");

        // Chỉ đọc đúng nội dung câu hỏi đã lưu, không nhận văn bản tùy ý từ trình duyệt
        // Giọng đọc chọn theo môn (Language/Edit); chưa chọn thì dùng giọng mặc định
        var voice = (await _language.GetSpeechConfigAsync(p.Session.SubjectId)).Data?.TtsVoice;
        var audio = await _voices.OpenSpeechAsync(turn.Content, voice);
        return audio is null
            ? ServiceResult<Stream>.Fail("Máy chủ giọng đọc cục bộ chưa chạy.")
            : ServiceResult<Stream>.Ok(audio);
    }

    public async Task<List<MyInterviewDto>> GetMyInterviewsAsync(int currentUserId)
    {
        var account = await _accounts.GetByIdAsync(currentUserId);
        if (account is not { IsActive: true, Role: Roles.Student, StudentCode: not null }) return new();

        return (await _exams.GetParticipantsByStudentCodeAsync(account.StudentCode))
            .Select(p => new MyInterviewDto(p.Id, p.Session.Title, p.Session.Subject.Code, p.Session.Subject.Name,
                p.SlotStart, p.SlotEnd, p.Status, p.Session.Status == ExamStatuses.Open))
            .ToList();
    }

    public async Task<ServiceResult<InterviewStateDto>> StartAsync(int currentUserId, int participantId)
    {
        var p = await _interviews.GetParticipantAsync(participantId);
        if (p is null)
            return ServiceResult<InterviewStateDto>.NotFound("Không tìm thấy thí sinh.");
        if (!await IsCandidateAsync(currentUserId, p))
            return ServiceResult<InterviewStateDto>.Forbidden("Chỉ chính sinh viên dự thi mới được làm bài. Giảng viên và quản trị viên chỉ theo dõi và xem biên bản.");

        if (p.Status != ParticipantStatuses.Completed)
        {
            if (p.Session.Status != ExamStatuses.Open)
                return ServiceResult<InterviewStateDto>.Fail("Phiên thi đã đóng.");

            if (!await _interviews.HasTurnsAsync(p.Id))
            {
                var first = p.ParticipantQuestions.OrderBy(pq => pq.OrderNo).FirstOrDefault();
                if (first is null)
                    return ServiceResult<InterviewStateDto>.Fail("Thí sinh chưa được phân bổ câu hỏi.");

                p.Status = ParticipantStatuses.InProgress;
                _interviews.AddTurn(NewTurn(p.Id, first.Question, TurnKinds.Main, first.Question.Content, null));
                await _interviews.SaveAsync();
            }
        }

        return ServiceResult<InterviewStateDto>.Ok(await BuildStateAsync(p));
    }

    public async Task<ServiceResult<InterviewStateDto>> SubmitAnswerAsync(int currentUserId, int turnId, string? answer, bool timedOut)
    {
        var turn = await _interviews.GetTurnAsync(turnId);
        if (turn is null)
            return ServiceResult<InterviewStateDto>.NotFound("Không tìm thấy lượt hỏi.");

        var p = await _interviews.GetParticipantAsync(turn.ParticipantId);
        if (p is null)
            return ServiceResult<InterviewStateDto>.NotFound("Không tìm thấy thí sinh.");
        if (!await IsCandidateAsync(currentUserId, p))
            return ServiceResult<InterviewStateDto>.Forbidden("Chỉ chính sinh viên dự thi mới được làm bài. Giảng viên và quản trị viên chỉ theo dõi và xem biên bản.");
        if (p.Session.Status != ExamStatuses.Open)
            return ServiceResult<InterviewStateDto>.Fail("Phiên thi đã đóng.");
        if (turn.AnsweredAt is not null)
            return ServiceResult<InterviewStateDto>.Fail("Câu hỏi này đã được trả lời.");

        var session = p.Session;
        var now = DateTime.UtcNow;
        var speech = (await _language.GetSpeechConfigAsync(session.SubjectId)).Data;

        // Sửa thuật ngữ chuyên ngành mà STT hay nghe sai, theo từ điển giảng viên nhập cho môn
        var text = SttTerms.Apply((answer ?? string.Empty).Trim(), speech?.SttTerms);
        if (text.Length > MaxAnswerLength) text = text[..MaxAnswerLength];

        // Giới hạn thời gian được kiểm ở server, không chỉ dựa vào đồng hồ trình duyệt
        turn.Answer = text;
        turn.AnsweredAt = now;
        turn.TimedOut = timedOut || (now - turn.AskedAt).TotalSeconds > session.AnswerSeconds + ReadingGraceSeconds;

        var turns = await _interviews.GetTurnsAsync(p.Id);
        var sameQuestion = turns.Where(t => t.QuestionId == turn.QuestionId).ToList();
        var followUpsOnQuestion = sameQuestion.Count(t => t.Kind == TurnKinds.FollowUp);
        var followUpsTotal = turns.Count(t => t.Kind == TurnKinds.FollowUp);
        var added = false;

        if (text.Length > 0
            && followUpsOnQuestion < session.MaxFollowUpsPerQuestion
            && followUpsTotal < session.MaxFollowUps)
        {
            var locale = speech?.TtsLocale ?? "vi-VN";
            var exchanges = sameQuestion
                .Where(t => t.AnsweredAt is not null)
                .Select(t => (t.Content, t.Answer ?? string.Empty))
                .ToList();

            // Giữ nhịp vấn đáp: AI chậm hơn thời gian chờ của môn thì dùng luật dự phòng
            using var aiTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(speech?.AiTimeoutSeconds ?? 8));
            var decision = await _followUps.DecideAsync(
                new FollowUpContext(turn.Question.Content, turn.Question.ExpectedPoints, locale, exchanges, speech?.UseExternalAi ?? true),
                aiTimeout.Token);

            if (decision.Ask && !string.IsNullOrWhiteSpace(decision.Question))
            {
                _interviews.AddTurn(NewTurn(p.Id, turn.Question, TurnKinds.FollowUp, Truncate(decision.Question, 1000), decision.Reason));
                added = true;
            }
        }

        if (!added)
        {
            var asked = turns.Where(t => t.Kind == TurnKinds.Main).Select(t => t.QuestionId).ToHashSet();
            var next = p.ParticipantQuestions.OrderBy(pq => pq.OrderNo).FirstOrDefault(pq => !asked.Contains(pq.QuestionId));

            if (next is not null)
                _interviews.AddTurn(NewTurn(p.Id, next.Question, TurnKinds.Main, next.Question.Content, null));
            else
                p.Status = ParticipantStatuses.Completed;
        }

        await _interviews.SaveAsync();
        return ServiceResult<InterviewStateDto>.Ok(await BuildStateAsync(p));
    }

    public async Task<ServiceResult<TranscriptDto>> GetTranscriptAsync(int currentUserId, int participantId)
    {
        var p = await _interviews.GetParticipantAsync(participantId);
        if (p is null)
            return ServiceResult<TranscriptDto>.NotFound("Không tìm thấy thí sinh.");
        if (!await CanAccessAsync(currentUserId, p))
            return ServiceResult<TranscriptDto>.Forbidden("Bạn chưa được phân công môn học này.");

        var turns = await _interviews.GetTurnsAsync(p.Id);
        var recorded = (await _interviews.GetRecordedTurnIdsAsync(p.Id)).ToHashSet();
        return ServiceResult<TranscriptDto>.Ok(new TranscriptDto(
            p.Id, p.SessionId, p.Session.Title, p.Session.Subject.Code, p.StudentCode, p.FullName, p.Status,
            turns.Select(t => new TurnDto(t.Id, t.Kind, t.Content, t.Reason, t.Answer, t.TimedOut, t.AskedAt, recorded.Contains(t.Id))).ToList()));
    }

    // ---- Ghi âm câu trả lời (để giảng viên nghe lại, đối chiếu với văn bản STT)
    public async Task<ServiceResult> SaveRecordingAsync(int currentUserId, int turnId, byte[] audio, string? contentType)
    {
        var turn = await _interviews.GetTurnAsync(turnId);
        var p = turn is null ? null : await _interviews.GetParticipantAsync(turn.ParticipantId);
        if (turn is null || p is null)
            return ServiceResult.NotFound("Không tìm thấy lượt hỏi.");
        if (!await IsCandidateAsync(currentUserId, p))
            return ServiceResult.Forbidden("Chỉ chính sinh viên dự thi mới được gửi ghi âm câu trả lời.");
        if (turn.AnsweredAt is null)
            return ServiceResult.Fail("Chỉ lưu ghi âm sau khi đã nộp câu trả lời của lượt này.");
        if (audio is null || audio.Length == 0)
            return ServiceResult.Fail("Tệp ghi âm rỗng.");
        if (audio.Length > MaxRecordingBytes)
            return ServiceResult.Fail($"Tệp ghi âm tối đa {MaxRecordingBytes / 1024 / 1024} MB.");
        var type = (contentType ?? string.Empty).Trim();
        if (!type.StartsWith("audio/", StringComparison.OrdinalIgnoreCase) || type.Length > 100)
            return ServiceResult.Fail("Tệp ghi âm phải là âm thanh (audio/...).");
        // Mỗi lượt chỉ một bản ghi: không cho ghi đè, để bản gốc không bị thay sau khi đã nộp
        if (await _interviews.GetRecordingAsync(turnId) is not null)
            return ServiceResult.Fail("Lượt này đã có ghi âm.");

        _interviews.AddRecording(new TurnRecording
        {
            TurnId = turnId,
            ContentType = type,
            Audio = audio,
            SizeBytes = audio.Length,
            CreatedAt = DateTime.UtcNow
        });
        await _interviews.SaveAsync();
        return ServiceResult.Ok();
    }

    public async Task<ServiceResult<RecordingDto>> GetRecordingAsync(int currentUserId, int turnId)
    {
        var turn = await _interviews.GetTurnAsync(turnId);
        var p = turn is null ? null : await _interviews.GetParticipantAsync(turn.ParticipantId);
        if (turn is null || p is null)
            return ServiceResult<RecordingDto>.NotFound("Không tìm thấy lượt hỏi.");
        // Quyền riêng tư: chỉ Admin và giảng viên phụ trách môn (CanAccessSubject trả false cho sinh viên)
        if (!await _assignmentService.CanAccessSubjectAsync(currentUserId, p.Session.SubjectId))
            return ServiceResult<RecordingDto>.Forbidden("Chỉ giảng viên phụ trách môn và quản trị viên được nghe ghi âm.");

        var recording = await _interviews.GetRecordingAsync(turnId);
        return recording is null
            ? ServiceResult<RecordingDto>.NotFound("Lượt này không có ghi âm.")
            : ServiceResult<RecordingDto>.Ok(new RecordingDto(turnId, recording.ContentType, recording.Audio));
    }

    // Làm bài (bắt đầu, trả lời, nghe câu hỏi): chỉ chính sinh viên có MSSV trùng thí sinh. Giảng viên không thi thay.
    private async Task<bool> IsCandidateAsync(int userId, ExamParticipant p)
    {
        var account = await _accounts.GetByIdAsync(userId);
        return account is { IsActive: true, Role: Roles.Student }
            && string.Equals(account.StudentCode, p.StudentCode, StringComparison.OrdinalIgnoreCase);
    }

    // Xem biên bản: sinh viên của lượt thi đó, hoặc Admin/giảng viên theo phân công môn
    private async Task<bool> CanAccessAsync(int userId, ExamParticipant p)
    {
        var account = await _accounts.GetByIdAsync(userId);
        if (account is { IsActive: true, Role: Roles.Student })
            return string.Equals(account.StudentCode, p.StudentCode, StringComparison.OrdinalIgnoreCase);

        return await _assignmentService.CanAccessSubjectAsync(userId, p.Session.SubjectId);
    }

    private async Task<InterviewStateDto> BuildStateAsync(ExamParticipant p)
    {
        var turns = await _interviews.GetTurnsAsync(p.Id);
        var open = turns.LastOrDefault(t => t.AnsweredAt is null);
        var speech = (await _language.GetSpeechConfigAsync(p.Session.SubjectId)).Data;
        var seconds = p.Session.AnswerSeconds;

        var secondsLeft = open is null
            ? 0
            : (int)Math.Clamp(seconds + ReadingGraceSeconds - (DateTime.UtcNow - open.AskedAt).TotalSeconds, 0, seconds);

        return new InterviewStateDto(
            p.Id, p.StudentCode, p.FullName, p.Session.Subject.Code,
            Finished: p.Status == ParticipantStatuses.Completed,
            TurnId: open?.Id, Kind: open?.Kind, Content: open?.Content,
            MainIndex: turns.Count(t => t.Kind == TurnKinds.Main), MainTotal: p.ParticipantQuestions.Count,
            FollowUpIndex: open is null ? 0 : turns.Count(t => t.QuestionId == open.QuestionId && t.Kind == TurnKinds.FollowUp),
            FollowUpMax: p.Session.MaxFollowUpsPerQuestion,
            SecondsLeft: secondsLeft, AnswerSeconds: seconds,
            SttLocale: speech?.SttLocale ?? "vi-VN", TtsLocale: speech?.TtsLocale ?? "vi-VN", SessionId: p.SessionId);
    }

    private static InterviewTurn NewTurn(int participantId, Question question, string kind, string content, string? reason) => new()
    {
        ParticipantId = participantId,
        QuestionId = question.Id,
        Kind = kind,
        Content = content,
        Reason = reason is null ? null : Truncate(reason, 500),
        AskedAt = DateTime.UtcNow
    };

    private static string Truncate(string s, int max) => s.Length <= max ? s : s[..max];
}
