using System;
using Shoko.Server.Scheduling.ResourceGovernance;

namespace Shoko.Server.Scheduling.Acquisition.Attributes;

[AttributeUsage(AttributeTargets.Class)]
public class AniDBHttpRateLimitedAttribute : SchedulerResourceAttribute
{
    public AniDBHttpRateLimitedAttribute() : base(SchedulerResource.AniDBHttp) { }
}
