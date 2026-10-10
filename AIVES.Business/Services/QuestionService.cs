using AIVES.Business.Common;
using AIVES.Business.DTOs;
using AIVES.Business.Interfaces;
using AIVES.DataAccess.Entities;
using AIVES.DataAccess.Repositories.Interfaces;

namespace AIVES.Business.Services;

// TV5 - Ass2 - việc 5.1. Xem AIVES.Tests/Business/ExamTests.cs (QuestionServiceTests).
public class QuestionService : IQuestionService
{
    private readonly IQuestionRepository _questions;
    private readonly IAssignmentService _assignmentService;

    public QuestionService(IQuestionRepository questions, IAssignmentService assignmentService)
    {
        _questions = questions;
        _assignmentService = assignmentService;
    }

    public async Task<ServiceResult<List<QuestionDto>>> GetBySubjectAsync(int currentUserId, int subjectId)
    {
        if (!await _assignmentService.CanAccessSubjectAsync(currentUserId, subjectId))
            return ServiceResult<List<QuestionDto>>.Forbidden("Bạn không có quyền truy cập môn học này.");

        var questions = await _questions.GetBySubjectAsync(subjectId, onlyActive: false);
        return ServiceResult<List<QuestionDto>>.Ok(questions
            .Select(q => new QuestionDto(q.Id, q.Content, q.ExpectedPoints, q.IsActive)).ToList());
    }

    public async Task<ServiceResult> AddAsync(int currentUserId, AddQuestionRequest request)
    {
        if (!await _assignmentService.CanAccessSubjectAsync(currentUserId, request.SubjectId))
            return ServiceResult.Forbidden("Bạn không có quyền truy cập môn học này.");

        var content = request.Content?.Trim();
        var points = request.ExpectedPoints?.Trim();
        if (string.IsNullOrEmpty(content)) return ServiceResult.Fail("Nhập nội dung câu hỏi.");
        if (content.Length > 1000) return ServiceResult.Fail("Nội dung câu hỏi không quá 1000 ký tự.");
        if (points?.Length > 1000) return ServiceResult.Fail("Ý chính trả lời không quá 1000 ký tự.");

        await _questions.AddAsync(new Question
        {
            SubjectId = request.SubjectId,
            Content = content,
            ExpectedPoints = string.IsNullOrEmpty(points) ? null : points,
            IsActive = true
        });
        return ServiceResult.Ok();
    }

    public async Task<ServiceResult> SetActiveAsync(int currentUserId, int questionId, bool isActive)
    {
        var question = await _questions.GetByIdAsync(questionId);
        if (question is null) return ServiceResult.NotFound("Không tìm thấy câu hỏi.");
        if (!await _assignmentService.CanAccessSubjectAsync(currentUserId, question.SubjectId))
            return ServiceResult.Forbidden("Bạn không có quyền truy cập môn học này.");

        question.IsActive = isActive;
        await _questions.UpdateAsync(question);
        return ServiceResult.Ok();
    }
}
