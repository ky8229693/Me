## Context

The PMS persists Identity users, Projects, memberships, labels, Issues, comments, history, and audit events in PostgreSQL. A fresh development database currently demonstrates only infrastructure; meaningful Dashboard and Kanban behavior requires manual data entry. Existing operational commands such as `bootstrap-admin` and E2E database lifecycle commands already branch before HTTP startup.

Demo data must be useful but must never silently appear in Production, CI, E2E, or normal application startup. It must also avoid publishing a default password.

## Goals / Non-Goals

**Goals:**

- Seed a coherent self-hosting Project that demonstrates the current PMS features.
- Reuse an explicitly supplied existing Identity user as Project Owner.
- Make repeated local runs safe and predictable.
- Preserve user-created records within and outside the demo Project.
- Test environment safety, representative data, and idempotency.

**Non-Goals:**

- Automatically seed every Development startup.
- Create demo credentials, passwords, Admin roles, or additional fake Identity users.
- Reset the entire database or provide a production sample-data migration.
- Keep seed Issues synchronized with every future OpenSpec change automatically.
- Add an anonymous or authenticated HTTP endpoint for seeding.

## Decisions

### 1. Use an explicit non-HTTP command

Add `seed-demo-data --owner-email <email>` following the existing command pattern. `Program.cs` resolves the command after database migration and foundational role/permission initialization, invokes the scoped seed service, writes a concise result, sets the exit code, and returns before middleware or HTTP endpoints start.

An explicit command is preferred over startup seeding because it makes database mutation visible and avoids slowing or surprising every development launch. An HTTP endpoint is rejected because seeding is an operational concern and would enlarge the attack surface.

### 2. Require an existing enabled owner

The command resolves a case-insensitive Identity email and rejects missing or disabled users. It does not create credentials. The developer first registers a user or uses the existing Admin bootstrap flow, then passes that email to the seed command.

This preserves the project's credential safety decisions and makes the resulting Project immediately accessible to a real local account.

### 3. Use stable deterministic GUIDs for seed-owned records

Define a centralized seed catalog containing fixed GUIDs for the demo Project, labels, and Issues. Membership uses the existing `(ProjectId, UserId)` key. Comments, history, and audit events use stable GUIDs or stable existence predicates tied to seed Issue IDs.

Deterministic identifiers provide stronger idempotency than title matching and remain reliable if display text evolves. The service adds missing records and required joins. It does not delete unexpected records and avoids overwriting ordinary user-edited text/status once the seed entity already exists, except that it may restore the supplied owner's Owner membership and required seed label assignments.

### 4. Seed a representative but bounded dataset

The catalog creates labels such as Backend, Frontend, Security, DevOps, Testing, and Documentation. Issues represent completed foundations and current/future work, span all five fixed statuses, include multiple priorities and due dates, and assign a subset to the owner. At least one non-Done assigned Issue has a past due date. Selected Issues receive one seed comment and representative history/audit entries.

The dataset remains small enough to understand visually and does not attempt load/performance testing.

### 5. Guard both command parsing and service execution

The command handler checks `IHostEnvironment.IsDevelopment()`. The seed service also requires an explicit `allowDevelopmentSeed` value derived from the environment so tests can prove fail-closed behavior even if it is called outside the normal command branch.

Defense in depth prevents a future `Program.cs` refactor from accidentally enabling Production writes.

### 6. Use one relational transaction

The seed service opens a transaction when `Database.IsRelational()`, builds or repairs the required graph, saves once, and commits. EF InMemory tests execute the same logic without a transaction. No schema migration is required because all target tables already exist.

## Risks / Trade-offs

- [Fixed IDs collide with unrelated manually inserted records] → Use reserved, documented GUIDs and verify an existing record with a seed ID has the expected Project ownership before modifying relationships.
- [Changing the supplied owner on a later run creates multiple Owners] → Ensure the new supplied owner has Owner membership but preserve prior memberships; do not silently demote users.
- [Seed content becomes stale] → Keep the catalog intentionally representative rather than a complete release history and update it only when demo value changes.
- [Developers expect automatic data after startup] → Document the explicit command and its prerequisites in the development guide.
- [A partial write leaves confusing data] → Use a relational transaction and test rollback/failure behavior where practical.

## Migration Plan

No schema migration is required. Deploying the code alone creates no data. Developers opt in by preparing their local database and running the documented command. Rollback consists of removing the command code; existing demo rows are ordinary PMS data and are intentionally not deleted automatically.

## Open Questions

- None blocking. The initial owner is the only seeded user; additional realistic team members can be introduced by a later opt-in change if multi-user demonstrations require them.
