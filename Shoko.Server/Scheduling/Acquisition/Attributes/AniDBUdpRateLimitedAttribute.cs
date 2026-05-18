using System;
using Shoko.Server.Scheduling.ResourceGovernance;

namespace Shoko.Server.Scheduling.Acquisition.Attributes;

[AttributeUsage(AttributeTargets.Class)]
public class AniDBUdpRateLimitedAttribute : SchedulerResourceAttribute
{
    public AniDBUdpRateLimitedAttribute() : base(SchedulerResource.AniDBUdp) { }
}
