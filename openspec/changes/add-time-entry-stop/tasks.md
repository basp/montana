# Tasks

## 1. Stop endpoint

- [ ] 1.1 Add `POST /time-entries/{id:guid}/stop` to the existing endpoint
  group in `src/Montana/Endpoints/TimeEntriesEndpoints.cs`, with a handler that
  loads the entry tracked, returns a 404 ProblemDetails for a missing entry,
  returns the existing representation without saving when `EndedAt` is already
  set, and otherwise assigns `EndedAt = DateTimeOffset.UtcNow`, saves, and
  returns the shared representation. Verify `dotnet build montana.slnx`
  succeeds and the existing test suite still passes.
- [ ] 1.2 Add a test that seeds a running entry with `StartedAt` a few minutes
  in the past, calls stop, and asserts `200`, a UTC `endedAt` at or after the
  start, a `durationSeconds` equal to the whole seconds between the returned
  timestamps, and the same values persisted in the database. Verify the test
  passes with `dotnet test`.
- [ ] 1.3 Add a test that stops the same entry twice and asserts both responses
  are `200` with an identical `endedAt` and `durationSeconds`, and that the
  stored row is unchanged after the second call. Verify the test passes with
  `dotnet test`.
- [ ] 1.4 Add tests for an unknown id (`404` with
  `application/problem+json`) and a malformed id (`404`), and assert no row is
  created or modified. Verify the tests pass with `dotnet test`.
- [ ] 1.5 Add a test that starts an entry, stops it, then reads it with
  `GET /time-entries/{id}` and asserts the stopped representation matches the
  stop response, including non-null `endedAt` and `durationSeconds` and no
  `url` field. Verify the test passes with `dotnet test`.
- [ ] 1.6 Add a test with two running entries that stops one and asserts the
  other remains running with null `endedAt` and `durationSeconds`. Verify the
  test passes with `dotnet test`.

## 2. Integration verification

- [ ] 2.1 Run the full suite with `dotnet test montana.slnx` and verify every
  test passes.
- [ ] 2.2 Start the app with the Development profile and use curl or Bruno to
  create an entry, stop it, stop it again, and read it back. Verify the first
  stop returns `200` with end time and duration, the second stop returns the
  same values unchanged, the read matches, and an unknown id returns a `404`
  ProblemDetails response.

## Workflow follow-up

- Archive the change after implementation and verification are complete.
- Confirm the archived `time-entries` spec no longer says end time and duration
  are always null on read.
