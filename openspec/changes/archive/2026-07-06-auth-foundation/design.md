## Context

The backend currently uses ASP.NET Core Minimal APIs with endpoint modules, shared `ApiResponse<T>` envelopes, centralized exception handling, CORS from configuration, and in-memory Todo state. The frontend has a shared fetch-based API client and Todo API module.

The local database target is PostgreSQL database `web_demo` on port `5432`, and it currently has no application tables. Authentication tables should be created by EF Core migrations through ASP.NET Core Identity rather than by manual SQL table design. Local development will use username `postgres` and password `1234`.

## Goals / Non-Goals

**Goals:**

- Add PostgreSQL-backed authentication persistence using EF Core and ASP.NET Core Identity.
- Establish a minimal JWT authentication flow for API clients.
- Keep API responses inside the existing `ApiResponse<T>` envelope.
- Keep local development HTTP-only.
- Add frontend auth API helpers and token handling that fit the existing `src/api/` pattern.
- Add visible login/register UI in the frontend as part of the foundation.
- Add focused backend and frontend tests for the foundation behavior.

**Non-Goals:**

- Full role/permission management UI.
- External OAuth providers.
- Refresh token rotation and long-lived session management.
- Email confirmation, password reset email delivery, MFA, account lockout tuning, audit logs, or production hardening.
- Migrating Todo persistence or assigning Todos to users in this change.

## Decisions

### Use PostgreSQL with EF Core migrations

The project SHALL use the Npgsql EF Core provider and a configured local PostgreSQL connection string. EF Core migrations will create the schema so Identity tables remain aligned with the framework model.

Alternative considered: manually create login tables first. This is riskier because password hashes, security stamps, claims, tokens, roles, and future Identity upgrades are easy to model incorrectly.

### Use open local registration

Registration SHALL be enabled for local development in this foundation. This lets the login flow be exercised end to end without manually seeding users first.

Alternative considered: seeded-only users. This is safer for locked-down environments, but it would make the first frontend login/register workflow less useful for local development.

### Use ASP.NET Core Identity for user storage

Identity provides a proven schema and user management APIs for password hashing, users, roles, claims, logins, and tokens. The initial application can start with a minimal application user type and add profile fields later through migrations.

Alternative considered: custom user table and password service. This would add security-sensitive code before the project needs custom auth behavior.

### Use JWT bearer tokens for API authentication

Login returns a signed JWT that the frontend can send as `Authorization: Bearer <token>`. This fits the existing API-client architecture and keeps the backend stateless for authenticated API calls.

Alternative considered: cookie authentication. Cookies are good for server-rendered apps, but this project has a Vue SPA client and an API boundary, so bearer tokens are the simpler first fit.

### Keep endpoint modules and service boundaries

Authentication routes should live in an `Endpoints/AuthEndpoints.cs` extension. Auth behavior that does more than simple endpoint orchestration should live under `Services/`, with options under `Options/` and persistence under a clearly named data folder.

Alternative considered: put all auth wiring into `Program.cs`. That would undo the API foundation cleanup.

### Defer Todo ownership

This change establishes identity first. Todo rows remain in-memory and anonymous until a later persistence or user-owned Todo change. This avoids mixing authentication, persistence migration, and domain ownership in one step.

Alternative considered: convert Todo to PostgreSQL and attach `UserId` immediately. That is valuable, but it expands the first auth change beyond foundation scope.

### Include visible login and registration UI

The frontend foundation SHALL include visible login/register UI, not only API helpers. The UI should stay practical and system-oriented, using Element Plus controls and the existing Vue Composition API style.

Alternative considered: API helpers only. That would reduce scope, but it would leave the authentication foundation hard to exercise from the app itself.

## Risks / Trade-offs

- Database connection details may differ across developer machines -> keep local connection string in configuration and avoid hard-coding credentials in source.
- JWT storage in the browser has security trade-offs -> keep this foundation minimal and document that production hardening, refresh tokens, and XSS controls are future work.
- Identity introduces several generated tables -> rely on EF Core migrations and avoid manual schema edits.
- Tests that require PostgreSQL can become brittle -> unit/integration tests should use test-friendly configuration where possible and keep live DB requirements explicit.

## Migration Plan

1. Add backend auth and EF Core dependencies.
2. Add configuration for local PostgreSQL and JWT options.
3. Add `AppDbContext` and Identity model wiring.
4. Generate an initial EF Core migration for Identity schema.
5. Add auth endpoints and tests.
6. Add frontend auth API helpers and tests.
7. Run backend build/tests, frontend build/tests, and OpenSpec validation.

Rollback for local development is to drop the generated database tables or recreate the local database, then revert the code and migration files.

## Open Questions

- None.
