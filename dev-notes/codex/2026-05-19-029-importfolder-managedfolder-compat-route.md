## Summary

Added minimal v3 backend compatibility aliases for the Web UI mismatch where the installed UI calls `/api/v3/ImportFolder` while the backend exposes `/api/v3/ManagedFolder`.

Inspection confirmed `ImportFolderController` was the old v3 route name and was replaced by `ManagedFolderController` in an older refactor (`d27b0e2e1`). The recent scheduler/AniDB governance commits did not introduce this mismatch.

## Files changed

- `Shoko.Server/API/v3/Controllers/ManagedFolderController.cs`

## Behaviour changed

- `GET /api/v3/ImportFolder` now routes to the existing managed-folder list action.
- `POST /api/v3/ImportFolder` now routes to the existing managed-folder create action.
- Existing `/api/v3/ManagedFolder` routes are unchanged.
- Existing `ManagedFolder` request/response models are reused.
- No business logic was duplicated.
- Scheduler, provider, rate-limiter, import, relocation, and resource-governance logic were not changed.

## Validation

- `dotnet build Shoko.Server.sln --no-restore`: passed.
- Looked for focused API/controller tests for `ManagedFolderController` / `ImportFolder`; none were present in `Shoko.Tests` or `Shoko.IntegrationTests`.
- `git diff --check`: passed with LF-to-CRLF warning for `ManagedFolderController.cs`.

## Risks / concerns

- Only the observed Web UI failures were aliased: `GET` and `POST /api/v3/ImportFolder`.
- Other old `ImportFolder` routes remain absent. If the installed Web UI calls PUT/PATCH/DELETE or nested scan/file routes under `ImportFolder`, those may still 404.
- Swagger may now show both canonical `ManagedFolder` and compatibility `ImportFolder` routes for these two actions.

## Suggested next step

Deploy to `shoko-chan-test` and verify the Web UI can list and create managed folders. Watch access logs for any additional `/api/v3/ImportFolder/...` 404s before adding more aliases.
