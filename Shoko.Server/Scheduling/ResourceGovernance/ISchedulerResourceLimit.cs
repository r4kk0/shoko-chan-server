using System;

namespace Shoko.Server.Scheduling.ResourceGovernance;

public interface ISchedulerResourceLimit
{
    SchedulerResource Resource { get; }

    SchedulerResourceKey ResourceKey { get; }

    TimeSpan GetDelayUntilAvailable();
}
