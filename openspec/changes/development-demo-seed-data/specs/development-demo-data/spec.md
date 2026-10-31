## ADDED Requirements

### Requirement: Demo data is created only by an explicit Development command
The API executable SHALL provide a non-HTTP `seed-demo-data` command that requires an owner email and SHALL execute only when the hosting environment is Development. Normal API startup, Production, CI, and E2E execution SHALL NOT automatically create demo business data.

#### Scenario: Developer explicitly seeds a local database
- **WHEN** a developer runs `seed-demo-data` in Development with the email of an existing enabled user
- **THEN** the command seeds the demo dataset, reports a successful summary, and exits without starting the HTTP server

#### Scenario: Seed command is attempted in Production
- **WHEN** `seed-demo-data` is invoked outside Development
- **THEN** the command refuses to write demo data and exits with a failure status

#### Scenario: Owner does not exist
- **WHEN** the supplied owner email does not identify an existing enabled Identity user
- **THEN** the command reports the missing owner and writes no demo business data

### Requirement: Demo dataset represents WEB_Demo product development
The command SHALL create a `WEB_Demo Development` Project owned by the supplied user, Project-scoped labels covering product and engineering work, and representative Issues distributed across Draft, Approved, InProgress, QA, and Done. The dataset SHALL include assigned, overdue, labeled, commented, and recently changed Issues so Dashboard, List filters, Kanban, Issue detail, comments, and history all have demonstrable content.

#### Scenario: Fresh database is seeded
- **WHEN** the command runs against a migrated database with a valid owner and no demo dataset
- **THEN** the owner can open `WEB_Demo Development` and see representative labels and Issues across all workflow columns
- **THEN** Dashboard contains assigned work, at least one overdue non-Done Issue, status counts, and recent activity

### Requirement: Demo seeding is idempotent and non-destructive
The command SHALL identify its Project, labels, and Issues using stable seed identifiers and SHALL be safe to run repeatedly. Repeated execution SHALL create missing seed records and repair required seed relationships without duplicating records, deleting user-created data, or resetting unrelated user changes.

#### Scenario: Seed command runs twice
- **WHEN** a developer runs the command twice for the same owner and database
- **THEN** the second run succeeds with the same demo Project, label, and Issue identifiers and creates no duplicate seed records

#### Scenario: User-created Project data exists
- **WHEN** the demo Project contains additional user-created labels, Issues, comments, or members
- **THEN** the command preserves those records while ensuring required seed records and owner membership exist

### Requirement: Demo seed operations are transactional
The command SHALL apply the demo business dataset in a database transaction when using a relational database. A failure before commit SHALL leave no partially created demo Project graph.

#### Scenario: Seed write fails
- **WHEN** any required demo record cannot be persisted
- **THEN** the command returns failure and the relational database rolls back the uncommitted seed changes

