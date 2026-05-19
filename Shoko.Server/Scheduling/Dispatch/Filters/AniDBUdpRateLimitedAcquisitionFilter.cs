using System;
using System.Collections.Generic;
using System.Linq;
using Shoko.Server.Providers.AniDB;
using Shoko.Server.Providers.AniDB.Interfaces;
using Shoko.Server.Scheduling.Jobs.Shoko;
using Shoko.Abstractions.Video.Services;
using Shoko.Abstractions.Core.Services;
using Shoko.Server.Scheduling.ResourceGovernance;
using Shoko.Server.Scheduling.ResourceGovernance.Limits;

namespace Shoko.Server.Scheduling.Dispatch.Filters;

public class AniDBUdpRateLimitedAcquisitionFilter : ResourceGatedAcquisitionFilter
{
    private readonly Type[] _typesWithoutProcessJob;

    private readonly Type[] _typesWithProcessJob;

    private bool _processJobIncluded;

    private bool _ready;

    private readonly IUDPConnectionHandler _connectionHandler;

    private readonly ISystemService _systemService;

    private readonly IVideoReleaseService _videoReleaseService;

    public AniDBUdpRateLimitedAcquisitionFilter(IUDPConnectionHandler connectionHandler, AniDBUdpResourceLimit resourceLimit, ISystemService systemService, IVideoReleaseService videoReleaseService) : base(resourceLimit)
    {
        _connectionHandler = connectionHandler;
        _systemService = systemService;
        _videoReleaseService = videoReleaseService;
        _connectionHandler.AniDBStateUpdate += OnAniDBStateUpdate;
        _systemService.AboutToStart += OnProvidersReady;
        _videoReleaseService.ProvidersUpdated += OnProvidersUpdated;
        _processJobIncluded = true;
        _ready = false;

        _typesWithProcessJob = GetJobTypesForResource(resourceLimit.ResourceKey);
        _typesWithoutProcessJob = _typesWithProcessJob.Where(a => !typeof(ProcessFileJob).IsAssignableFrom(a)).ToArray();
    }

    ~AniDBUdpRateLimitedAcquisitionFilter()
    {
        _connectionHandler.AniDBStateUpdate -= OnAniDBStateUpdate;
        _systemService.AboutToStart -= OnProvidersReady;
        _videoReleaseService.ProvidersUpdated -= OnProvidersUpdated;
    }

    private void OnProvidersReady(object sender, EventArgs e)
    {
        _ready = true;
        _processJobIncluded = _videoReleaseService.GetAvailableProviders(onlyEnabled: true).Any(a => a.Provider.Name is "AniDB");
        NotifyStateChanged();
    }

    private void OnProvidersUpdated(object sender, EventArgs e)
    {
        if (!_ready) return;
        _processJobIncluded = _videoReleaseService.GetAvailableProviders(onlyEnabled: true).Any(a => a.Provider.Name is "AniDB");
        NotifyStateChanged();
    }

    private void OnAniDBStateUpdate(object sender, AniDBStateUpdate e)
    {
        NotifyStateChanged();
    }

    protected override IEnumerable<Type> GetResourceLimitedTypes() => _processJobIncluded ? _typesWithProcessJob : _typesWithoutProcessJob;

    protected override IEnumerable<Type> GetStateBlockedTypes() => !_connectionHandler.IsAlive || _connectionHandler.IsBanned || _connectionHandler.IsInvalidSession
        ? GetResourceLimitedTypes()
        : [];
}
