using AIVES.Business.Common;
using AIVES.Business.DTOs;
using AIVES.Business.Interfaces;
using AIVES.DataAccess.Repositories.Interfaces;

namespace AIVES.Business.Services;

// TV3 - Ass3 - việc 3.3. Máy trạng thái của một buổi phỏng vấn. Xem AIVES.Tests/Business/InterviewTests.cs và AIVES_PhanCong_Ass2_Ass3.md.
public class InterviewService : IInterviewService
{
    public InterviewService(IInterviewRepository interviews, IExamRepository exams, IAccountRepository accounts, IVoiceService voices,
        IAssignmentService assignmentService, ILanguageConfigService language, IFollowUpGenerator followUps)
    {
    }

    public Task<ServiceResult<InterviewStateDto>> StartAsync(int currentUserId, int participantId) => throw new NotImplementedException();

    public Task<ServiceResult<InterviewStateDto>> SubmitAnswerAsync(int currentUserId, int turnId, string? answer, bool timedOut) =>
        throw new NotImplementedException();

    public Task<List<MyInterviewDto>> GetMyInterviewsAsync(int currentUserId) => throw new NotImplementedException();

    public Task<ServiceResult<Stream>> SpeakAsync(int currentUserId, int turnId) => throw new NotImplementedException();

    public Task<ServiceResult<TranscriptDto>> GetTranscriptAsync(int currentUserId, int participantId) => throw new NotImplementedException();
}
