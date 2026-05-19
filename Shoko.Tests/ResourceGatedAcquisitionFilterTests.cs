using System;
using System.Collections.Generic;
using System.Linq;
using Shoko.Server.Scheduling.Dispatch.Filters;
using Shoko.Server.Scheduling.Jobs.AniDB;
using Shoko.Server.Scheduling.Jobs.Shoko;
using Shoko.Server.Scheduling.ResourceGovernance;
using Xunit;

namespace Shoko.Tests;

public class ResourceGatedAcquisitionFilterTests
{
    [Fact]
    public void AniDBUdpCooldownExcludesAniDBUdpJobsButNotLocalShokoJobs()
    {
        var filter = new TestResourceGatedAcquisitionFilter(SchedulerResource.AniDBUdp);
        var excludedTypes = filter.GetTypesToExclude().ToHashSet();
        var shokoJobsAssembly = typeof(HashFileJob).Assembly;

        Assert.Contains(typeof(AddFileToMyListJob), excludedTypes);
        Assert.DoesNotContain(typeof(HashFileJob), excludedTypes);
        Assert.DoesNotContain(shokoJobsAssembly.GetType("Shoko.Server.Scheduling.Jobs.Shoko.ScanFolderJob", throwOnError: true), excludedTypes);
        Assert.DoesNotContain(typeof(MediaInfoJob), excludedTypes);
        Assert.DoesNotContain(typeof(RenameMoveFileJob), excludedTypes);
        Assert.DoesNotContain(typeof(ValidateAllImagesJob), excludedTypes);
    }

    private sealed class TestResourceGatedAcquisitionFilter(SchedulerResource resource) : ResourceGatedAcquisitionFilter(new TestResourceLimit(resource))
    {
        protected override IEnumerable<Type> GetResourceLimitedTypes() => GetJobTypesForResource(resource);
    }

    private sealed class TestResourceLimit(SchedulerResource resource) : ISchedulerResourceLimit
    {
        public SchedulerResource Resource { get; } = resource;

        public TimeSpan GetDelayUntilAvailable() => TimeSpan.FromSeconds(1);
    }
}
