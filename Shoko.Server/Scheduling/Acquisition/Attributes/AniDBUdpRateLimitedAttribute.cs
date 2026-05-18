using System;
using Shoko.Server.Scheduling.ResourceLimits;

namespace Shoko.Server.Scheduling.Acquisition.Attributes;

[AttributeUsage(AttributeTargets.Class)]
public class AniDBUdpRateLimitedAttribute : SchedulerResourceAttribute
{
    public AniDBUdpRateLimitedAttribute() : base(SchedulerResource.AniDBUdp) { }
}
