using System;

namespace Shoko.Server.Scheduling.ResourceGovernance;

public static class SchedulerResourceExtensions
{
    public static SchedulerResourceKey ToKey(this SchedulerResource resource) => resource.ToResourceKey();

    public static SchedulerResourceKey ToResourceKey(this SchedulerResource resource)
        => resource switch
        {
            SchedulerResource.AniDBHttp => SchedulerResources.AniDBHttp,
            SchedulerResource.AniDBUdp => SchedulerResources.AniDBUdp,
            SchedulerResource.TMDBApi => SchedulerResources.TMDBApi,
            SchedulerResource.LocalCpu => SchedulerResources.LocalCpu,
            SchedulerResource.LocalDiskRead => SchedulerResources.LocalDiskRead,
            SchedulerResource.LocalDiskWrite => SchedulerResources.LocalDiskWrite,
            SchedulerResource.Database => SchedulerResources.LocalDatabase,
            _ => throw new ArgumentOutOfRangeException(nameof(resource), resource, null),
        };
}
