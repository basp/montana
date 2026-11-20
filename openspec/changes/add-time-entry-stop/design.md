# Design

## Context

See `proposal.md` for motivation and `specs/time-entries/spec.md` for the
behavior contract. The first slice established the conventions this change
must follow:

- `POST /time-entries` and `GET /time-entries/{id:guid}` live in
  `src/Montana/Endpoints/TimeEntriesEndpoints.cs` and share
  `TimeEntryResponse` and a `ToResponse` mapping.
- `TimeEntry.StartedAt` and `EndedAt` are `DateTimeOffset` values normalized to
  UTC by `UtcDateTimeOffsetConverter`.
- `MontanaDbContext.PrepareChanges` derives `DurationSeconds` on every added or
  modified `TimeEntry`: null while `EndedAt` is null, otherwise the whole
  seconds between the timestamps, throwing if the end precedes the start.
- A SQLite check constraint, `CK_TimeEntries_EndedAt_Not_Before_StartedAt`,
  enforces `EndedAt >= StartedAt` at the storage layer.
- `DurationSeconds` has an internal setter, so only the DbContext assigns it.
- HTTP tests run through `MontanaWebApplicationFactory` with an in-memory
  SQLite connection and a `Seed` helper.

## Goals / Non-Goals

**Goals:**

- Complete the start-stop lifecycle with a single additive route.
- Keep end-time ownership on the server and duration derivation in one place.
- Make stop safe to retry without client-side bookkeeping.
- Reuse the existing representation and lookup-failure conventions.

**Non-Goals:**

- Client-supplied end times, backdating, edit, delete, list, or assignment.
- Concurrency tokens, retry policies, or a global ProblemDetails pipeline.
- Schema changes, migrations, or new dependencies.
- Changing start behavior, including the allowance for multiple running
  entries.

## Decisions

### Route and handler shape

Add `POST /time-entries/{id:guid}/stop` to the existing endpoint group.

```
POST /time-entries/{id:guid}/stop
        |
        v
  load tracked entry
        |
        +-- missing -------------> 404 problem+json
        +-- EndedAt != null -----> 200, existing representation (no save)
        +-- running -------------> EndedAt = UtcNow, SaveChanges, 200
```

`PATCH /time-entries/{id}` with an `endedAt` value was considered and rejected:
it invites client-supplied times and edits, while stop is a command with no
inputs in this slice. The route constraint keeps malformed ids out of the
handler, matching `GET`.

### Server-assigned end time

The handler sets `EndedAt = DateTimeOffset.UtcNow` and does not read an end time
from the request. This mirrors the start endpoint, avoids client clock skew, and
keeps backdating an explicit future capability. Accepting a duration instead of
an end time was rejected because duration would drift from the stored
timestamps and duplicate the model's invariant.

### Idempotent 200 instead of a conflict

When `EndedAt` is already set, the handler returns `200 OK` with the stored
representation and skips `SaveChanges`. The original end time and duration are
never rewritten.

`409 Conflict` was considered and rejected: timers get double-clicked and
clients retry, and a personal tracker gains nothing from rejecting the second
call. Overwriting the end time was rejected because it makes retries
non-idempotent and silently corrupts recorded history. `200` rather than `201`
reflects that no resource is created and no `Location` header is needed.

### Duration remains DbContext-derived

The handler only assigns `EndedAt`; `PrepareChanges` computes
`DurationSeconds` during the save. Computing it in the handler would duplicate
the invariant, and `DurationSeconds` is not externally settable. A database
computed column would require a migration and add nothing over the existing
derivation.

### Lookup and failure behavior

The handler loads the entity tracked (no `AsNoTracking`) because it may mutate
it. A missing id returns `Results.Problem(statusCode: 404)`, the same shape the
read endpoint already produces. A non-GUID id does not match `{id:guid}` and
returns a bare 404, also matching the read endpoint. A uniform ProblemDetails
body for malformed ids remains a later cross-cutting change rather than scope
creep here.

### No concurrency token

No row version or concurrency token is introduced. Sequential retries are
already safe through idempotency. Two genuinely simultaneous stops can both
observe a null `EndedAt` and write end times milliseconds apart; SQLite
serializes the writes, the last row wins, and the derived duration is
recomputed consistently with that row. For a single-user SQLite tracker this is
acceptable. `ExecuteUpdate` with a null guard was considered but rejected
because it bypasses `PrepareChanges`, forcing duration derivation to be
duplicated. A multi-user store would revisit this with optimistic concurrency
or a conditional update.

### Testing approach

HTTP-level tests reuse `MontanaWebApplicationFactory` and the existing `Seed`
helper.

- Seed an entry with `StartedAt` a few minutes in the past, stop it, and assert
  `200`, a UTC `endedAt` at or after the start, and a `durationSeconds` equal to
  `(long)(endedAt - startedAt).TotalSeconds`.
- Assert duration against the returned timestamps rather than a positive
  minimum, because a stop within the same second correctly yields `0`.
- Stop the same entry twice and assert the second response is identical to the
  first and the stored row is unchanged.
- Assert `GET` after stop returns the same stopped representation.
- Assert unknown and malformed ids return 404, and the unknown case is
  `application/problem+json`.

## Risks / Trade-offs

- **A quick tap produces `durationSeconds = 0`** -> expected behavior from
  whole-second truncation; tests derive the expected value from the timestamps
  instead of assuming a positive minimum.
- **Idempotent 200 hides an accidental double stop** -> retry safety is worth
  more than surfacing a conflict for this product, and the original end time is
  preserved.
- **Concurrent stops are last-write-wins** -> acceptable for a single-user
  SQLite deployment; revisit if the app becomes multi-user or moves to a
  server database.
- **A negative interval throws during save** -> only reachable through
  inconsistent seeded or manipulated data, since the server owns both
  timestamps; the existing check constraint and exception are left as the
  guard rather than adding API handling.
- **Malformed ids return a bare 404** -> consistent with the read endpoint;
  uniform error bodies stay a separate concern.

## Migration Plan

No schema or data migration is required. The change is additive: one route and
its tests. Deploying is a normal application update, and rollback removes the
route, handler, and tests. Existing rows are unaffected because their
`EndedAt` remains null.

## Open Questions

None.
