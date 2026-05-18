using System;
using Shoko.Server.Scheduling.ResourceGovernance;

namespace Shoko.Server.Scheduling.Dispatch.Attributes;

[AttributeUsage(AttributeTargets.Class)]
public class AniDBUdpRateLimitedAttribute : SchedulerResourceAttribute
{
    public AniDBUdpRateLimitedAttribute() : base(SchedulerResource.AniDBUdp) { }
}
