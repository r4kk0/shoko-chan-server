using System;
using Shoko.Server.Scheduling.ResourceGovernance;

namespace Shoko.Server.Providers.AniDB;

public static class AniDBResourceCooldownGuard
{
    public static readonly TimeSpan LongCooldownThreshold = TimeSpan.FromSeconds(30);

    public static void ThrowIfLongCooldown(SchedulerResourceKey resourceKey, TimeSpan retryAfter)
    {
        if (retryAfter > LongCooldownThreshold)
            throw new AniDBResourceCooldownException(resourceKey, retryAfter);
    }
}
