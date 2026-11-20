# Proposal

## Why

The API can start and read a running time entry, but nothing can end it, so
`endedAt` and `durationSeconds` are permanently null. A client can begin the
core timer workflow but never finish it. Stopping is the smallest step that
makes the start-stop loop complete and exercises the persistence model's
derived duration.

The current representation requirement also states that `endedAt` and
`durationSeconds` are null whenever an entry is returned by the read endpoint.
That is only true for a running entry and will be false as soon as an entry can
be stopped, so the requirement must distinguish running from stopped.

## What Changes

- Add `POST /time-entries/{id}/stop` to end a running time entry.
- The server assigns `endedAt` as the current UTC time; clients cannot supply an
  end time in this slice.
- `durationSeconds` is derived by the existing DbContext save behavior from the
  stored timestamps and returned in the response.
- Stopping an entry that is already stopped is idempotent: the API responds
  `200 OK` with the original `endedAt` and `durationSeconds` unchanged. It does
  not rewrite the end time.
- An unknown or malformed identifier returns `404 Not Found` with an
  `application/problem+json` body, consistent with `GET /time-entries/{id}`.
- Rename the representation requirement from "Represent a running time entry"
  to "Represent a time entry", and amend it so `endedAt` and `durationSeconds`
  are null only while the entry is running, and are non-null with a
  non-negative duration once stopped.
- Explicitly out of scope: client-supplied start or end times, backdating,
  editing entries, deleting entries, listing or filtering entries, project or
  tag assignment, concurrency tokens, `TimeProvider`, and a global
  ProblemDetails pipeline.

## Capabilities

### New Capabilities

- None.

### Modified Capabilities

- `time-entries`: adds stopping a running entry with a server-assigned end time
  and idempotent repeat behavior, and clarifies that the end time and duration
  are null only while the entry is running.

## Impact

- Affected API: one new route, `POST /time-entries/{id}/stop`, returning the
  same representation as create and read.
- Affected code: `src/Montana/Endpoints/TimeEntriesEndpoints.cs` gains a stop
  handler and route; `src/Montana.Tests` gains stop coverage.
- No schema or migration changes. The `TimeEntries` table already stores
  `EndedAt`, and `MontanaDbContext.PrepareChanges` already derives
  `DurationSeconds` when an entry is saved with an end time.
- No new runtime or test dependencies.
