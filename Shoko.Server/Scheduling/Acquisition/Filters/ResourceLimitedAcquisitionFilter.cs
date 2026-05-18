using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using Quartz;
using Shoko.Server.Scheduling.ResourceLimits;

#nullable enable
namespace Shoko.Server.Scheduling.Acquisition.Filters;

public abstract class ResourceLimitedAcquisitionFilter : IAcquisitionFilter
{
    private readonly object _timerLock = new();
    private readonly ISchedulerResourceLimit _resourceLimit;
    private Timer? _resourceTimer;
    private DateTimeOffset? _resourceSignalAt;

    protected ResourceLimitedAcquisitionFilter(ISchedulerResourceLimit resourceLimit)
    {
        _resourceLimit = resourceLimit;
    }

    ~ResourceLimitedAcquisitionFilter()
    {
        _resourceTimer?.Dispose();
    }

    public IEnumerable<Type> GetTypesToExclude()
    {
        var stateBlockedTypes = GetStateBlockedTypes().ToArray();
        if (stateBlockedTypes.Length > 0)
            return stateBlockedTypes;

        var wait = _resourceLimit.GetDelayUntilAvailable();
        if (wait <= TimeSpan.Zero)
            return Array.Empty<Type>();

        ScheduleStateChanged(wait);
        return GetResourceLimitedTypes();
    }

    protected abstract IEnumerable<Type> GetResourceLimitedTypes();

    protected virtual IEnumerable<Type> GetStateBlockedTypes() => Array.Empty<Type>();

    protected void NotifyStateChanged() => StateChanged?.Invoke(this, EventArgs.Empty);

    protected static Type[] GetJobTypesForResource(SchedulerResource resource)
        => AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(a => a.GetTypes())
            .Where(type =>
                typeof(IJob).IsAssignableFrom(type) &&
                !type.IsAbstract &&
                type.GetCustomAttributes<SchedulerResourceAttribute>(true).Any(attribute => attribute.Resource == resource)
            )
            .ToArray();

    private void ScheduleStateChanged(TimeSpan wait)
    {
        var signalAt = DateTimeOffset.UtcNow + wait;
        lock (_timerLock)
        {
            if (_resourceSignalAt.HasValue && _resourceSignalAt.Value <= signalAt)
                return;

            _resourceSignalAt = signalAt;
            _resourceTimer ??= new Timer(_ =>
            {
                lock (_timerLock)
                    _resourceSignalAt = null;

                NotifyStateChanged();
            });
            _resourceTimer.Change(wait, Timeout.InfiniteTimeSpan);
        }
    }

    public event EventHandler? StateChanged;
}
