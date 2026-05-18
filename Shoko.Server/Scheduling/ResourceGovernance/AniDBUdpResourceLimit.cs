using System;
using Shoko.Server.Providers.AniDB.UDP;

namespace Shoko.Server.Scheduling.ResourceGovernance;

public class AniDBUdpResourceLimit(UDPRateLimiter rateLimiter) : ISchedulerResourceLimit
{
    public SchedulerResource Resource => SchedulerResource.AniDBUdp;

    public TimeSpan GetDelayUntilAvailable() => rateLimiter.GetTimeUntilAvailable();
}
