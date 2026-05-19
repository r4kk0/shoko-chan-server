using System;
using Shoko.Server.Scheduling.Dispatch.Attributes;

namespace Shoko.Server.Scheduling.ResourceGovernance;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class SchedulerResourceAttribute(SchedulerResource resource) : AcquisitionFilterAttribute
{
    public SchedulerResource Resource { get; } = resource;
}
