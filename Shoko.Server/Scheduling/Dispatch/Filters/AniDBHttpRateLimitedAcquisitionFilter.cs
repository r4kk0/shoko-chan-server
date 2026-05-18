using System;
using System.Collections.Generic;
using Shoko.Server.Providers.AniDB;
using Shoko.Server.Providers.AniDB.Interfaces;
using Shoko.Server.Scheduling.ResourceGovernance;
using Shoko.Server.Scheduling.ResourceGovernance.Limits;

namespace Shoko.Server.Scheduling.Dispatch.Filters;

public class AniDBHttpRateLimitedAcquisitionFilter : ResourceGatedAcquisitionFilter
{
    private readonly Type[] _types;
    private readonly IHttpConnectionHandler _connectionHandler;

    public AniDBHttpRateLimitedAcquisitionFilter(IHttpConnectionHandler connectionHandler, AniDBHttpResourceLimit resourceLimit) : base(resourceLimit)
    {
        _connectionHandler = connectionHandler;
        _connectionHandler.AniDBStateUpdate += OnAniDBStateUpdate;
        _types = GetJobTypesForResource(SchedulerResource.AniDBHttp);
    }

    ~AniDBHttpRateLimitedAcquisitionFilter()
    {
        _connectionHandler.AniDBStateUpdate -= OnAniDBStateUpdate;
    }

    private void OnAniDBStateUpdate(object sender, AniDBStateUpdate e)
    {
        NotifyStateChanged();
    }

    protected override IEnumerable<Type> GetResourceLimitedTypes() => _types;

    protected override IEnumerable<Type> GetStateBlockedTypes() => !_connectionHandler.IsAlive || _connectionHandler.IsBanned ? _types : Array.Empty<Type>();
}
