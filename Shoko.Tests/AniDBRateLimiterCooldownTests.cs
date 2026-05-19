#nullable enable

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using Namotion.Reflection;
using NJsonSchema;
using Shoko.Abstractions.Config;
using Shoko.Abstractions.Config.Services;
using Shoko.Abstractions.Core;
using Shoko.Abstractions.Plugin.Models;
using Shoko.Server.Providers.AniDB;
using Shoko.Server.Providers.AniDB.HTTP;
using Shoko.Server.Providers.AniDB.UDP;
using Shoko.Server.Scheduling.ResourceGovernance;
using Shoko.Server.Scheduling.ResourceGovernance.Calibration;
using Shoko.Server.Settings;
using Xunit;

namespace Shoko.Tests;

public class AniDBRateLimiterCooldownTests
{
    [Fact]
    public async Task HttpEnsureRateThrowsDuringLongCooldownWithoutInvokingAction()
    {
        var calibrator = CreateCalibratorWithLongCooldown(SchedulerResources.AniDBHttp);
        var limiter = new HttpRateLimiter(Mock.Of<ILogger<HttpRateLimiter>>(), new ConfigurationProvider<ServerSettings>(CreateConfigurationService()), calibrator);
        var invoked = false;

        var exception = await Assert.ThrowsAsync<AniDBResourceCooldownException>(() => limiter.EnsureRate(() =>
        {
            invoked = true;
            return Task.FromResult("ok");
        }));

        Assert.False(invoked);
        Assert.Equal(SchedulerResources.AniDBHttp, exception.ResourceKey);
        Assert.True(exception.RetryAfter > AniDBResourceCooldownGuard.LongCooldownThreshold);
    }

    [Fact]
    public void UdpEnsureRateThrowsDuringLongCooldownWithoutInvokingAction()
    {
        var calibrator = CreateCalibratorWithLongCooldown(SchedulerResources.AniDBUdp);
        var limiter = new UDPRateLimiter(Mock.Of<ILogger<UDPRateLimiter>>(), new ConfigurationProvider<ServerSettings>(CreateConfigurationService()), calibrator);
        var invoked = false;

        var exception = Assert.Throws<AniDBResourceCooldownException>(() => limiter.EnsureRate(() =>
        {
            invoked = true;
            return "ok";
        }));

        Assert.False(invoked);
        Assert.Equal(SchedulerResources.AniDBUdp, exception.ResourceKey);
        Assert.True(exception.RetryAfter > AniDBResourceCooldownGuard.LongCooldownThreshold);
    }

    private static AniDBLimitCalibrator CreateCalibratorWithLongCooldown(SchedulerResourceKey resourceKey)
    {
        var calibrator = new AniDBLimitCalibrator(Mock.Of<ILogger<AniDBLimitCalibrator>>());
        calibrator.RecordThrottle(resourceKey, TimeSpan.FromMinutes(5), "test");
        return calibrator;
    }

    private static IConfigurationService CreateConfigurationService()
    {
        var configurationService = new Mock<IConfigurationService>();
        var configurationInfo = CreateConfigurationInfo(configurationService.Object);
        configurationService.Setup(service => service.GetConfigurationInfo<ServerSettings>()).Returns(configurationInfo);
        configurationService.Setup(service => service.Load(configurationInfo, It.IsAny<bool>())).Returns(new ServerSettings());
        return configurationService.Object;
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
            PluginInfo = new LocalPluginInfo
            {
                ID = Guid.NewGuid(),
                Name = "TestPlugin",
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
            },
        };
}
