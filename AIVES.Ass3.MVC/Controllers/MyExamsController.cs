using AIVES.Business.Interfaces;
using AIVES.Business.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AIVES.Ass3.MVC.Controllers;

// TV5 - Ass3 - việc 5.3: Bài thi của tôi (chỉ Sinh viên).
// Danh sách lọc theo MÃ SINH VIÊN của tài khoản đang đăng nhập (Service làm), không theo họ tên: tên có thể trùng, mã thì không.
[Authorize(Roles = Roles.Student)]
public class MyExamsController : AppController
{
    private readonly IInterviewService _interviews;

    public MyExamsController(IInterviewService interviews)
    {
        _interviews = interviews;
    }

    public async Task<IActionResult> Index() => View(await _interviews.GetMyInterviewsAsync(CurrentUserId));
}
