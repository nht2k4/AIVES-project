using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace AIVES.Ass3.MVC.Infrastructure;

// SignalR: báo cho các trình duyệt đang mở rằng dữ liệu vừa đổi, trang tự làm mới phần liên quan (không cần F5).
// Hub không có hàm nào vì chỉ server gửi xuống (qua IHubContext<LiveHub>); trình duyệt chỉ nghe sự kiện "changed".
[Authorize]
public class LiveHub : Hub
{
    public const string Path = "/hubs/live";
}

// Chủ đề (topic): trang nào có vùng data-live="<topic>" thì vùng đó được làm mới khi có sự kiện cùng topic
public static class LiveTopics
{
    public const string Accounts = "accounts";
    public const string Exams = "exams";
    public static string Exam(int id) => $"exam-{id}";
}

public static class LiveHubExtensions
{
    // Gửi cho mọi người đã đăng nhập; thông điệp không chứa dữ liệu riêng, trang tự tải lại theo quyền của chính người xem.
    // Cần giới hạn người nhận thì thêm Groups trong OnConnectedAsync.
    public static Task NotifyAsync(this IHubContext<LiveHub> hub, string message, params string[] topics) =>
        Task.WhenAll(topics.Select(t => hub.Clients.All.SendAsync("changed", t, message)));
}
