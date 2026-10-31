# Project Instructions

## Language

- Default to Traditional Chinese for user-facing replies.
- Keep explanations concise and practical unless the user asks for deeper detail.

## Project Stack

- Backend: ASP.NET Core Web API on .NET 8 LTS.
- Frontend: Vue 3 + TypeScript + Vite.
- UI library: PrimeVue with `primeicons` and `@primeuix/themes`.
- PrimeVue is registered manually in `src/WEB_Demo.Client/src/main.ts` with the Aura theme.
- OpenSpec is used for spec-driven development.
- Development currently uses HTTP only.

## Local Development URLs

- API: `http://localhost:5186`
- Swagger: `http://localhost:5186/swagger`
- Frontend: `http://127.0.0.1:5173`
- Vite proxy maps `/api` to `http://localhost:5186`.

## Commands

Backend:

```powershell
cd src/WEB_Demo.Api
dotnet run
```

Frontend:

```powershell
cd src/WEB_Demo.Client
npm install
npm run dev
```

Validation:

```powershell
cd src/WEB_Demo.Api
dotnet build
```

```powershell
cd src/WEB_Demo.Client
npm run build
```

### Local vs CI Validation

- Use risk-based local validation during implementation; do not automatically duplicate the entire CI workflow after every OpenSpec change.
- Always run the build, unit tests, integration tests, or targeted E2E specs directly affected by the change.
- If no E2E spec or shared E2E infrastructure changed, leave unaffected E2E coverage to CI unless the application change itself requires a targeted local Playwright check.
- When adding or modifying an E2E spec, run that affected spec locally at least once before considering it complete.
- When changing shared E2E infrastructure such as the runner, authentication helpers, database lifecycle, global setup, Playwright configuration, or Docker integration, run the complete local E2E suite because the change can affect every spec.
- Run a targeted local Playwright spec when changing cross-frontend/backend flows, authentication, routing, database behavior, or workflow behavior. Leave the remaining unchanged E2E suite to CI unless broad regression risk justifies a local full run.
- Run local Compose config and image builds only when changing Dockerfiles, Compose, Nginx, environment interpolation, production build behavior, or deployment configuration. For ordinary application features, the CI `Containers` job is the required verification boundary.
- Before merge, require the GitHub Actions `Backend`, `Frontend`, `OpenSpec`, `E2E`, and `Containers` checks to pass. A check may be accepted from CI instead of being rerun locally.
- OpenSpec verification tasks should say "local or CI" when either environment provides equivalent evidence; do not leave a change incomplete solely because an unchanged CI-owned check was not duplicated locally.

Tests:

```powershell
dotnet test WEB_Demo.sln
```

OpenSpec:

```powershell
npm run spec:doctor
npm run spec:list
npm run spec:list-specs
npm run spec:validate
npm run spec:view
```

## Development Workflow

- Prefer OpenSpec SDD for new features or behavior changes:
  1. Propose the change.
  2. Define requirements and acceptance criteria.
  3. Validate the spec.
  4. After the spec is complete and valid, proceed directly to implementation unless a blocker or material user decision remains; do not wait for a second confirmation.
  5. Verify with build/tests.
- For small mechanical fixes, direct implementation is acceptable.
- Keep changes scoped and aligned with the existing stack.
- Do not introduce HTTPS unless explicitly requested.
- Prefer simple architecture first; add abstractions only when they reduce real complexity.

## OpenSpec Notes

- For deeper project history and decision context, read `docs/memory.md` when planning architecture or resuming after a long pause.
- OpenSpec content may be written in Traditional Chinese, but keep required OpenSpec structural markers in English:
  - `## ADDED Requirements`
  - `### Requirement:`
  - `#### Scenario:`
  - `- **WHEN**`
  - `- **THEN**`
- Keep change IDs and capability IDs in English kebab-case, for example `api-foundation`.
- Current API foundation spec is `openspec/changes/api-foundation`.
- Current active API test foundation spec is `openspec/changes/api-test-foundation`.
- `api-foundation` intentionally covers only the first API foundation batch:
  1. API structure split into endpoints/services/contracts-style folders.
  2. Centralized exception handling.
  3. CORS origins moved to `appsettings`.
  4. ASP.NET Core Health Checks.
- `api-foundation` also preserves existing behavior such as `ApiResponse<T>`, Swagger UI, `/` to `/swagger`, and HTTP-only local development.
- Defer EF Core/database, authentication/authorization, JWT/Identity, roles/permissions, Serilog, large DTO restructuring, and controller migration to separate future specs.
- `api-test-foundation` covers only the first backend testing batch: xUnit test project, `InMemoryTodoService` unit tests, a few API integration tests, and test command documentation.
- Defer frontend unit tests, E2E tests, database tests, authentication tests, and coverage thresholds to later specs.

## Backend Conventions

- Keep API routes under `/api`.
- Use ASP.NET Core Minimal APIs until the project grows enough to justify controllers.
- Keep `Program.cs` focused on service registration, middleware, Swagger, CORS, and endpoint module wiring.
- Register feature routes through `Endpoints/*Endpoints.cs` extension classes.
- Put feature logic and in-memory demo state in `Services/`.
- Put cross-cutting middleware in `Middleware/`.
- Put configuration option classes in `Options/`.
- Read CORS allowed origins from `appsettings.*.json` under `Cors:AllowedOrigins`.
- Return API payloads with the shared `ApiResponse<T>` envelope while preserving proper HTTP status codes.
- Use ASP.NET Core Health Checks for `/api/health`.
- Keep API tests under `tests/WEB_Demo.Api.Tests`.
- Use xUnit, FluentAssertions, and `Microsoft.AspNetCore.Mvc.Testing` for backend tests.
- Use Entity Framework Core when persistence is introduced.
- SQL Server is the default database unless the user chooses another database.

## Frontend Conventions

- Use Vue 3 Composition API with TypeScript.
- Use PrimeVue for common UI controls before creating custom components.
- Use Sakai Vue as the admin/dashboard layout reference for sidebar, topbar, tables, and CRUD screens: `https://github.com/primefaces/sakai-vue`.
- Do not copy the whole Sakai template by default; adapt layout ideas incrementally into the existing Vue + Vite client.
- Keep UI practical and system-oriented rather than marketing-style.
- Avoid decorative complexity unless the user asks for a more polished visual design.
- Keep API calls centralized once the frontend grows beyond simple examples.

## Tooling Notes

- .NET SDK 8.0.422 was installed through winget.
- If `dotnet` is not found, restart the terminal or VS Code so PATH refreshes.
- Node.js 24.18.0 LTS was installed through winget.
