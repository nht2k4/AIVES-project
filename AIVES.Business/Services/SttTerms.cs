using System.Text.RegularExpressions;

namespace AIVES.Business.Services;

// Từ điển thuật ngữ của môn: STT tiếng Việt hay nghe sai thuật ngữ chuyên ngành ("ra dơ pây" thay vì "Razor Pages").
// Giảng viên tự nhập, mỗi dòng "cách nghe sai => cách viết đúng"; câu trả lời được sửa trước khi lưu và trước khi gửi cho AI.
internal static class SttTerms
{
    public const int MaxLength = 2000;
    private const string Arrow = "=>";

    // Trả về danh sách cặp, hoặc thông báo lỗi chỉ rõ dòng sai
    public static List<(string Heard, string Correct)> Parse(string? text, out string? error)
    {
        error = null;
        var pairs = new List<(string, string)>();
        var lines = (text ?? string.Empty).Replace("\r", "").Split('\n');

        for (var i = 0; i < lines.Length; i++)
        {
            var line = lines[i].Trim();
            if (line.Length == 0) continue;

            var at = line.IndexOf(Arrow, StringComparison.Ordinal);
            var heard = at < 0 ? "" : line[..at].Trim();
            var correct = at < 0 ? "" : line[(at + Arrow.Length)..].Trim();
            if (heard.Length == 0 || correct.Length == 0)
            {
                error = $"Dòng {i + 1} của từ điển thuật ngữ phải có dạng \"cách nghe sai => cách viết đúng\".";
                return new();
            }
            pairs.Add((heard, correct));
        }
        return pairs;
    }

    // Thay theo cả cụm từ (không thay giữa chừng một từ), không phân biệt hoa thường; cụm dài được thay trước
    public static string Apply(string answer, string? terms)
    {
        foreach (var (heard, correct) in Parse(terms, out _).OrderByDescending(p => p.Heard.Length))
            answer = Regex.Replace(answer, $@"(?<![\p{{L}}\p{{N}}]){Regex.Escape(heard)}(?![\p{{L}}\p{{N}}])", correct.Replace("$", "$$"),
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        return answer;
    }
}
