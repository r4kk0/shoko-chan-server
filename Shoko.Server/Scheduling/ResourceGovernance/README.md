# Scheduler Resource Governance

Resource governance is the scheduler's control fabric. Jobs declare the scheduler resources they consume, resource limits define the hard ceilings for each resource, and acquisition filters act as dispatch gates before Quartz starts a job.

## Concepts

- Resource domain: a shared resource that can be exhausted or rate limited, such as AniDB HTTP, AniDB UDP, TMDB, local CPU, disk, or the database.
- Resource limit: the top-level policy that decides when a resource is available again.
- Dispatch gate: an acquisition filter that keeps jobs queued while one of their resource domains is unavailable.

This keeps external API limits and local capacity limits above individual jobs, so job implementations do not each need to reinvent their own throttling rules.
