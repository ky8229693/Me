## 1. Configuration Decisions

- [x] 1.1 Confirm local PostgreSQL database name `web_demo`, username `postgres`, password `1234`, and port `5432`.
- [x] 1.2 Decide that this foundation includes visible login/register UI.
- [x] 1.3 Decide that registration is open in local development.

## 2. Backend Dependencies and Configuration

- [x] 2.1 Add EF Core, Npgsql provider, Identity, and JWT bearer authentication packages to `WEB_Demo.Api`.
- [x] 2.2 Add PostgreSQL connection string and JWT option settings to API configuration.
- [x] 2.3 Add strongly typed options for JWT settings under `Options/`.

## 3. Identity Persistence

- [x] 3.1 Add application database context for Identity persistence.
- [x] 3.2 Add application user model only if the default Identity user is not sufficient.
- [x] 3.3 Register EF Core PostgreSQL and ASP.NET Core Identity services in `Program.cs`.
- [x] 3.4 Generate the initial EF Core migration for Identity schema.
- [x] 3.5 Document or verify the command used to apply migrations to the local PostgreSQL database.

## 4. Authentication API

- [x] 4.1 Add authentication request/response contracts.
- [x] 4.2 Add token issuing service for signed JWT creation.
- [x] 4.3 Add `Endpoints/AuthEndpoints.cs` for registration, login, and current-user routes under `/api`.
- [x] 4.4 Configure authentication and authorization middleware in the correct pipeline order.
- [x] 4.5 Ensure auth endpoints return `ApiResponse<T>` payloads with appropriate HTTP status codes.

## 5. Frontend Integration

- [x] 5.1 Add reusable auth API types to the frontend API layer.
- [x] 5.2 Add `src/WEB_Demo.Client/src/api/auth.ts` with register, login, and current-user calls.
- [x] 5.3 Extend the shared API client to include a bearer token when an auth token is available.
- [x] 5.4 Add logout/auth-state clearing behavior.
- [x] 5.5 Add visible login/register UI using Vue Composition API and Element Plus.

## 6. Tests

- [x] 6.1 Add backend tests for successful registration or login behavior.
- [x] 6.2 Add backend tests for unauthorized access to an authenticated endpoint.
- [x] 6.3 Add frontend unit tests for auth API helper behavior.
- [x] 6.4 Add frontend unit tests for bearer token header behavior.

## 7. Verification and Documentation

- [x] 7.1 Run `dotnet build` for the API.
- [x] 7.2 Run `dotnet test WEB_Demo.sln`.
- [x] 7.3 Run frontend unit tests.
- [x] 7.4 Run frontend build.
- [x] 7.5 Validate the OpenSpec change.
- [x] 7.6 Update `docs/memory.md` or README with PostgreSQL/auth development notes if useful.
