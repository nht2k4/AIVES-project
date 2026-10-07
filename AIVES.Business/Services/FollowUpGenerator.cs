using AIVES.Business.Interfaces;
using AIVES.Business.Models;

namespace AIVES.Business.Services;

// TV6 - Ass3 - việc 6.2. "Giám khảo AI": quyết định có hỏi xoáy không. Xem AIVES.Tests/Business/AiAndVoiceTests.cs
// (FollowUpGeneratorFallbackTests) và AIVES_PhanCong_Ass2_Ass3.md.
public class FollowUpGenerator : IFollowUpGenerator
{
    public FollowUpGenerator(AiOptions options)
    {
    }

    public Task<FollowUpDecision> DecideAsync(FollowUpContext context, CancellationToken ct = default) => throw new NotImplementedException();
}
