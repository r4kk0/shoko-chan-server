using System;
using Shoko.Server.Scheduling.ResourceLimits;

namespace Shoko.Server.Scheduling.Acquisition.Attributes;

[AttributeUsage(AttributeTargets.Class)]
public class AniDBHttpRateLimitedAttribute : SchedulerResourceAttribute
{
    public AniDBHttpRateLimitedAttribute() : base(SchedulerResource.AniDBHttp) { }
}
