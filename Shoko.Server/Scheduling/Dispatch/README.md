# Scheduler Dispatch

Dispatch is the scheduler's pre-execution gate. Filters inspect queued jobs and hold back work that cannot safely run yet because the network is unavailable, the database is blocked, or a governed resource domain is still cooling down.

```text
Queued jobs
    |
    v
Dispatch filters
    |
    +-- NetworkRequiredAcquisitionFilter
    |     Holds network jobs when network-dependent services are not ready.
    |
    +-- DatabaseRequiredAcquisitionFilter
    |     Holds database jobs when the database is blocked.
    |
    +-- AniDBHttpRateLimitedAcquisitionFilter
    |     Holds AniDB HTTP jobs until the AniDB HTTP resource limit opens.
    |
    +-- AniDBUdpRateLimitedAcquisitionFilter
          Holds AniDB UDP jobs until UDP state and resource limits allow dispatch.
```

## Classification

Attributes in `Attributes/` classify jobs:

```text
[NetworkRequired]
[DatabaseRequired]
[AniDBHttpRateLimited]
[AniDBUdpRateLimited]
```

Those attributes describe what a job needs. Filters in `Filters/` enforce those needs before Quartz starts execution.

```text
Job type
  -> attributes describe requirements
  -> filters read those attributes
  -> blocked job remains queued
  -> allowed job moves into execution
```

## Resource-Gated Dispatch

Resource-gated filters use `ResourceGovernance/` instead of each job owning its own throttle.

```text
AniDB job
  -> declares AniDB HTTP or UDP resource usage
  -> dispatch filter asks the matching resource limit for availability
  -> if unavailable, the job stays queued and the queue is signaled later
```

This keeps ban-prevention and throughput policy above individual jobs.
