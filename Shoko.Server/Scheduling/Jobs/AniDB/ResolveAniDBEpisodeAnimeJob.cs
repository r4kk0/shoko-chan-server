using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Shoko.Abstractions.Metadata.Anidb.Enums;
using Shoko.Abstractions.Metadata.Anidb.Services;
using Shoko.Server.Providers.AniDB.Interfaces;
using Shoko.Server.Providers.AniDB.UDP.Info;
using Shoko.Server.Repositories;
using Shoko.Server.Scheduling.Attributes;
using Shoko.Server.Scheduling.Concurrency;
using Shoko.Server.Scheduling.Dispatch.Attributes;
using Shoko.Server.Settings;

#pragma warning disable CS8618
#nullable enable
namespace Shoko.Server.Scheduling.Jobs.AniDB;

[DatabaseRequired]
[NetworkRequired]
[AniDBUdpRateLimited]
[DisallowConcurrencyGroup(ConcurrencyGroups.AniDB_UDP)]
[JobKeyGroup(JobKeyGroup.AniDB)]
public class ResolveAniDBEpisodeAnimeJob : BaseJob
{
    private readonly IRequestFactory _requestFactory;
    private readonly IAnidbService _anidbService;
    private readonly ISettingsProvider _settingsProvider;

    [JobKeyMember]
    public int EpisodeID { get; set; }

    public override string TypeName => "Resolve AniDB Episode Anime";

    public override string Title => "Resolving AniDB Episode Anime";

    public override Dictionary<string, object> Details => new()
    {
        { "EpisodeID", EpisodeID },
    };

    public override async Task Process()
    {
        _logger.LogInformation("Processing {Job}: {EpisodeID}", nameof(ResolveAniDBEpisodeAnimeJob), EpisodeID);

        var animeID = RepoFactory.AniDB_Episode.GetByEpisodeID(EpisodeID)?.AnimeID;
        if (animeID is null)
        {
            var request = _requestFactory.Create<RequestGetEpisode>(r => r.EpisodeID = EpisodeID);
            var response = request.Send();
            animeID = response.Response?.AnimeID;
        }

        if (animeID is not > 0)
            return;

        var xrefs = RepoFactory.CrossRef_File_Episode.GetByEpisodeID(EpisodeID)
            .Where(xref => xref.AnimeID == 0)
            .ToList();
        if (xrefs.Count == 0)
            return;

        foreach (var xref in xrefs)
            xref.AnimeID = animeID.Value;
        RepoFactory.CrossRef_File_Episode.Save(xrefs);

        if (RepoFactory.AnimeSeries.GetByAnimeID(animeID.Value) is not null &&
            RepoFactory.AnimeEpisode.GetByAniDBEpisodeID(EpisodeID) is not null)
            return;

        var settings = _settingsProvider.GetSettings();
        var refreshMethod = AnidbRefreshMethod.Remote | AnidbRefreshMethod.DeferToRemoteIfUnsuccessful | AnidbRefreshMethod.SkipTmdbUpdate | AnidbRefreshMethod.CreateShokoSeries;
        if (settings.AutoGroupSeries || settings.AniDb.DownloadRelatedAnime)
            refreshMethod |= AnidbRefreshMethod.DownloadRelations;

        await _anidbService.ScheduleRefreshOfAnimeByID(animeID.Value, refreshMethod).ConfigureAwait(false);
    }

    public ResolveAniDBEpisodeAnimeJob(IRequestFactory requestFactory, IAnidbService anidbService, ISettingsProvider settingsProvider)
    {
        _requestFactory = requestFactory;
        _anidbService = anidbService;
        _settingsProvider = settingsProvider;
    }

    protected ResolveAniDBEpisodeAnimeJob() { }
}
