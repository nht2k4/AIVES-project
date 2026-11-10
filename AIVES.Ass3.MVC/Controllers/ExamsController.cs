using AIVES.Ass3.MVC.Infrastructure;
using AIVES.Ass3.MVC.ViewModels;
using AIVES.Business.Common;
using AIVES.Business.DTOs;
using AIVES.Business.Interfaces;
using AIVES.Business.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace AIVES.Ass3.MVC.Controllers;

[Authorize(Roles = Roles.Admin + "," + Roles.Lecturer)]
public class ExamsController : AppController
{
    private readonly IExamService _examService;
    private readonly IQuestionService _questionService;
    private readonly ISubjectService _subjectService;
    private readonly IHubContext<LiveHub> _live;

    public ExamsController(
        IExamService examService,
        IQuestionService questionService,
        ISubjectService subjectService,
        IHubContext<LiveHub> live)
    {
        _examService = examService;
        _questionService = questionService;
        _subjectService = subjectService;
        _live = live;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var exams = await _examService.GetListAsync(CurrentUserId);
        return View(new ExamListViewModel { Exams = exams });
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var subjects = await _examService.GetSubjectsForExamAsync(CurrentUserId);
        var vm = new ExamCreateViewModel
        {
            Subjects = subjects,
            StartTime = DateTime.Today.AddDays(1).AddHours(8)
        };
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ExamCreateViewModel model)
    {
        if (!ModelState.IsValid)
        {
            model.Subjects = await _examService.GetSubjectsForExamAsync(CurrentUserId);
            return View(model);
        }

        var request = new CreateExamRequest(
            model.SubjectId,
            model.Title.Trim(),
            model.StartTime,
            model.SlotMinutes,
            model.MainQuestionCount,
            model.MaxFollowUps,
            model.MaxFollowUpsPerQuestion,
            model.AnswerSeconds,
            model.StudentList);

        var result = await _examService.CreateAsync(CurrentUserId, request);
        if (!result.Succeeded)
        {
            var failure = MapFailure(result);
            if (failure != null) return failure;

            ModelState.AddModelError(string.Empty, result.Error!);
            model.Subjects = await _examService.GetSubjectsForExamAsync(CurrentUserId);
            return View(model);
        }

        FlashSuccess($"Đã tạo phiên thi '{model.Title}'.");
        await _live.NotifyAsync("Đã tạo phiên thi mới.", LiveTopics.Exams);
        return RedirectToAction(nameof(Details), new { id = result.Data });
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var result = await _examService.GetDetailAsync(CurrentUserId, id);
        if (!result.Succeeded)
        {
            return MapFailure(result) ?? NotFound();
        }

        return View(new ExamDetailViewModel { Exam = result.Data! });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetOpen(int id, bool open)
    {
        var result = await _examService.SetOpenAsync(CurrentUserId, id, open);
        if (!result.Succeeded)
        {
            var failure = MapFailure(result);
            if (failure != null) return failure;

            FlashError(result.Error ?? "Không thể đổi trạng thái phiên thi.");
            return RedirectToAction(nameof(Details), new { id });
        }

        var message = open ? "Đã mở lại phiên thi." : "Đã đóng phiên thi.";
        FlashSuccess(message);
        await _live.NotifyAsync(message, LiveTopics.Exams, LiveTopics.Exam(id));
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reallocate(int id)
    {
        var result = await _examService.ReallocateAsync(CurrentUserId, id);
        if (!result.Succeeded)
        {
            var failure = MapFailure(result);
            if (failure != null) return failure;

            FlashError(result.Error ?? "Không thể chọn lại bộ câu hỏi.");
            return RedirectToAction(nameof(Details), new { id });
        }

        FlashSuccess("Đã chọn lại bộ câu hỏi cho thí sinh.");
        await _live.NotifyAsync("Đã chọn lại bộ câu hỏi.", LiveTopics.Exam(id));
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpGet]
    public async Task<IActionResult> Questions(int subjectId)
    {
        var subjResult = await _subjectService.GetDetailAsync(subjectId);
        if (!subjResult.Succeeded)
        {
            return MapFailure(subjResult) ?? NotFound();
        }

        var questionsResult = await _questionService.GetBySubjectAsync(CurrentUserId, subjectId);
        if (!questionsResult.Succeeded)
        {
            return MapFailure(questionsResult) ?? Forbid();
        }

        var vm = new ExamQuestionsViewModel
        {
            Subject = subjResult.Data!.Subject,
            Questions = questionsResult.Data!,
            IsAdmin = User.IsInRole(Roles.Admin)
        };

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddQuestion(int subjectId, [Bind(Prefix = "NewQuestion")] ExamAddQuestionInput newQuestion)
    {
        if (!ModelState.IsValid)
        {
            var subjResult = await _subjectService.GetDetailAsync(subjectId);
            if (!subjResult.Succeeded) return MapFailure(subjResult) ?? NotFound();

            var questionsResult = await _questionService.GetBySubjectAsync(CurrentUserId, subjectId);
            if (!questionsResult.Succeeded) return MapFailure(questionsResult) ?? Forbid();

            var vm = new ExamQuestionsViewModel
            {
                Subject = subjResult.Data!.Subject,
                Questions = questionsResult.Data!,
                IsAdmin = User.IsInRole(Roles.Admin),
                NewQuestion = newQuestion
            };
            return View("Questions", vm);
        }

        var request = new AddQuestionRequest(
            subjectId,
            newQuestion.Content.Trim(),
            string.IsNullOrWhiteSpace(newQuestion.ExpectedPoints) ? null : newQuestion.ExpectedPoints.Trim());

        var result = await _questionService.AddAsync(CurrentUserId, request);
        if (!result.Succeeded)
        {
            var failure = MapFailure(result);
            if (failure != null) return failure;

            FlashError(result.Error ?? "Không thể thêm câu hỏi.");
            return RedirectToAction(nameof(Questions), new { subjectId });
        }

        FlashSuccess("Đã thêm câu hỏi mới vào ngân hàng.");
        return RedirectToAction(nameof(Questions), new { subjectId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetQuestionActive(int subjectId, int questionId, bool isActive)
    {
        var result = await _questionService.SetActiveAsync(CurrentUserId, questionId, isActive);
        if (!result.Succeeded)
        {
            var failure = MapFailure(result);
            if (failure != null) return failure;

            FlashError(result.Error ?? "Không thể cập nhật trạng thái câu hỏi.");
            return RedirectToAction(nameof(Questions), new { subjectId });
        }

        FlashSuccess(isActive ? "Đã bật câu hỏi." : "Đã tắt câu hỏi.");
        return RedirectToAction(nameof(Questions), new { subjectId });
    }
}
