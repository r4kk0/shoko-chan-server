using System;
using Shoko.Server.Scheduling.Acquisition.Attributes;

namespace Shoko.Server.Scheduling.ResourceGovernance;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class SchedulerResourceAttribute(SchedulerResource resource) : NetworkRequiredAttribute
{
    public SchedulerResource Resource { get; } = resource;
}
