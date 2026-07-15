## Purpose

Define the authentication foundation for PostgreSQL-backed ASP.NET Core Identity, JWT bearer authentication, and frontend authentication integration.

## Requirements

### Requirement: PostgreSQL-backed Identity schema
The backend SHALL use EF Core with PostgreSQL to persist ASP.NET Core Identity authentication data through migrations.

#### Scenario: Initial authentication schema creation
- **WHEN** the authentication foundation migration is applied to the local PostgreSQL database on port `5432`
- **THEN** the database MUST contain the Identity tables required for users, roles, claims, logins, and tokens

#### Scenario: Manual login tables are avoided
- **WHEN** authentication persistence is implemented
- **THEN** the project MUST create authentication tables through EF Core migrations instead of handwritten database table scripts

### Requirement: Authentication configuration is environment-based
The backend SHALL read PostgreSQL connection settings and JWT settings from configuration instead of hard-coding them in endpoint or service code.

#### Scenario: Local PostgreSQL configuration
- **WHEN** the API starts in local development
- **THEN** it MUST be able to read a PostgreSQL connection string targeting localhost port `5432`

#### Scenario: JWT settings configuration
- **WHEN** JWT authentication is configured
- **THEN** issuer, audience, signing key, and token lifetime settings MUST be read from configuration

### Requirement: Authentication APIs use the shared response envelope
The backend SHALL expose authentication endpoints under `/api` that return `ApiResponse<T>` payloads while preserving appropriate HTTP status codes, SHALL include the user's current roles in successful authentication user data, and SHALL register each issued bearer token as an active server-side session.

#### Scenario: User registration
- **WHEN** a valid registration request is submitted
- **THEN** the API MUST create an Identity user, assign the default `User` role, and return a successful `ApiResponse<T>` payload containing the user's current roles
- **THEN** the returned bearer token MUST have an active Memory allowlist session

#### Scenario: User login
- **WHEN** valid credentials for an enabled account are submitted
- **THEN** the API MUST ensure an account without roles receives the default `User` role
- **THEN** the API MUST return a successful `ApiResponse<T>` payload containing a bearer token, basic user information, and current roles
- **THEN** the returned bearer token MUST have an active Memory allowlist session

#### Scenario: Invalid credentials
- **WHEN** invalid credentials are submitted
- **THEN** the API MUST return an unsuccessful response with an appropriate HTTP status code and backend message

#### Scenario: Disabled account login
- **WHEN** valid credentials for a disabled account are submitted
- **THEN** the API MUST reject login without disclosing whether the account is disabled

#### Scenario: Current authenticated user
- **WHEN** an authenticated request with an active session asks for the current user
- **THEN** the API MUST return the current user's basic identity information and current roles

#### Scenario: Authenticated logout
- **WHEN** an authenticated user submits logout with an active JWT session
- **THEN** the API MUST revoke the current `jti` session and return a successful shared response

### Requirement: JWT bearer authentication protects authenticated API behavior
The backend SHALL validate JWT bearer tokens for endpoints that require an authenticated user, SHALL include the user's current Identity roles and a unique `jti` when issuing tokens, and SHALL reject tokens whose server-side session is not active.

#### Scenario: Missing token
- **WHEN** a request without a bearer token calls an authenticated endpoint
- **THEN** the API MUST reject the request as unauthorized

#### Scenario: Valid current token
- **WHEN** a request provides a cryptographically valid, unexpired bearer token whose `jti` and user id match an active Memory allowlist entry
- **THEN** the API MUST allow the request and resolve identity and roles from the token claims

#### Scenario: Role claims are issued
- **WHEN** the backend issues a JWT for a user with one or more Identity roles
- **THEN** the token MUST contain a role claim for every assigned role

#### Scenario: Account is disabled after token issuance
- **WHEN** a token was issued before its Identity account was disabled
- **THEN** the backend MUST revoke all sessions for that user before returning mutation success
- **THEN** the backend MUST reject the old token as unauthorized on the next protected request

#### Scenario: Security-relevant user state changes after token issuance
- **WHEN** a user's enabled state, roles, role names, or security stamp changes successfully
- **THEN** the backend MUST revoke every active session for the affected user
- **THEN** the backend MUST reject each old token as unauthorized on the next protected request

### Requirement: Frontend auth client integrates with the API layer
The frontend SHALL provide authentication API helpers and token handling through the existing frontend API boundary, and SHALL notify the backend when the current session logs out.

#### Scenario: Login API call
- **WHEN** the frontend submits login credentials
- **THEN** it MUST call an auth API module instead of using direct `fetch` calls from UI components

#### Scenario: Authenticated API request
- **WHEN** the frontend has an access token and sends an authenticated API request
- **THEN** the shared API client MUST include the bearer token in the `Authorization` header

#### Scenario: Logout state clearing
- **WHEN** the frontend logout flow is triggered
- **THEN** the auth API helper MUST attempt an authenticated backend logout request
- **THEN** local authentication state MUST be cleared whether backend logout succeeds, returns unauthorized, or fails because of network connectivity

### Requirement: Authentication foundation has focused test coverage
The project SHALL include backend and frontend tests for the authentication foundation behavior introduced by this change.

#### Scenario: Backend auth tests
- **WHEN** backend tests run
- **THEN** they MUST cover successful registration or login behavior and unauthorized access behavior

#### Scenario: Frontend auth API tests
- **WHEN** frontend unit tests run
- **THEN** they MUST cover auth API helper behavior and bearer token header behavior
