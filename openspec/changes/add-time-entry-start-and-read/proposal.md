# Proposal

## Why

Montana has a complete time-entry data model and SQLite persistence, but its
HTTP surface is still only a placeholder root endpoint. The first product
capability is starting a timer and reading it back, so a client can exercise
the core workflow end to end before more advanced features are added.

## What Changes

- Add `POST /time-entries` to create a running time entry. The server assigns
  `startedAt` in UTC; `endedAt` and `durationSeconds` are null.
- Accept an optional `description`, trim it, treat empty or whitespace as
  null, and reject descriptions over 2048 characters with a
  `400 application/problem+json` response.
- Return `201 Created` with a `Location: /time-entries/{id}` header and a body
  containing `id`, `description`, `startedAt`, `endedAt`, and
  `durationSeconds`.
- Add `GET /time-entries/{id}` returning the same representation, or a
  `404 application/problem+json` response when the entry does not exist.
- Use a shared response DTO for both endpoints. Do not expose the `TimeEntry`
  entity or its navigations through the API.
- Allow multiple concurrently running entries. The API does not auto-stop or
  reject an existing timer; coordinating timers is the client's
  responsibility.
- Explicitly out of scope: the stop endpoint, listing entries, project or tag
  assignment, a `url` self-link field, authentication, CORS, OpenAPI, and
  connection-string or migration automation.

## Capabilities

### New Capabilities

- `time-entries`: Creating and reading time entries through the HTTP API,
  including the running-entry representation, description normalization, UTC
  timestamps, concurrent-running behavior, and not-found behavior.

### Modified Capabilities

- None.

## Impact

- Affected API: two new minimal API endpoints under `/time-entries`.
- Affected code: `src/Montana/Program.cs` and a new endpoint/contract area,
  with tests in `src/Montana.Tests`.
- No schema or migration changes; the existing `TimeEntries` table already
  supports the representation.
- No new runtime dependencies. Endpoint tests may add
  `Microsoft.AspNetCore.Mvc.Testing`.
