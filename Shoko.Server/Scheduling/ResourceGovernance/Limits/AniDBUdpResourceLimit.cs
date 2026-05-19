using System;
using Shoko.Server.Providers.AniDB.UDP;

namespace Shoko.Server.Scheduling.ResourceGovernance.Limits;

public class AniDBUdpResourceLimit(UDPRateLimiter rateLimiter) : ISchedulerResourceLimit
{
    public SchedulerResource Resource => SchedulerResource.AniDBUdp;

    public SchedulerResourceKey ResourceKey => SchedulerResources.AniDBUdp;

    public TimeSpan GetDelayUntilAvailable() => rateLimiter.GetTimeUntilAvailable();
}
