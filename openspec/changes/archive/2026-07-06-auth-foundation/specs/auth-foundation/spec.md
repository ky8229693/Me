## ADDED Requirements

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
The backend SHALL expose authentication endpoints under `/api` that return `ApiResponse<T>` payloads while preserving appropriate HTTP status codes.

#### Scenario: User registration
- **WHEN** a valid registration request is submitted
- **THEN** the API MUST create an Identity user and return a successful `ApiResponse<T>` payload

#### Scenario: User login
- **WHEN** valid credentials are submitted
- **THEN** the API MUST return a successful `ApiResponse<T>` payload containing a bearer token and basic user information

#### Scenario: Invalid credentials
- **WHEN** invalid credentials are submitted
- **THEN** the API MUST return an unsuccessful response with an appropriate HTTP status code and backend message

#### Scenario: Current authenticated user
- **WHEN** an authenticated request asks for the current user
- **THEN** the API MUST return the current user's basic identity information

### Requirement: JWT bearer authentication protects authenticated API behavior
The backend SHALL validate JWT bearer tokens for endpoints that require an authenticated user.

#### Scenario: Missing token
- **WHEN** a request without a bearer token calls an authenticated endpoint
- **THEN** the API MUST reject the request as unauthorized

#### Scenario: Valid token
- **WHEN** a request with a valid bearer token calls an authenticated endpoint
- **THEN** the API MUST allow the request and resolve the authenticated user

### Requirement: Frontend auth client integrates with the API layer
The frontend SHALL provide authentication API helpers and token handling through the existing frontend API boundary.

#### Scenario: Login API call
- **WHEN** the frontend submits login credentials
- **THEN** it MUST call an auth API module instead of using direct `fetch` calls from UI components

#### Scenario: Authenticated API request
- **WHEN** the frontend has an access token and sends an authenticated API request
- **THEN** the shared API client MUST include the bearer token in the `Authorization` header

#### Scenario: Logout state clearing
- **WHEN** the frontend logout flow is triggered
- **THEN** local authentication state MUST be cleared so later API requests are sent without the previous token

### Requirement: Authentication foundation has focused test coverage
The project SHALL include backend and frontend tests for the authentication foundation behavior introduced by this change.

#### Scenario: Backend auth tests
- **WHEN** backend tests run
- **THEN** they MUST cover successful registration or login behavior and unauthorized access behavior

#### Scenario: Frontend auth API tests
- **WHEN** frontend unit tests run
- **THEN** they MUST cover auth API helper behavior and bearer token header behavior
