## Summary

Replaced the v3 immediate AniDB refresh bypass with governed scheduler enqueueing. `ActionService.QueueAniDBRefresh` now always schedules AniDB refresh work through `IAnidbService.ScheduleRefreshOfAnimeByID`, passing `prioritize: immediate` so `immediate=true` becomes high-priority queued work instead of direct provider execution.

## Files changed

- `Shoko.Server/Services/ActionService.cs`
- `Shoko.Server/API/v3/Controllers/SeriesController.cs`
- `Shoko.Tests/ActionServiceTests.cs`
- `Shoko.Tests/Shoko.Tests.csproj`

## Behaviour changed

- `QueueAniDBRefresh(immediate: true)` no longer calls `_anidbService.RefreshAnimeByID`.
- Both immediate and non-immediate refresh requests now enter the scheduler conveyor belt through `_anidbService.ScheduleRefreshOfAnimeByID(...)`.
- `immediate=true` is represented as `prioritize: true`, which uses existing Quartz trigger priority support.
- The existing `Task<bool>` return shape is unchanged.
- The method still returns `false` after enqueueing, preserving the existing queued-work response shape.
- v3 XML comments now describe `immediate` as queued refresh prioritization, not synchronous execution.

## Tests added/updated

- Added `ActionServiceTests`.
- Covered `QueueAniDBRefresh(immediate: true)` scheduling with `prioritize: true`.
- Covered `QueueAniDBRefresh(immediate: false)` scheduling with `prioritize: false`.
- Verified the direct `RefreshAnimeByID` path is not called for either case.
- Added a narrow local Quartz reference to `Shoko.Tests.csproj` because `ActionService`'s constructor exposes `ISchedulerFactory`.

## Validation

- `dotnet build Shoko.Server.sln --no-restore` passed.
- `dotnet test Shoko.Tests/Shoko.Tests.csproj --no-restore --filter "FullyQualifiedName~ResourceGatedAcquisitionFilterTests"` passed: 8 tests.
- `dotnet test Shoko.Tests/Shoko.Tests.csproj --no-restore --filter "FullyQualifiedName~AniDBRateLimiterCooldownTests"` passed: 2 tests.
- `dotnet test Shoko.Tests/Shoko.Tests.csproj --no-restore --filter "FullyQualifiedName~ActionServiceTests"` passed: 2 tests.
- `git diff --check` passed with line-ending warnings only:
  - `ActionService.cs`: LF will be replaced by CRLF next time Git touches it.
  - `Shoko.Tests.csproj`: LF will be replaced by CRLF next time Git touches it.

## Compatibility notes

- The v3 response type remains `ActionResult<bool>`.
- Callers will now receive queued-work semantics for `immediate=true`; the endpoint no longer confirms synchronous refresh completion.
- The response remains `false` after enqueueing, matching the previous queued result shape.

## Risks / concerns

- Existing clients that treated `true` from `immediate=true` as "fresh data is available now" will no longer get that signal.
- The new unit tests instantiate `ActionService` with null-for-unused dependencies. This is intentionally scoped to `QueueAniDBRefresh`, which only uses `_anidbService`.
- `force: true` plus `cacheOnly: true` still builds a refresh method with neither cache nor remote, preserving existing behavior even though `AnidbService.ScheduleRefreshInternal` will no-op for that flag combination.

## Suggested next step

Clean up the v3 API contract in a follow-up by changing these refresh endpoints from `ActionResult<bool>` to a queued-work response such as `Accepted()` or a small queue status DTO.
