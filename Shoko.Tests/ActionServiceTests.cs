#nullable enable

using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using Shoko.Abstractions.Metadata.Anidb.Enums;
using Shoko.Abstractions.Metadata.Anidb.Services;
using Shoko.Abstractions.Plugin;
using Shoko.Abstractions.Video.Services;
using Shoko.Server.Databases;
using Shoko.Server.Providers.AniDB;
using Shoko.Server.Providers.AniDB.Interfaces;
using Shoko.Server.Providers.TMDB;
using Shoko.Server.Services;
using Shoko.Server.Settings;
using Xunit;

namespace Shoko.Tests;

public class ActionServiceTests
{
    [Fact]
    public async Task QueueAniDBRefreshWithImmediateTrueSchedulesPrioritizedRefresh()
    {
        var anidbService = new Mock<IAnidbService>();
        anidbService
            .Setup(service => service.ScheduleRefreshOfAnimeByID(123, It.IsAny<AnidbRefreshMethod>(), true))
            .Returns(Task.CompletedTask);
        var actionService = CreateActionService(anidbService.Object);

        var result = await actionService.QueueAniDBRefresh(123, force: false, downloadRelations: true, createSeriesEntry: true, immediate: true, cacheOnly: false, skipTmdbUpdate: true);

        Assert.False(result);
        anidbService.Verify(service => service.ScheduleRefreshOfAnimeByID(
            123,
            AnidbRefreshMethod.Remote |
            AnidbRefreshMethod.Cache |
            AnidbRefreshMethod.DownloadRelations |
            AnidbRefreshMethod.CreateShokoSeries |
            AnidbRefreshMethod.DeferToRemoteIfUnsuccessful |
            AnidbRefreshMethod.SkipTmdbUpdate,
            true), Times.Once);
        anidbService.Verify(service => service.RefreshAnimeByID(123, It.IsAny<AnidbRefreshMethod>(), default), Times.Never);
    }

    [Fact]
    public async Task QueueAniDBRefreshWithImmediateFalseSchedulesNormalRefresh()
    {
        var anidbService = new Mock<IAnidbService>();
        anidbService
            .Setup(service => service.ScheduleRefreshOfAnimeByID(456, It.IsAny<AnidbRefreshMethod>(), false))
            .Returns(Task.CompletedTask);
        var actionService = CreateActionService(anidbService.Object);

        var result = await actionService.QueueAniDBRefresh(456, force: true, downloadRelations: false, createSeriesEntry: false, immediate: false, cacheOnly: true, skipTmdbUpdate: false);

        Assert.False(result);
        anidbService.Verify(service => service.ScheduleRefreshOfAnimeByID(
            456,
            AnidbRefreshMethod.DeferToRemoteIfUnsuccessful,
            false), Times.Once);
        anidbService.Verify(service => service.RefreshAnimeByID(456, It.IsAny<AnidbRefreshMethod>(), default), Times.Never);
    }

    private static ActionService CreateActionService(IAnidbService anidbService)
        => new(
            Mock.Of<ILogger<ActionService>>(),
            null!,
            Mock.Of<IRequestFactory>(),
            Mock.Of<ISettingsProvider>(),
            Mock.Of<IVideoReleaseService>(),
            anidbService,
            Mock.Of<IVideoService>(),
            null!,
            null!,
            null!,
            Mock.Of<IPluginPackageManager>());
}
