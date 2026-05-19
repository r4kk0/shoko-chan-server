#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using Namotion.Reflection;
using NJsonSchema;
using Shoko.Abstractions.Config;
using Shoko.Abstractions.Config.Services;
using Shoko.Abstractions.Core;
using Shoko.Abstractions.Core.Events;
using Shoko.Abstractions.Core.Services;
using Shoko.Abstractions.Plugin.Models;
using Shoko.Abstractions.Video.Release;
using Shoko.Abstractions.Video.Services;
using Shoko.Server.Providers.AniDB.Interfaces;
using Shoko.Server.Providers.AniDB.UDP;
using Shoko.Server.Scheduling.Dispatch.Filters;
using Shoko.Server.Scheduling.Jobs.AniDB;
using Shoko.Server.Scheduling.Jobs.Shoko;
using Shoko.Server.Scheduling.ResourceGovernance;
using Shoko.Server.Scheduling.ResourceGovernance.Calibration;
using Shoko.Server.Scheduling.ResourceGovernance.Limits;
using Shoko.Server.Settings;
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
        Assert.DoesNotContain(shokoJobsAssembly.GetType("Shoko.Server.Scheduling.Jobs.Shoko.ScanFolderJob", throwOnError: true)!, excludedTypes);
        Assert.DoesNotContain(typeof(MediaInfoJob), excludedTypes);
        Assert.DoesNotContain(typeof(RenameMoveFileJob), excludedTypes);
        Assert.DoesNotContain(typeof(ValidateAllImagesJob), excludedTypes);
    }

    [Fact]
    public void AniDBUdpCooldownOnlyExcludesProcessFileJobWhenAniDBReleaseProviderIsEnabled()
    {
        var withoutAniDB = CreateAniDBUdpFilter("TestProvider");
        var withoutAniDBExcludedTypes = withoutAniDB.GetTypesToExclude().ToHashSet();

        Assert.DoesNotContain(typeof(ProcessFileJob), withoutAniDBExcludedTypes);

        var withAniDB = CreateAniDBUdpFilter("AniDB");
        var withAniDBExcludedTypes = withAniDB.GetTypesToExclude().ToHashSet();

        Assert.Contains(typeof(ProcessFileJob), withAniDBExcludedTypes);
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

    private static AniDBUdpRateLimitedAcquisitionFilter CreateAniDBUdpFilter(params string[] enabledProviderNames)
    {
        var connectionHandler = new Mock<IUDPConnectionHandler>();
        connectionHandler.SetupGet(handler => handler.IsAlive).Returns(true);
        connectionHandler.SetupGet(handler => handler.IsBanned).Returns(false);
        connectionHandler.SetupGet(handler => handler.IsInvalidSession).Returns(false);

        var configurationService = new Mock<IConfigurationService>();
        var configurationInfo = CreateConfigurationInfo(configurationService.Object);
        configurationService.Setup(service => service.GetConfigurationInfo<ServerSettings>()).Returns(configurationInfo);
        configurationService.Setup(service => service.Load(configurationInfo, It.IsAny<bool>())).Returns(new ServerSettings());

        var calibrator = new AniDBLimitCalibrator(Mock.Of<ILogger<AniDBLimitCalibrator>>());
        calibrator.RecordThrottle(SchedulerResource.AniDBUdp, TimeSpan.FromMinutes(1), "test");
        var rateLimiter = new UDPRateLimiter(Mock.Of<ILogger<UDPRateLimiter>>(), new ConfigurationProvider<ServerSettings>(configurationService.Object), calibrator);
        var resourceLimit = new AniDBUdpResourceLimit(rateLimiter);

        var systemService = new Mock<ISystemService>();
        var videoReleaseService = new Mock<IVideoReleaseService>();
        videoReleaseService.Setup(service => service.GetAvailableProviders(true))
            .Returns(enabledProviderNames.Select(CreateProviderInfo).ToArray());

        var filter = new AniDBUdpRateLimitedAcquisitionFilter(connectionHandler.Object, resourceLimit, systemService.Object, videoReleaseService.Object);
        systemService.Raise(service => service.AboutToStart += null, new ServerAboutToStartEventArgs { ServiceProvider = Mock.Of<IServiceProvider>() });

        return filter;
    }

    private static ConfigurationInfo CreateConfigurationInfo(IConfigurationService configurationService)
        => new(configurationService)
        {
            ID = Guid.NewGuid(),
            Path = null,
            Name = nameof(ServerSettings),
            Description = string.Empty,
            HasCustomActions = false,
            HasCustomNewFactory = false,
            HasCustomValidation = false,
            HasCustomSave = false,
            HasCustomLoad = false,
            HasLiveEdit = false,
            Type = typeof(ServerSettings),
            ContextualType = typeof(ServerSettings).ToContextualType(),
            Schema = new JsonSchema(),
            PluginInfo = CreatePluginInfo("TestPlugin"),
        };

    private static ReleaseProviderInfo CreateProviderInfo(string name)
    {
        var provider = new TestReleaseInfoProvider(name);
        return new ReleaseProviderInfo
        {
            ID = Guid.NewGuid(),
            Version = new Version(1, 0),
            Name = name,
            Description = string.Empty,
            Provider = provider,
            ConfigurationInfo = null,
            PluginInfo = CreatePluginInfo(name),
            Enabled = true,
            Priority = 0,
        };
    }

    private static LocalPluginInfo CreatePluginInfo(string name)
        => new()
        {
            ID = Guid.NewGuid(),
            Name = name,
            Description = string.Empty,
            Version = new VersionInformation
            {
                Version = new Version(1, 0),
                RuntimeIdentifier = "any",
                AbstractionVersion = new Version(1, 0),
                SourceRevision = null,
                ReleaseTag = null,
                Channel = ReleaseChannel.Debug,
                ReleasedAt = DateTime.UtcNow,
            },
            Authors = null,
            RepositoryUrl = null,
            HomepageUrl = null,
            Tags = Array.Empty<string>(),
            LoadOrder = 0,
            Thumbnail = null,
            InstalledAt = DateTime.UtcNow,
            IsEnabled = true,
            IsActive = true,
            CanLoad = true,
            CanUninstall = false,
            Plugin = null,
            PluginType = null,
            ServiceRegistrationType = null,
            ApplicationRegistrationType = null,
            ContainingDirectory = null,
            DLLs = Array.Empty<string>(),
            Types = Array.Empty<Type>(),
        };

    private sealed class TestReleaseInfoProvider(string name) : IReleaseInfoProvider
    {
        public string Name { get; } = name;

        public Task<ReleaseInfo?> GetReleaseInfoForVideo(ReleaseInfoContext context, CancellationToken cancellationToken) => Task.FromResult<ReleaseInfo?>(null);

        public Task<ReleaseInfo?> GetReleaseInfoById(string releaseId, CancellationToken cancellationToken) => Task.FromResult<ReleaseInfo?>(null);
    }
}
