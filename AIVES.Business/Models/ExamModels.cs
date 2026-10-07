namespace AIVES.Business.Models;

// Giá trị lưu trong database (cột VARCHAR có CHECK constraint)
public static class ExamStatuses
{
    public const string Open = "Open";
    public const string Closed = "Closed";
}

public static class ParticipantStatuses
{
    public const string Scheduled = "Scheduled";
    public const string InProgress = "InProgress";
    public const string Completed = "Completed";
}

public static class TurnKinds
{
    public const string Main = "Main";
    public const string FollowUp = "FollowUp";
}

// Cấu hình AI sinh câu hỏi xoáy. Để trống ApiKey thì hệ thống dùng bộ luật dự phòng (không cần internet).
public class AiOptions
{
    public string? ApiKey { get; set; }
    public string Model { get; set; } = "deepseek-chat";
}

// Máy chủ VieNeu-TTS chạy cục bộ (API tương thích OpenAI). Không chạy thì phòng thi tự dùng giọng của trình duyệt.
public class TtsOptions
{
    public string BaseUrl { get; set; } = "http://localhost:8000";
    public string Model { get; set; } = "vieneu-v3-turbo";
    public string Voice { get; set; } = "Mai Anh";

    // Ứng dụng tự khởi động VieNeu-TTS (thư mục VieNeu-TTS trong dự án) khi chạy; đặt false nếu bạn tự bật máy chủ
    public bool AutoStart { get; set; } = true;
    public string ServerPath { get; set; } = "VieNeu-TTS";

    // true: chạy bằng card NVIDIA (uv sync --extra cuda, tải PyTorch khoảng vài GB). Mặc định CPU, vẫn đủ nhanh.
    public bool UseGpu { get; set; }
}
