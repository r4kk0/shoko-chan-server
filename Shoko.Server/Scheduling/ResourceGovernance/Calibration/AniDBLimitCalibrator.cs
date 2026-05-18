using System;
using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace Shoko.Server.Scheduling.ResourceGovernance.Calibration;

public class AniDBLimitCalibrator
{
    private static readonly TimeSpan ThrottlePenalty = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan BanPenaltyFallback = TimeSpan.FromHours(12);

    private readonly ConcurrentDictionary<SchedulerResource, ProviderLimitProfile> _profiles = new();
    private readonly ILogger<AniDBLimitCalibrator> _logger;

    public AniDBLimitCalibrator(ILogger<AniDBLimitCalibrator> logger)
    {
        _logger = logger;
    }

    public TimeSpan GetDelayUntilAvailable(SchedulerResource resource)
    {
        if (!_profiles.TryGetValue(resource, out var profile))
            return TimeSpan.Zero;

        var now = DateTimeOffset.UtcNow;
        if (!profile.PauseUntil.HasValue || profile.PauseUntil.Value <= now)
            return TimeSpan.Zero;

        return profile.PauseUntil.Value - now;
    }

    public void RecordSuccess(SchedulerResource resource)
    {
        var profile = GetProfile(resource);
        lock (profile)
        {
            profile.ConsecutiveSuccesses++;
            profile.ConsecutiveThrottleSignals = 0;
            profile.LastSuccessAt = DateTimeOffset.UtcNow;
        }
    }

    public void RecordThrottle(SchedulerResource resource, TimeSpan? retryAfter, string reason)
    {
        var pause = retryAfter.GetValueOrDefault(ThrottlePenalty);
        var pauseUntil = DateTimeOffset.UtcNow + pause;
        var profile = GetProfile(resource);
        lock (profile)
        {
            profile.ConsecutiveSuccesses = 0;
            profile.ConsecutiveThrottleSignals++;
            profile.LastThrottleAt = DateTimeOffset.UtcNow;
            profile.PauseUntil = Max(profile.PauseUntil, pauseUntil);
        }

        _logger.LogWarning("AniDB {Resource} calibration observed throttle signal ({Reason}); pausing dispatch until {PauseUntil}", resource, reason, pauseUntil);
    }

    public void RecordBan(SchedulerResource resource, DateTime? banExpires)
    {
        var pauseUntil = banExpires.HasValue
            ? new DateTimeOffset(banExpires.Value.ToUniversalTime())
            : DateTimeOffset.UtcNow + BanPenaltyFallback;

        var profile = GetProfile(resource);
        lock (profile)
        {
            profile.ConsecutiveSuccesses = 0;
            profile.ConsecutiveThrottleSignals++;
            profile.LastThrottleAt = DateTimeOffset.UtcNow;
            profile.PauseUntil = Max(profile.PauseUntil, pauseUntil);
        }

        _logger.LogWarning("AniDB {Resource} calibration observed ban signal; pausing dispatch until {PauseUntil}", resource, pauseUntil);
    }

    private ProviderLimitProfile GetProfile(SchedulerResource resource)
        => _profiles.GetOrAdd(resource, static resource => new ProviderLimitProfile(resource));

    private static DateTimeOffset Max(DateTimeOffset? current, DateTimeOffset next)
        => current.HasValue && current.Value > next ? current.Value : next;

    private sealed class ProviderLimitProfile
    {
        public ProviderLimitProfile(SchedulerResource resource)
        {
            Resource = resource;
        }

        public SchedulerResource Resource { get; }

        public DateTimeOffset? PauseUntil { get; set; }

        public DateTimeOffset? LastSuccessAt { get; set; }

        public DateTimeOffset? LastThrottleAt { get; set; }

        public int ConsecutiveSuccesses { get; set; }

        public int ConsecutiveThrottleSignals { get; set; }
    }
}
