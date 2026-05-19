using System;
using System.Diagnostics;
using System.Threading;
using Microsoft.Extensions.Logging;
using Shoko.Abstractions.Config;
using Shoko.Abstractions.Config.Events;
using Shoko.Server.Scheduling.ResourceGovernance;
using Shoko.Server.Scheduling.ResourceGovernance.Calibration;
using Shoko.Server.Settings;

#nullable enable
namespace Shoko.Server.Providers.AniDB.UDP;

public class UDPRateLimiter
{
    private readonly ILogger _logger;
    private readonly object _lock = new();

    private readonly object _settingsLock = new();

    private readonly Stopwatch _requestWatch = new();

    private readonly Stopwatch _activeTimeWatch = new();

    private readonly ConfigurationProvider<ServerSettings> _settingsProvider;

    private readonly Func<IServerSettings, AnidbRateLimitSettings> _settingsSelector;

    private readonly AniDBLimitCalibrator _calibrator;

    private int? _shortDelay;

    // From AniDB's wiki about UDP rate limiting:
    // Short Term:
    // A Client MUST NOT send more than 0.5 packets per second(that's one packet every two seconds, not two packets a second!)
    // The server will start to enforce the limit after the first 5 packets have been received.
    private int ShortDelay
    {
        get
        {
            EnsureUsable();

            return _shortDelay!.Value;
        }
    }

    private int? _longDelay;

    // From AniDB's wiki about UDP rate limiting:
    // Long Term:
    // A Client MUST NOT send more than one packet every four seconds over an extended amount of time.
    // An extended amount of time is not defined. Use common sense.
    private int LongDelay
    {
        get
        {
            EnsureUsable();

            return _longDelay!.Value;
        }
    }

    private long? _shortPeriod;

    // Switch to longer delay after a short period
    private long ShortPeriod
    {
        get
        {
            EnsureUsable();

            return _shortPeriod!.Value;
        }
    }

    private long? _resetPeriod;

    private int? _safetyHeadroom;

    // Switch to shorter delay after inactivity
    private long ResetPeriod
    {
        get
        {
            EnsureUsable();

            return _resetPeriod!.Value;
        }
    }

    /// <summary>
    /// Ensures that all the rate limiting values are usable.
    /// </summary>
    /// <param name="force">Force the values to be reapplied from settings, even if they are already in a usable state.</param>
    private void EnsureUsable(bool force = false)
    {
        if (!force && _shortDelay.HasValue)
            return;

        lock (_settingsLock)
        {
            if (!force && _shortDelay.HasValue)
                return;

            var settings = _settingsSelector(_settingsProvider.Load());
            var baseRate = settings.BaseRateInSeconds * 1000;
            _shortDelay = baseRate;
            _longDelay = baseRate * settings.SlowRateMultiplier;
            _shortPeriod = baseRate * settings.SlowRatePeriodMultiplier;
            _resetPeriod = baseRate * settings.ResetPeriodMultiplier;
            _safetyHeadroom = settings.SafetyHeadroomInMilliseconds;
        }
    }

    public UDPRateLimiter(ILogger<UDPRateLimiter> logger, ConfigurationProvider<ServerSettings> settingsProvider, AniDBLimitCalibrator calibrator)
    {
        _logger = logger;
        _calibrator = calibrator;
        _requestWatch.Start();
        _activeTimeWatch.Start();
        _settingsProvider = settingsProvider;
        _settingsSelector = s => s.AniDb.UDPRateLimit;
        _settingsProvider.Saved += OnSettingsSaved;
    }

    ~UDPRateLimiter()
    {
        _settingsProvider.Saved -= OnSettingsSaved;
    }

    private void OnSettingsSaved(object? sender, ConfigurationSavedEventArgs<ServerSettings> eventArgs)
    {
        // Reset the cached values when the settings are updated.
        EnsureUsable(true);
    }

    private void ResetRate()
    {
        var elapsedTime = _activeTimeWatch.ElapsedMilliseconds;
        _activeTimeWatch.Restart();
        _logger.LogTrace("Rate is reset. Active time was {Time} ms", elapsedTime);
    }

    public TimeSpan GetTimeUntilAvailable(bool forceShortDelay = false)
    {
        var calibrationDelay = _calibrator.GetDelayUntilAvailable(SchedulerResources.AniDBUdp);
        if (!Monitor.TryEnter(_lock))
            return Max(calibrationDelay, TimeSpan.FromMilliseconds(100));

        try
        {
            var delay = _requestWatch.ElapsedMilliseconds;
            if (delay > ResetPeriod)
                return calibrationDelay;

            var currentDelay = !forceShortDelay && _activeTimeWatch.ElapsedMilliseconds > ShortPeriod ? LongDelay : ShortDelay;
            if (delay > currentDelay)
                return calibrationDelay;

            return Max(calibrationDelay, TimeSpan.FromMilliseconds(currentDelay - delay + _safetyHeadroom!.Value));
        }
        finally
        {
            Monitor.Exit(_lock);
        }
    }

    public T EnsureRate<T>(Func<T> action, bool forceShortDelay = false)
    {
        var calibrationDelay = _calibrator.GetDelayUntilAvailable(SchedulerResources.AniDBUdp);
        AniDBResourceCooldownGuard.ThrowIfLongCooldown(SchedulerResources.AniDBUdp, calibrationDelay);

        lock (_lock)
            try
            {
                calibrationDelay = _calibrator.GetDelayUntilAvailable(SchedulerResources.AniDBUdp);
                AniDBResourceCooldownGuard.ThrowIfLongCooldown(SchedulerResources.AniDBUdp, calibrationDelay);
                if (calibrationDelay > TimeSpan.Zero)
                {
                    _logger.LogTrace("AniDB UDP calibration is delaying request for {Delay} ms", calibrationDelay.TotalMilliseconds);
                    Thread.Sleep(calibrationDelay);
                }

                var delay = _requestWatch.ElapsedMilliseconds;
                if (delay > ResetPeriod) ResetRate();
                var currentDelay = !forceShortDelay && _activeTimeWatch.ElapsedMilliseconds > ShortPeriod ? LongDelay : ShortDelay;

                if (delay > currentDelay)
                {
                    _logger.LogTrace("Time since last request is {Delay} ms, not throttling", delay);
                    _logger.LogTrace("Sending AniDB command");
                    var response = action();
                    _calibrator.RecordSuccess(SchedulerResources.AniDBUdp);
                    return response;
                }

                var waitTime = currentDelay - (int)delay + _safetyHeadroom!.Value;

                _logger.LogTrace("Time since last request is {Delay} ms, throttling for {Time}ms", delay, waitTime);
                Thread.Sleep(waitTime);

                _logger.LogTrace("Sending AniDB command");
                var delayedResponse = action();
                _calibrator.RecordSuccess(SchedulerResources.AniDBUdp);
                return delayedResponse;
            }
            catch (AniDBBannedException ex)
            {
                _calibrator.RecordBan(SchedulerResources.AniDBUdp, ex.BanExpires);
                throw;
            }
            finally
            {
                _requestWatch.Restart();
            }
    }

    private static TimeSpan Max(TimeSpan left, TimeSpan right) => left > right ? left : right;
}
