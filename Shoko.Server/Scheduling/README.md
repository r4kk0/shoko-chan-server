# Scheduler Architecture

The scheduler is organized around the path a job takes from queueing to execution. The main goal is to keep work moving while preventing unsafe pressure on shared resources such as AniDB, TMDB, local disk, CPU, and the database.

```text
Job is scheduled
    |
    v
Quartz queue / job store
    |
    v
Dispatch gates
    |       \
    |        -> Resource governance
    |              |
    |              -> AniDB HTTP / AniDB UDP / TMDB / local capacity limits
    v
Job execution
    |
    v
Observation events
    |
    -> API / SignalR / CLI queue status
```

## Folders

```text
Scheduling/
  Dispatch/
    Attributes/        Job classification attributes.
    Filters/           Pre-execution gates.

  ResourceGovernance/
    Calibration/       Passive provider limit observations.
    Limits/            Per-resource hard limit implementations.
    SchedulerResource  Shared resource domains.

  Locks/               Low-level scheduler lock implementations.
  Observation/         Queue state models and events.
  Jobs/                Actual Quartz jobs.

  QuartzStartup.cs     Scheduler registration and recurring job setup.
  ThreadPooledJobStore Custom Quartz job store and dispatch integration.
  JobFactory.cs        DI-backed Quartz job creation.
  QueueHandler.cs      Public queue control/status surface.
```

## Responsibility Map

```text
Need to add a new job?
  -> Add it under Jobs/.

Need to say a job uses a shared resource?
  -> Add or reuse an attribute under Dispatch/Attributes/.

Need to block jobs before execution?
  -> Add or update a filter under Dispatch/Filters/.

Need a hard limit for an API or local resource?
  -> Add a resource domain and limit under ResourceGovernance/.

Need to expose queue state to UI/API/CLI?
  -> Use Observation/ and QueueHandler.
```

Quartz integration and queue plumbing still live at the root of this folder while the scheduler architecture settles.

## Dispatch And Resource Governance

Jobs declare their runtime requirements with attributes. A job can mark that it needs network access, database access, or a governed provider resource such as AniDB HTTP or AniDB UDP.

```text
Job type
  -> requirement attributes
  -> dispatch filters
  -> resource governance
  -> wait or execute
```

Dispatch filters read those attributes and exclude job types that cannot safely run. Excluded jobs remain queued; they are not failed just because a required resource is cooling down or temporarily unavailable.

`ResourceGovernance/` owns the resource availability model. It defines resource domains, tracks cooldown concepts, and exposes whether a governed resource can accept more work. Provider code can report API pressure, session pressure, ban signals, or rate-limit/backoff signals into this layer, but providers do not decide which queued jobs should run next.

Dispatch remains the decision point:

```text
Provider pressure signal
    |
    v
ResourceGovernance updates availability
    |
    v
Dispatch filter checks job requirements
    |
    +-- resource available   -> job may execute
    |
    +-- resource unavailable -> job waits in the queue
```
