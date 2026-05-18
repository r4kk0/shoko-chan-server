# Scheduler Resource Governance

Resource governance is the scheduler's control fabric. Jobs declare the scheduler resources they consume, resource limits define the hard ceilings for each resource, and acquisition filters act as dispatch gates before Quartz starts a job.

```text
Job classification
    |
    v
SchedulerResource
    |
    v
ISchedulerResourceLimit
    |
    +-- documented/configured rate limit
    |
    +-- passive calibration delay
    |
    v
Dispatch filter decision
    |
    +-- available   -> job may execute
    |
    +-- cooling down -> job remains queued
```

## Concepts

- Resource domain: a shared resource that can be exhausted or rate limited, such as AniDB HTTP, AniDB UDP, TMDB, local CPU, disk, or the database.
- Resource limit: the top-level policy that decides when a resource is available again. Concrete limit implementations live in `Limits/`.
- Dispatch gate: an acquisition filter that keeps jobs queued while one of their resource domains is unavailable.

This keeps external API limits and local capacity limits above individual jobs, so job implementations do not each need to reinvent their own throttling rules.

## Current Resource Domains

```text
SchedulerResource
  AniDBHttp       Governed by AniDBHttpResourceLimit.
  AniDBUdp        Governed by AniDBUdpResourceLimit.
  TMDBApi         Reserved for TMDB API governance.
  LocalCpu        Reserved for CPU/thread pressure governance.
  LocalDiskRead   Reserved for disk read pressure governance.
  LocalDiskWrite  Reserved for disk write pressure governance.
  Database        Reserved for database pressure governance.
```

## Current AniDB Wiring

```text
[AniDBHttpRateLimited]
    |
    v
SchedulerResource.AniDBHttp
    |
    v
AniDBHttpResourceLimit
    |
    v
HttpRateLimiter
    |
    v
AniDBLimitCalibrator

[AniDBUdpRateLimited]
    |
    v
SchedulerResource.AniDBUdp
    |
    v
AniDBUdpResourceLimit
    |
    v
UDPRateLimiter
    |
    v
AniDBLimitCalibrator
```

Calibration observations are passive. AniDB responses, overload backoff, and ban signals can add temporary cooling time, but the scheduler does not probe for higher limits by intentionally exceeding documented guidance.

## Adding a New Governed Resource

```text
1. Add a value to SchedulerResource.
2. Add an attribute or reuse SchedulerResourceAttribute.
3. Add an ISchedulerResourceLimit implementation under Limits/.
4. Register the limit in QuartzStartup.
5. Add or update a dispatch filter that asks the limit before execution.
6. Apply the attribute to jobs that consume that resource.
```

The intended direction is that external providers and local capacity controls are governed here, while jobs remain focused on their actual work.
