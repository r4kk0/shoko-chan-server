using System;
using Shoko.Server.Scheduling.ResourceGovernance;
using Shoko.Server.Services.ErrorHandling;

namespace Shoko.Server.Providers.AniDB;

[Serializable, SentryIgnore]
public class AniDBResourceCooldownException : Exception
{
    public AniDBResourceCooldownException(SchedulerResourceKey resourceKey, TimeSpan retryAfter) : base($"AniDB resource {resourceKey} is cooling down for {retryAfter}.")
    {
        ResourceKey = resourceKey;
        RetryAfter = retryAfter;
        RetryAt = DateTimeOffset.UtcNow + retryAfter;
    }

    public SchedulerResourceKey ResourceKey { get; }

    public TimeSpan RetryAfter { get; }

    public DateTimeOffset RetryAt { get; }
}
