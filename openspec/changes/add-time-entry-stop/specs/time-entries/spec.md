# Spec Delta

## ADDED Requirements

### Requirement: Stop a running time entry

A client SHALL be able to stop a running time entry. The server MUST assign the
end time as the current UTC time; clients cannot supply an end time. After
stopping, the entry MUST have an end time and a derived duration, and the
response MUST reflect both.

#### Scenario: Stop a running entry

- **WHEN** a client sends `POST /time-entries/{id}/stop` for a running entry
- **THEN** the API responds `200 OK` with the entry's representation, where
  `endedAt` is a UTC timestamp at or after `startedAt` and `durationSeconds` is
  the whole number of seconds between them
- **AND** the entry is persisted with that end time and duration

#### Scenario: Stopping an already-stopped entry is idempotent

- **WHEN** a client stops an entry that is already stopped
- **THEN** the API responds `200 OK` with the original `endedAt` and
  `durationSeconds` unchanged, and does not assign a new end time

#### Scenario: Stop an unknown entry

- **WHEN** a client sends `POST /time-entries/{id}/stop` for an identifier that
  does not exist
- **THEN** the API responds `404 Not Found` with an `application/problem+json`
  body

#### Scenario: Stop with a malformed identifier

- **WHEN** a client sends `POST /time-entries/{id}/stop` with an identifier that
  is not a GUID
- **THEN** the API responds `404`

## MODIFIED Requirements

### Requirement: Represent a time entry

A time-entry representation MUST contain `id`, `description`, `startedAt`,
`endedAt`, and `durationSeconds`. It MUST identify the entry by `id` and MUST
NOT include a `url` self-link field, project data, tag data, or navigation data
in this slice.

#### Scenario: Created response contains the running representation

- **WHEN** a time entry is created
- **THEN** the response body contains the entry's identifier, description, UTC
  start time, a null end time, and a null duration

#### Scenario: Representation identifies the entry by id

- **WHEN** a time entry is returned by any endpoint
- **THEN** the body contains an `id` for the entry and does not contain a `url`
  field

#### Scenario: Running entry has null end time and duration

- **WHEN** a time entry that has not been stopped is returned by the create or
  read endpoint
- **THEN** `endedAt` and `durationSeconds` are null

#### Scenario: Stopped entry has an end time and duration

- **WHEN** a stopped time entry is returned by the read endpoint
- **THEN** `endedAt` is a UTC timestamp at or after `startedAt`
- **AND** `durationSeconds` is the whole number of seconds elapsed

## RENAMED Requirements

- FROM: `### Requirement: Represent a running time entry`
- TO: `### Requirement: Represent a time entry`
