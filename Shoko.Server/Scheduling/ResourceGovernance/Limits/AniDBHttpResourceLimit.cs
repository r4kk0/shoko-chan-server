using System;
using Shoko.Server.Providers.AniDB.HTTP;

namespace Shoko.Server.Scheduling.ResourceGovernance.Limits;

public class AniDBHttpResourceLimit(HttpRateLimiter rateLimiter) : ISchedulerResourceLimit
{
    public SchedulerResource Resource => SchedulerResource.AniDBHttp;

    public TimeSpan GetDelayUntilAvailable() => rateLimiter.GetTimeUntilAvailable();
}
