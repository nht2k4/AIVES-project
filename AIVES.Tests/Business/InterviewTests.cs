using AIVES.Business.Common;
using AIVES.Business.DTOs;
using AIVES.Business.Interfaces;
using AIVES.Business.Models;
using AIVES.Business.Services;
using AIVES.DataAccess.Entities;
using AIVES.Tests.Support;
using Xunit;

namespace AIVES.Tests.Business;

// ====== máy trạng thái của một buổi phỏng vấn ======

public class InterviewServiceTests
{
    private readonly TestDb _db = new();
    private readonly Account _lecturer;
    private readonly Account _student;
    private readonly Subject _subject;
    private readonly ExamSession _session;
    private readonly StubLanguageConfig _language = new();
    private readonly StubVoiceService _voices = new();
    private readonly ScriptedFollowUp _ai = new();

    public InterviewServiceTests()
    {
        _lecturer = _db.AddAccount(Roles.Lecturer);
        _subject = _db.AddSubject("PRN222");
        _db.Assign(_lecturer, _subject);
        _student = _db.AddAccount(Roles.Student, studentCode: "SE1");
        // 2 câu chính mỗi thí sinh, tối đa 4 câu đào sâu mỗi thí sinh, 2 lượt hỏi xoáy mỗi câu, 60 giây mỗi câu
        _session = _db.AddSession(_subject, _lecturer, mainQuestions: 2, maxFollowUps: 4, maxPerQuestion: 2, answerSeconds: 60, ExamStatuses.Open, "SE1", "SE2");
    }

    private ExamParticipant Participant => _session.ExamParticipants.First();
    private string FirstQuestion => Participant.ParticipantQuestions.Single(q => q.OrderNo == 1).Question.Content;
    private string SecondQuestion => Participant.ParticipantQuestions.Single(q => q.OrderNo == 2).Question.Content;

    private InterviewService Service() =>
        new(_db.InterviewRepo, _db.ExamRepo, _db.AccountRepo, _voices,
            new AssignmentService(_db.AccountRepo, _db.SubjectRepo, _db.LecturerSubjectRepo), _language, _ai);

    private async Task<InterviewStateDto> StartAsync()
    {
        var result = await Service().StartAsync(_student.Id, Participant.Id);
        Assert.True(result.Succeeded, result.Error);
        return result.Data!;
    }

    private async Task<ServiceResult<InterviewStateDto>> AnswerAsync(InterviewStateDto state, string answer, bool timedOut = false) =>
        await Service().SubmitAnswerAsync(_student.Id, state.TurnId!.Value, answer, timedOut);

    // ---- Bắt đầu
    [Fact]
    public async Task Start_FirstTime_CreatesTheFirstMainQuestion()
    {
        var state = await StartAsync();

        Assert.False(state.Finished);
        Assert.Equal(TurnKinds.Main, state.Kind);
        Assert.Equal(FirstQuestion, state.Content);
        Assert.Equal((1, 2), (state.MainIndex, state.MainTotal));
        Assert.Equal(0, state.FollowUpIndex);
        Assert.Equal(2, state.FollowUpMax);
        Assert.Equal(60, state.AnswerSeconds);
        Assert.Equal(60, state.SecondsLeft);
        Assert.Equal(("vi-VN", "vi-VN"), (state.SttLocale, state.TtsLocale));
        Assert.Equal(("SE1", "PRN222"), (state.StudentCode, state.SubjectCode));
        Assert.Equal(_session.Id, state.SessionId); // trang Phiên thi dùng mã này để cập nhật trực tiếp
        Assert.Equal(ParticipantStatuses.InProgress, Participant.Status);
        Assert.Single(_db.Turns);
    }

    [Fact]
    public async Task Start_Twice_ReturnsTheSameOpenTurn_WithoutCreatingAnother()
    {
        var first = await StartAsync();
        var second = await StartAsync();

        Assert.Equal(first.TurnId, second.TurnId);
        Assert.Single(_db.Turns);
    }

    [Fact]
    public async Task Start_Resuming_ShowsTheRemainingTime()
    {
        await StartAsync();
        _db.Turns.Single().AskedAt = DateTime.UtcNow.AddSeconds(-50); // đã 50 giây trôi qua

        var resumed = await StartAsync();

        Assert.InRange(resumed.SecondsLeft, 24, 26);
    }

    [Fact]
    public async Task Start_ClosedSession_IsRejected()
    {
        _session.Status = ExamStatuses.Closed;
        var result = await Service().StartAsync(_student.Id, Participant.Id);

        Assert.False(result.Succeeded);
        Assert.Empty(_db.Turns);
    }

    [Fact]
    public async Task Start_ParticipantWithoutQuestions_IsRejected()
    {
        var empty = _db.AddSession(_subject, _lecturer, mainQuestions: 0, 4, 2, 60, ExamStatuses.Open, "SE5");
        var se5 = _db.AddAccount(Roles.Student, studentCode: "SE5");

        var result = await Service().StartAsync(se5.Id, empty.ExamParticipants.First().Id);

        Assert.False(result.Succeeded);
    }

    [Fact]
    public async Task Start_UnknownParticipant_IsNotFound()
    {
        Assert.Equal(ServiceErrorType.NotFound, (await Service().StartAsync(_student.Id, 999)).ErrorType);
    }

    // ---- Quyền
    [Fact]
    public async Task Access_StudentOnlyForTheirOwnTurn()
    {
        var other = _db.AddAccount(Roles.Student, studentCode: "SE2");

        Assert.True((await Service().StartAsync(_student.Id, Participant.Id)).Succeeded);
        Assert.Equal(ServiceErrorType.Forbidden, (await Service().StartAsync(other.Id, Participant.Id)).ErrorType);
    }

    [Fact]
    public async Task Access_StudentCodeMatchIsCaseInsensitive()
    {
        var student = _db.AddAccount(Roles.Student, studentCode: "se1");
        Assert.True((await Service().StartAsync(student.Id, Participant.Id)).Succeeded);
    }

    // Giảng viên và admin KHÔNG thi thay sinh viên: không bắt đầu, không trả lời, không nghe câu hỏi; chỉ xem biên bản
    [Fact]
    public async Task Access_StaffCannotTakeTheExam_ButCanReadTheTranscript()
    {
        var admin = _db.AddAccount(Roles.Admin);
        var stranger = _db.AddAccount(Roles.Lecturer);

        Assert.Equal(ServiceErrorType.Forbidden, (await Service().StartAsync(_lecturer.Id, Participant.Id)).ErrorType);
        Assert.Equal(ServiceErrorType.Forbidden, (await Service().StartAsync(admin.Id, Participant.Id)).ErrorType);
        Assert.Empty(_db.Turns);

        var state = await StartAsync();
        Assert.Equal(ServiceErrorType.Forbidden, (await Service().SubmitAnswerAsync(_lecturer.Id, state.TurnId!.Value, "thi hộ", false)).ErrorType);
        Assert.Equal(ServiceErrorType.Forbidden, (await Service().SpeakAsync(admin.Id, state.TurnId!.Value)).ErrorType);
        Assert.Null(_db.Turns.Single().Answer);

        Assert.True((await Service().GetTranscriptAsync(_lecturer.Id, Participant.Id)).Succeeded);
        Assert.True((await Service().GetTranscriptAsync(admin.Id, Participant.Id)).Succeeded);
        Assert.Equal(ServiceErrorType.Forbidden, (await Service().GetTranscriptAsync(stranger.Id, Participant.Id)).ErrorType);
    }

    [Fact]
    public async Task Access_LockedStudent_IsForbidden()
    {
        _student.IsActive = false;
        Assert.Equal(ServiceErrorType.Forbidden, (await Service().StartAsync(_student.Id, Participant.Id)).ErrorType);
    }

    // ---- Thông số chỉnh tay của môn
    [Fact]
    public async Task Submit_FixesTheSubjectsTerms_BeforeSavingAndBeforeAskingTheAi()
    {
        _language.Config = new SpeechConfig("PRN222", "vi-VN", "vi-VN", SttTerms: "ra dơ pây => Razor Pages\nđi ai => DI");
        var state = await StartAsync();

        await AnswerAsync(state, "Em dùng RA DƠ PÂY với đi ai, không phải radơpây");

        Assert.Equal("Em dùng Razor Pages với DI, không phải radơpây", _db.Turns.First().Answer);
        Assert.Equal("Em dùng Razor Pages với DI, không phải radơpây", _ai.Calls.Single().Exchanges.Last().Answer);
    }

    [Fact]
    public async Task Submit_PassesTheSubjectsAiTimeoutAndPrivacySetting()
    {
        _language.Config = new SpeechConfig("PRN222", "vi-VN", "vi-VN", AiTimeoutSeconds: 3, UseExternalAi: false);
        var state = await StartAsync();

        await AnswerAsync(state, "Một câu trả lời");

        Assert.False(_ai.Calls.Single().AllowExternalAi);   // môn tắt AI bên ngoài
        Assert.True(_ai.Tokens.Single().CanBeCanceled);     // có hạn chờ AI, quá hạn thì dùng luật dự phòng
    }

    // ---- Nộp câu trả lời: sang câu chính kế tiếp, kết thúc
    [Fact]
    public async Task Submit_WithoutFollowUp_MovesToTheNextMainQuestion()
    {
        var state = await StartAsync();

        var next = (await AnswerAsync(state, "Đây là câu trả lời đầy đủ")).Data!;

        Assert.Equal(TurnKinds.Main, next.Kind);
        Assert.Equal(SecondQuestion, next.Content);
        Assert.Equal(2, next.MainIndex);
        var answered = _db.Turns.First();
        Assert.Equal("Đây là câu trả lời đầy đủ", answered.Answer);
        Assert.NotNull(answered.AnsweredAt);
        Assert.False(answered.TimedOut);
    }

    [Fact]
    public async Task Submit_LastAnswer_FinishesTheInterview()
    {
        var state = await StartAsync();
        state = (await AnswerAsync(state, "Trả lời câu 1")).Data!;

        var done = (await AnswerAsync(state, "Trả lời câu 2")).Data!;

        Assert.True(done.Finished);
        Assert.Null(done.TurnId);
        Assert.Equal(ParticipantStatuses.Completed, Participant.Status);
        Assert.Equal(2, _db.Turns.Count);
    }

    [Fact]
    public async Task Start_AfterCompleted_ReturnsFinished_AndCreatesNothing()
    {
        var state = await StartAsync();
        state = (await AnswerAsync(state, "Câu 1")).Data!;
        await AnswerAsync(state, "Câu 2");

        var again = await Service().StartAsync(_student.Id, Participant.Id);

        Assert.True(again.Data!.Finished);
        Assert.Equal(2, _db.Turns.Count);
    }

    // ---- Câu hỏi xoáy
    [Fact]
    public async Task Submit_WhenTheAiWantsToAsk_CreatesAFollowUpOnTheSameQuestion()
    {
        _ai.Decide = _ => new FollowUpDecision(true, "Cho ví dụ cụ thể?", "Câu trả lời còn mơ hồ");
        var state = await StartAsync();

        var next = (await AnswerAsync(state, "Em nghĩ là vậy")).Data!;

        Assert.Equal(TurnKinds.FollowUp, next.Kind);
        Assert.Equal("Cho ví dụ cụ thể?", next.Content);
        Assert.Equal(1, next.FollowUpIndex);
        Assert.Equal(1, next.MainIndex); // vẫn đang ở câu chính số 1
        var followUp = _db.Turns.Last();
        Assert.Equal(_db.Turns.First().QuestionId, followUp.QuestionId);
        Assert.Equal("Câu trả lời còn mơ hồ", followUp.Reason);
    }

    [Fact]
    public async Task Submit_PassesQuestionPointsLocaleAndWholeDialogueToTheAi()
    {
        _db.Questions.First(q => q.Content == FirstQuestion).ExpectedPoints = "Scoped;Singleton";
        _language.Config = new SpeechConfig("PRN222", "en-US", "en-US", null);
        _ai.Decide = c => c.Exchanges.Count < 2 ? new FollowUpDecision(true, "Giải thích thêm?", null) : new FollowUpDecision(false, null, null);
        var state = await StartAsync();
        state = (await AnswerAsync(state, "Trả lời lần một")).Data!;

        await AnswerAsync(state, "Trả lời lần hai");

        Assert.Equal(2, _ai.Calls.Count);
        Assert.Equal((FirstQuestion, "Scoped;Singleton", "en-US"), (_ai.Calls[0].Question, _ai.Calls[0].ExpectedPoints, _ai.Calls[0].Locale));
        Assert.Equal(new[] { (FirstQuestion, "Trả lời lần một") }, _ai.Calls[0].Exchanges);
        Assert.Equal(new[] { (FirstQuestion, "Trả lời lần một"), ("Giải thích thêm?", "Trả lời lần hai") }, _ai.Calls[1].Exchanges);
    }

    [Fact]
    public async Task Submit_StopsAskingAfterMaxFollowUpsPerQuestion_ThenMovesOn()
    {
        _ai.Decide = ScriptedFollowUp.Always().Decide; // AI muốn hỏi mãi, hệ thống phải chặn ở 2 lượt
        var state = await StartAsync();

        state = (await AnswerAsync(state, "a")).Data!;   // hỏi xoáy 1
        Assert.Equal((TurnKinds.FollowUp, 1), (state.Kind, state.FollowUpIndex));
        state = (await AnswerAsync(state, "b")).Data!;   // hỏi xoáy 2
        Assert.Equal((TurnKinds.FollowUp, 2), (state.Kind, state.FollowUpIndex));
        state = (await AnswerAsync(state, "c")).Data!;   // hết lượt: sang câu chính 2

        Assert.Equal((TurnKinds.Main, 2), (state.Kind, state.MainIndex));
        Assert.Equal(2, _ai.Calls.Count);                // AI không bị hỏi lần thứ ba cho câu này
        Assert.Equal(SecondQuestion, state.Content);
    }

    [Fact]
    public async Task Submit_StopsAskingAfterMaxFollowUpsPerStudent()
    {
        var limited = _db.AddSession(_subject, _lecturer, mainQuestions: 2, maxFollowUps: 1, maxPerQuestion: 5, 60, ExamStatuses.Open, "SE7");
        var participant = limited.ExamParticipants.First();
        var se7 = _db.AddAccount(Roles.Student, studentCode: "SE7");
        _ai.Decide = ScriptedFollowUp.Always().Decide;

        var state = (await Service().StartAsync(se7.Id, participant.Id)).Data!;
        state = (await Service().SubmitAnswerAsync(se7.Id, state.TurnId!.Value, "a", false)).Data!;
        Assert.Equal(TurnKinds.FollowUp, state.Kind);                                             // dùng hết 1 câu đào sâu của cả buổi thi
        state = (await Service().SubmitAnswerAsync(se7.Id, state.TurnId!.Value, "b", false)).Data!;
        Assert.Equal((TurnKinds.Main, 2), (state.Kind, state.MainIndex));                         // câu chính 2
        state = (await Service().SubmitAnswerAsync(se7.Id, state.TurnId!.Value, "c", false)).Data!;

        Assert.True(state.Finished);                                                              // không được hỏi xoáy thêm nữa
    }

    [Fact]
    public async Task Submit_EmptyAnswer_DoesNotCallTheAi_AndMovesOn()
    {
        _ai.Decide = ScriptedFollowUp.Always().Decide;
        var state = await StartAsync();

        var next = (await AnswerAsync(state, "   ")).Data!;

        Assert.Empty(_ai.Calls);
        Assert.Equal(TurnKinds.Main, next.Kind);
        Assert.Equal(2, next.MainIndex);
    }

    [Fact]
    public async Task Submit_AiSaysAskButGivesNoQuestion_IsTreatedAsNoFollowUp()
    {
        _ai.Decide = _ => new FollowUpDecision(true, "  ", "lý do");
        var state = await StartAsync();

        var next = (await AnswerAsync(state, "Trả lời")).Data!;

        Assert.Equal(TurnKinds.Main, next.Kind);
        Assert.Equal(2, next.MainIndex);
    }

    // ---- Giới hạn thời gian, dữ liệu vào
    [Fact]
    public async Task Submit_ClientSaysTimedOut_IsFlagged()
    {
        var state = await StartAsync();
        await AnswerAsync(state, "Chưa nói xong", timedOut: true);
        Assert.True(_db.Turns.First().TimedOut);
    }

    [Fact]
    public async Task Submit_AnswerArrivesAfterTheDeadline_IsFlaggedByTheServer_EvenIfTheClientLies()
    {
        var state = await StartAsync();
        _db.Turns.Single().AskedAt = DateTime.UtcNow.AddSeconds(-200); // hạn là 60 giây + 15 giây chờ AI đọc

        await AnswerAsync(state, "Trả lời quá muộn", timedOut: false);

        Assert.True(_db.Turns.First().TimedOut);
    }

    [Fact]
    public async Task Submit_AnswerWithinTheDeadline_IsNotFlagged()
    {
        var state = await StartAsync();
        _db.Turns.Single().AskedAt = DateTime.UtcNow.AddSeconds(-30);

        await AnswerAsync(state, "Kịp giờ");

        Assert.False(_db.Turns.First().TimedOut);
    }

    [Fact]
    public async Task Submit_VeryLongAnswer_IsCutAt4000Characters_AndTrimmed()
    {
        var state = await StartAsync();
        await AnswerAsync(state, "  " + new string('x', 5000) + "  ");
        Assert.Equal(4000, _db.Turns.First().Answer!.Length);
    }

    [Fact]
    public async Task Submit_SameTurnTwice_IsRejected()
    {
        var state = await StartAsync();
        await AnswerAsync(state, "Lần một");

        var second = await AnswerAsync(state, "Lần hai (nộp trùng)");

        Assert.False(second.Succeeded);
        Assert.Equal("Lần một", _db.Turns.First().Answer);
    }

    [Fact]
    public async Task Submit_ClosedSession_IsRejected()
    {
        var state = await StartAsync();
        _session.Status = ExamStatuses.Closed;

        Assert.False((await AnswerAsync(state, "Muộn")).Succeeded);
        Assert.Null(_db.Turns.First().Answer);
    }

    [Fact]
    public async Task Submit_UnknownTurn_IsNotFound_StrangerIsForbidden()
    {
        var state = await StartAsync();
        var stranger = _db.AddAccount(Roles.Lecturer);

        Assert.Equal(ServiceErrorType.NotFound, (await Service().SubmitAnswerAsync(_student.Id, 999, "x", false)).ErrorType);
        Assert.Equal(ServiceErrorType.Forbidden, (await Service().SubmitAnswerAsync(stranger.Id, state.TurnId!.Value, "x", false)).ErrorType);
    }

    // ---- Phần của sinh viên, biên bản, giọng đọc
    [Fact]
    public async Task GetMyInterviews_ListsOnlyTheStudentsOwnTurns()
    {
        var mine = await Service().GetMyInterviewsAsync(_student.Id);

        var item = Assert.Single(mine);
        Assert.Equal(Participant.Id, item.ParticipantId);
        Assert.Equal(("Phiên thi mẫu", "PRN222", ParticipantStatuses.Scheduled, true), (item.SessionTitle, item.SubjectCode, item.Status, item.SessionOpen));

        _session.Status = ExamStatuses.Closed;
        Assert.False((await Service().GetMyInterviewsAsync(_student.Id)).Single().SessionOpen);
    }

    [Fact]
    public async Task GetMyInterviews_IsEmptyForNonStudents_LockedStudents_AndStudentsWithoutExams()
    {
        var nobody = _db.AddAccount(Roles.Student, studentCode: "SE404");
        Assert.Empty(await Service().GetMyInterviewsAsync(_lecturer.Id));
        Assert.Empty(await Service().GetMyInterviewsAsync(nobody.Id));

        _student.IsActive = false;
        Assert.Empty(await Service().GetMyInterviewsAsync(_student.Id));
    }

    [Fact]
    public async Task Transcript_ShowsEveryTurnInOrder_ToAuthorizedUsersOnly()
    {
        _ai.Decide = c => c.Exchanges.Count == 1 ? new FollowUpDecision(true, "Hỏi xoáy?", "Thiếu ý") : new FollowUpDecision(false, null, null);
        var state = await StartAsync();
        state = (await AnswerAsync(state, "Trả lời 1")).Data!;
        await AnswerAsync(state, "Trả lời xoáy");
        var stranger = _db.AddAccount(Roles.Lecturer);

        var transcript = await Service().GetTranscriptAsync(_lecturer.Id, Participant.Id);

        Assert.True(transcript.Succeeded);
        Assert.Equal(("SE1", "PRN222"), (transcript.Data!.StudentCode, transcript.Data.SubjectCode));
        Assert.Equal(new[] { TurnKinds.Main, TurnKinds.FollowUp, TurnKinds.Main }, transcript.Data.Turns.Select(t => t.Kind));
        Assert.Equal("Trả lời 1", transcript.Data.Turns[0].Answer);
        Assert.Equal("Thiếu ý", transcript.Data.Turns[1].Reason);
        Assert.Null(transcript.Data.Turns[2].Answer); // câu chính 2 đang mở

        Assert.Equal(ServiceErrorType.Forbidden, (await Service().GetTranscriptAsync(stranger.Id, Participant.Id)).ErrorType);
        Assert.Equal(ServiceErrorType.NotFound, (await Service().GetTranscriptAsync(_lecturer.Id, 999)).ErrorType);
    }

    [Fact]
    public async Task Speak_UsesTheSubjectsVoice_ForTheStoredQuestionText()
    {
        _language.Config = new SpeechConfig("PRN222", "vi-VN", "vi-VN", "Hải Đăng");
        var state = await StartAsync();

        var result = await Service().SpeakAsync(_student.Id, state.TurnId!.Value);

        Assert.True(result.Succeeded);
        Assert.Equal("Hải Đăng", _voices.VoicesUsed.Last());
    }

    [Fact]
    public async Task Speak_WhenTtsIsDown_IsAValidationFailure_SoTheBrowserCanFallBack()
    {
        _voices.Available = false;
        var state = await StartAsync();

        var result = await Service().SpeakAsync(_student.Id, state.TurnId!.Value);

        Assert.Equal(ServiceErrorType.Validation, result.ErrorType);
    }

    [Fact]
    public async Task Speak_UnknownTurnIsNotFound_StrangerIsForbidden()
    {
        var state = await StartAsync();
        var stranger = _db.AddAccount(Roles.Lecturer);

        Assert.Equal(ServiceErrorType.NotFound, (await Service().SpeakAsync(_student.Id, 999)).ErrorType);
        Assert.Equal(ServiceErrorType.Forbidden, (await Service().SpeakAsync(stranger.Id, state.TurnId!.Value)).ErrorType);
    }
}
