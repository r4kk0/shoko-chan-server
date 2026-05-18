using System;
using Shoko.Server.Scheduling.ResourceGovernance;

namespace Shoko.Server.Scheduling.Dispatch.Attributes;

[AttributeUsage(AttributeTargets.Class)]
public class AniDBHttpRateLimitedAttribute : SchedulerResourceAttribute
{
    public AniDBHttpRateLimitedAttribute() : base(SchedulerResource.AniDBHttp) { }
}
