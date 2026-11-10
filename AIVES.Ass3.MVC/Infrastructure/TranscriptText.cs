using System.Text;
using AIVES.Business.DTOs;
using AIVES.Business.Models;

namespace AIVES.Ass3.MVC.Infrastructure;

// TV5 - Ass3 - mini-task: chuyển biên bản thành văn bản thuần để tải về (.txt)
public static class TranscriptText
{
    public static string Build(TranscriptDto t)
    {
        var sb = new StringBuilder();
        sb.AppendLine("BIÊN BẢN PHỎNG VẤN - AIVES");
        sb.AppendLine($"Phiên thi : {t.SessionTitle} ({t.SubjectCode})");
        sb.AppendLine($"Thí sinh  : {t.StudentName} - {t.StudentCode}");
        sb.AppendLine($"Trạng thái: {StatusText(t.Status)}");
        sb.AppendLine($"Số lượt   : {t.Turns.Count}");
        sb.AppendLine(new string('=', 60));

        var mainNo = 0;
        foreach (var turn in t.Turns)
        {
            var followUp = turn.Kind == TurnKinds.FollowUp;
            if (!followUp) mainNo++;
            var indent = followUp ? "    " : "";

            sb.AppendLine();
            var label = followUp ? "[Hỏi xoáy]" : $"[Câu chính {mainNo}]";
            if (turn.TimedOut) label += " [Hết giờ]";
            sb.AppendLine($"{indent}{label} {turn.AskedAt.ToLocalTime():HH:mm:ss dd/MM/yyyy}");
            sb.AppendLine($"{indent}Hỏi: {turn.Content}");
            if (followUp && !string.IsNullOrWhiteSpace(turn.Reason))
                sb.AppendLine($"{indent}Lý do hỏi thêm: {turn.Reason}");

            var answer = turn.Answer is null ? "(chưa trả lời)"
                : string.IsNullOrWhiteSpace(turn.Answer) ? "(không trả lời)"
                : turn.Answer.Replace("\n", "\n" + indent + "     ");
            sb.AppendLine($"{indent}Đáp: {answer}");
        }
        return sb.ToString();
    }

    // Tên file chỉ dùng ký tự an toàn: bien-ban-SE180001-12.txt
    public static string FileName(TranscriptDto t)
    {
        var code = new string(t.StudentCode.Where(char.IsLetterOrDigit).ToArray());
        return $"bien-ban-{code}-{t.ParticipantId}.txt";
    }

    private static string StatusText(string status) => status switch
    {
        ParticipantStatuses.Scheduled => "Chưa thi",
        ParticipantStatuses.InProgress => "Đang thi",
        ParticipantStatuses.Completed => "Đã thi",
        _ => status
    };
}
