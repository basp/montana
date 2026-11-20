# Tasks

## 1. Test host scaffolding

- [x] 1.1 Add `Microsoft.AspNetCore.Mvc.Testing` to
  `src/Montana.Tests/Montana.Tests.csproj` and expose the top-level `Program`
  class as `public partial class Program`. Verify `dotnet build montana.slnx`
  succeeds.
- [x] 1.2 Add a test application factory that replaces the registered
  `MontanaDbContext` with a shared in-memory SQLite connection and calls
  `EnsureCreated`. Verify a smoke test can host the app and receive a response
  from `GET /`.

## 2. Endpoint structure and contracts

- [x] 2.1 Create `src/Montana/Endpoints/TimeEntriesEndpoints.cs` with a
  `/time-entries` route group and wire `app.MapTimeEntryEndpoints()` from
  `Program.cs`. Verify `dotnet build montana.slnx` succeeds and the app starts.
- [x] 2.2 Add `CreateTimeEntryRequest`, `TimeEntryResponse`, and a shared
  response-mapping function. Verify the response contains only `id`,
  `description`, `startedAt`, `endedAt`, and `durationSeconds`, with no `url`,
  project, tag, or navigation fields.

## 3. Read endpoint

- [x] 3.1 Implement `GET /time-entries/{id:guid}` with `AsNoTracking`, the shared
  response mapping, a `404 application/problem+json` response for missing
  entries, and the route name `GetTimeEntry`. Verify `dotnet build` succeeds and
  the route name is available for `Location` generation.
- [x] 3.2 Add integration tests that seed an entry and assert `200` with the
  shared representation, plus `404` for an unknown id and `404` for a malformed
  id. Verify the tests pass with `dotnet test`.

## 4. Create endpoint

- [x] 4.1 Implement `POST /time-entries` with a server-assigned UTC start time,
  `endedAt` and `durationSeconds` left null, and a `201 Created` response with a
  `Location` header generated from the `GetTimeEntry` route. Verify a successful
  start returns the expected representation and the row is persisted.
- [x] 4.2 Add description normalization and validation: trim the value, convert
  empty or whitespace to null, and return `400 application/problem+json` with a
  `description` error for values over 2048 characters after trimming. Verify
  with tests for trimming, blank input, and an overlong value that no row is
  created.
- [x] 4.3 Add a test that starts two entries in succession and asserts both are
  running with distinct ids and neither is stopped or modified. Verify the test
  passes with `dotnet test`.
- [x] 4.4 Add a test that asserts the create and read representations agree and
  that neither contains a `url` field. Verify the test passes with `dotnet test`.

## 5. Integration verification

- [x] 5.1 Run the full suite with `dotnet test montana.slnx` and verify every
  test passes.
- [x] 5.2 Start the app with the Development profile and use Bruno or curl to
  `POST /time-entries`, follow the `Location` header with `GET`, and confirm the
  `201`, `200`, running representation, and a `400` ProblemDetails response for
  an overlong description.

## Workflow follow-up

- Archive the change after implementation and verification are complete.
