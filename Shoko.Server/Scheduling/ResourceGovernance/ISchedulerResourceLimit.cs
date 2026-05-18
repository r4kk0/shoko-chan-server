using System;

namespace Shoko.Server.Scheduling.ResourceGovernance;

public interface ISchedulerResourceLimit
{
    SchedulerResource Resource { get; }

    TimeSpan GetDelayUntilAvailable();
}
