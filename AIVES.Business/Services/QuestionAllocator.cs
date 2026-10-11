namespace AIVES.Business.Services;

// TV5 - Ass2 - việc 5.1. Xem AIVES.Tests/Business/ExamTests.cs (QuestionAllocatorTests) và AIVES_PhanCong_Ass2_Ass3.md.
public static class QuestionAllocator
{
    // pool: mã các câu hỏi đang dùng. Trả về, theo thứ tự thi, bộ câu hỏi (mã câu) của từng sinh viên.
    public static List<int[]> Allocate(IReadOnlyList<int> pool, int students, int perStudent, Random rng)
    {
        ArgumentNullException.ThrowIfNull(pool);
        ArgumentNullException.ThrowIfNull(rng);
        ArgumentOutOfRangeException.ThrowIfNegative(students);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(perStudent);

        var result = new List<int[]>(students);
        if (students == 0) return result;

        var ids = pool.Distinct().ToArray();
        if (ids.Length < perStudent)
            throw new ArgumentException("Ngân hàng không đủ câu hỏi khác nhau cho một sinh viên.", nameof(pool));

        var usage = ids.ToDictionary(id => id, _ => 0);
        var previous = new HashSet<int>();
        for (var student = 0; student < students; student++)
        {
            // Generate one random tie-break per question, rather than calling RNG inside a comparator.
            var selected = ids.Select(id => new { Id = id, TieBreak = rng.NextDouble() })
                .OrderBy(q => previous.Contains(q.Id))
                .ThenBy(q => usage[q.Id])
                .ThenBy(q => q.TieBreak)
                .Take(perStudent)
                .Select(q => q.Id)
                .ToArray();

            foreach (var id in selected) usage[id]++;
            result.Add(selected);
            previous = selected.ToHashSet();
        }
        return result;
    }
}
