# time-entries Specification

## Purpose

Defines how a client starts and reads a time entry through the HTTP API,
including the running-entry representation, description normalization, UTC
timestamps, concurrent-running behavior, and not-found behavior.

## Requirements

### Requirement: Start a running time entry

A client SHALL be able to create a new time entry that is running immediately.
The request MUST be a JSON object with an optional description. The server MUST
assign the start time; clients cannot supply a start time in this slice. The new
entry MUST have no end time and no duration until it is stopped.

#### Scenario: Start with a description

- **WHEN** a client sends `POST /time-entries` with a non-empty description
- **THEN** the API responds `201 Created` with a `Location: /time-entries/{id}`
  header and the created entry's representation

#### Scenario: Start without a description

- **WHEN** a client sends `POST /time-entries` with the description omitted
- **THEN** the entry is created with a null description

#### Scenario: Start time is assigned by the server

- **WHEN** a client starts a time entry
- **THEN** the returned start time is a UTC timestamp close to the time the request was received, and the end time and duration are null

### Requirement: Represent a running time entry

A time-entry representation MUST contain `id`, `description`, `startedAt`, `endedAt`, and `durationSeconds`. It MUST identify the entry by `id` and MUST NOT include a `url` self-link field, project data, tag data, or navigation data in this slice.

#### Scenario: Created response contains the running representation

- **WHEN** a time entry is created
- **THEN** the response body contains the entry's identifier, description, UTC start time, a null end time, and a null duration

#### Scenario: Representation identifies the entry by id

- **WHEN** a time entry is returned by the create or read endpoint
- **THEN** the body contains an `id` for the entry and does not contain a `url` field. The `endedAt` and `durationSecond` fiels will be `null`.

### Requirement: Normalize descriptions

The API MUST trim leading and trailing whitespace from a description before storing and returning it. A description that is empty or contains only whitespace MUST be treated as absent and represented as null.

#### Scenario: Trim surrounding whitespace

- **WHEN** a client sends a description with leading and trailing whitespace
- **THEN** the stored and returned description has the surrounding whitespace
  removed

#### Scenario: Blank description becomes null

- **WHEN** a client sends a description containing only whitespace
- **THEN** the entry is created with a null description

### Requirement: Reject overlong descriptions

The API MUST reject a description longer than 2048 characters after trimming.
The response MUST be `400 Bad Request` with an `application/problem+json` body
that identifies the description field, and no time entry may be created.

#### Scenario: Overlong description is rejected

- **WHEN** a client sends a description of 2049 or more characters after
  trimming
- **THEN** the API responds `400` with an error for the description field and
  no time entry is persisted

### Requirement: Read a time entry

A client SHALL be able to retrieve a time entry by its identifier. A successful
read MUST return the same representation as creation. An unknown or malformed
identifier MUST return `404 Not Found` with an `application/problem+json` body.

#### Scenario: Read an existing entry

- **WHEN** a client sends `GET /time-entries/{id}` for an existing entry
- **THEN** the API responds `200 OK` with the same representation returned by
  creation

#### Scenario: Unknown entry is not found

- **WHEN** a client sends `GET /time-entries/{id}` for an identifier that does
  not exist
- **THEN** the API responds `404` with an `application/problem+json` body

#### Scenario: Malformed identifier is not found

- **WHEN** a client sends `GET /time-entries/{id}` with an identifier that is
  not a GUID
- **THEN** the API responds `404`

### Requirement: Allow concurrent running entries

The API MUST allow more than one time entry to be running at the same time.
Starting a new entry MUST NOT stop, modify, or reject an existing running
entry. Coordinating concurrent timers is the client's responsibility.

#### Scenario: Starting another entry leaves the first running

- **WHEN** two time entries are started in succession
- **THEN** both are stored as running entries with distinct identifiers, and
  neither is ended or modified by the other
