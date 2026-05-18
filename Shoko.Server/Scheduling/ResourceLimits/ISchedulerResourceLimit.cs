using System;

namespace Shoko.Server.Scheduling.ResourceLimits;

public interface ISchedulerResourceLimit
{
    SchedulerResource Resource { get; }

    TimeSpan GetDelayUntilAvailable();
}
