# Design

## Context

See `proposal.md` for motivation and `specs/time-entries/spec.md` for the
behavior contract. The current application is a minimal API with a single
placeholder route and a complete `TimeEntry` persistence model:

- `TimeEntry.Id` is a Guid v7 generated in the entity constructor.
- `StartedAt` and `EndedAt` are `DateTimeOffset` values normalized to UTC by
  `UtcDateTimeOffsetConverter`.
- `DurationSeconds` is derived by `MontanaDbContext.SaveChanges` and is null
  while `EndedAt` is null.
- `Description` has a 2048-character maximum in the model, but SQLite does not
  enforce that metadata.
- The project already has domain tests using an in-memory SQLite connection and
  `EnsureCreated`.

The HTTP surface is otherwise empty, so this change establishes the first API
conventions: routing, request/response DTOs, error shape, and endpoint tests.

## Goals / Non-Goals

**Goals:**

- Establish a small, repeatable endpoint structure for time entries.
- Keep the HTTP contract independent of the EF entity and its navigations.
- Make the created resource discoverable through a real `Location` header.
- Prove the full write-and-read path with an automated test.
- Keep the behavior aligned with the existing model: server-owned UTC time,
  derived duration, and multiple allowed running entries.

**Non-Goals:**

- Stop, list, edit, delete, project assignment, or tag assignment.
- Authentication, CORS, OpenAPI, rate limiting, or offline synchronization.
- URL self-links in the response body.
- Connection-string cleanup, startup migrations, or schema changes.
- A general error-handling pipeline beyond the errors this slice produces.

## Decisions

### Endpoint structure

Add an endpoint group under `/time-entries` in a dedicated
`Endpoints/TimeEntriesEndpoints.cs` file, wired from `Program.cs` with a single
mapping call.

```
Program.cs
  |
  +--> MapTimeEntryEndpoints()
         |
         +--> POST /time-entries
         +--> GET  /time-entries/{id:guid}
```

This keeps `Program.cs` to composition and gives the upcoming stop/list
endpoints a home. Alternatives considered: keeping everything in `Program.cs`
(simple for one endpoint, but does not scale past the first slice) and
controllers (heavier and inconsistent with the minimal API direction already in
place).

### DTO boundary

Use two records next to the endpoints:

- `CreateTimeEntryRequest` with an optional `Description`.
- `TimeEntryResponse` with `Id`, `Description`, `StartedAt`, `EndedAt`, and
  `DurationSeconds`.

Do not bind or return `TimeEntry` directly. The entity exposes navigations to
`Project` and `Tags`; serializing it would leak storage shape, risk reference
cycles once navigations are loaded, and allow derived fields to appear writable
in the contract. A shared response type also prevents create and read from
drifting.

### Server-owned start time

The create handler sets `StartedAt` to `DateTimeOffset.UtcNow`. The request does
not accept a start or end time in this slice. This avoids client clock skew and
keeps the start action unambiguous.

`DateTimeOffset.UtcNow` makes exact timestamp assertions non-deterministic.
Tests should assert that the value is UTC and close to the request time. If
deterministic time becomes important, inject `TimeProvider` later; it is not
needed for this slice.

### Description normalization and validation

The handler trims the description, converts an empty or whitespace-only result
to null, and rejects a trimmed value longer than 2048 characters with
`Results.ValidationProblem`, producing `400 application/problem+json` with an
error keyed on `description`.

SQLite does not enforce the model's `HasMaxLength(2048)`, so the API boundary is
the guard. Alternatives considered: relying on a database constraint (SQLite
does not enforce text length as configured) and adding a global validation
framework (unnecessary for one optional field).

### Response identity: keep `id`, omit `url`

The representation identifies the entry by `id`. A `url` self-link was
considered and deliberately dropped for this slice.

Reasons:

- `id` is the stable identity; a URL is an address that changes with routing,
  host, or versioning.
- The next slice adds `POST /time-entries/{id}/stop`; an id keeps that action
  obvious instead of requiring URL string surgery.
- The existing Guid v7 choice is intended for client-side generation,
  optimistic UI, and predictable referencing. Hiding the id behind a URL works
  against that model.
- The id is already present in any URL, so a url-only representation does not
  hide it; it only forces parsing or opaque URL handling.

The `Location` header already provides the created resource's address. A body
`url` can be added later as an additive convenience if a concrete client need
appears.

### Created response and `Location`

Name the read endpoint (`GetTimeEntry`) and generate the created response with
the route name rather than a hardcoded path. The `Location` value is the
relative path `/time-entries/{id}`.

This keeps the header correct if the group prefix moves and makes the location
host-agnostic. An absolute URL was considered but rejected: it is sensitive to
the request scheme, host, port, proxies, and test-server configuration, and the
client already knows its base URL.

### Read behavior

`GET /time-entries/{id:guid}` loads the entry with `AsNoTracking` and maps it
through the same response function as create. A missing entry returns
`404 application/problem+json`. A malformed identifier does not match the route
constraint and returns a bare 404.

The route constraint is preferred over manual parsing for this slice. If a
consistent problem-details body is later required for malformed ids, drop the
constraint and parse explicitly.

### Concurrency policy

The create endpoint is a pure append. It does not query for running entries,
stop them, or reject a new start. This matches the existing model and tests,
which explicitly allow multiple running entries. Coordination belongs to the
client. A future policy can add an explicit option or a separate endpoint; it
should not be hidden inside create.

### Error format

Validation failures use `application/problem+json` with field errors. Missing
entries use the same media type. No global exception handler is added in this
slice; unexpected failures may still return a framework 500. Adding a
ProblemDetails pipeline can be a later cross-cutting change.

### Tests

Add one HTTP-level test through `WebApplicationFactory<Program>`:

1. POST a time entry.
2. Assert `201`, a `Location` header, and the running representation.
3. GET the `Location`.
4. Assert `200` and the same representation.
5. Assert the entry is persisted in the test database.

This requires `Microsoft.AspNetCore.Mvc.Testing`, exposing `Program` as
`public partial class Program`, and replacing the registered `MontanaDbContext`
with an in-memory SQLite connection, reusing the existing test setup pattern.
The test covers routing, binding, serialization, persistence, and `Location`
generation in one flow. Additional tests cover an overlong description and an
unknown id. A manual Bruno request remains the client-facing smoke check.

Alternatives considered: handler-only tests (faster, but do not cover routing or
serialization) and manual-only verification (no regression safety).

## Risks / Trade-offs

- **Exact-time assertions are nondeterministic** -> assert UTC and a recent
  range; adopt `TimeProvider` only if that becomes insufficient.
- **In-memory SQLite with `EnsureCreated` does not validate migrations** -> this
  change has no schema change, so the risk is low; migration coverage can be a
  separate concern.
- **Malformed ids return a bare 404** -> acceptable for the first slice; parse
  manually later if a consistent problem-details body is required.
- **No global exception handler** -> unexpected failures may return a non-
  ProblemDetails 500; add a pipeline when error handling becomes cross-cutting.
- **A visible `Location` header but no body `url`** -> clients must resolve the
  relative path themselves; accepted to keep identity and address separate.
- **Multiple running timers can surprise a client** -> this is an explicit
  product decision; clients own the consequences.
- **Test-only WAF dependency** -> one package and a `Program` accessibility
  change; the scaffolding is reusable for every later endpoint.

## Migration Plan

No database schema or data migration is required. The endpoints are additive.
Deployment is a normal application update; rollback is removing the new routes
and endpoint file. Existing rows are unaffected.

## Open Questions

- Whether to introduce `TimeProvider` for deterministic time tests. Deferred;
  the current range assertion is sufficient for this slice.
