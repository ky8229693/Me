## Why

The project is ready to move from an anonymous Todo demo toward real application access control. Authentication should be introduced before designing user-owned data so the database schema, API contracts, and frontend API client can evolve around a stable identity foundation.

## What Changes

- Add backend authentication foundation using ASP.NET Core Identity with EF Core.
- Use local PostgreSQL on port `5432` as the development database provider.
- Create database schema through EF Core migrations instead of manually creating login tables.
- Add registration, login, current-user, and logout/auth-state oriented API behavior under `/api`.
- Add JWT bearer authentication for API requests while keeping local development HTTP-only.
- Add frontend authentication API module and basic login state handling.
- Keep roles and permission policies minimal for this foundation; deeper authorization is deferred.

## Capabilities

### New Capabilities

- `auth-foundation`: Covers PostgreSQL-backed Identity persistence, authentication API behavior, JWT issuance/validation, and frontend auth client integration.

### Modified Capabilities

- None.

## Impact

- Backend adds EF Core, Npgsql provider, ASP.NET Core Identity, and JWT bearer authentication dependencies.
- Backend adds application database context, Identity user model if needed, migration files, authentication endpoints, and authentication service wiring.
- Configuration adds PostgreSQL connection string settings for local development.
- Frontend adds auth API calls and stores/uses authentication token state for API requests.
- Tests add backend coverage for authentication service/API behavior and frontend coverage for auth API client behavior.
