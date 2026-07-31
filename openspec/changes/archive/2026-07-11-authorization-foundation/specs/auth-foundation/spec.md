## MODIFIED Requirements

### Requirement: Authentication APIs use the shared response envelope
The backend SHALL expose authentication endpoints under `/api` that return `ApiResponse<T>` payloads while preserving appropriate HTTP status codes.

#### Scenario: User registration
- **WHEN** a valid registration request is submitted
- **THEN** the API MUST create an Identity user, assign the default `User` role, and return a successful `ApiResponse<T>` payload

#### Scenario: User login
- **WHEN** valid credentials are submitted
- **THEN** the API MUST ensure an account without roles receives the default `User` role
- **THEN** the API MUST return a successful `ApiResponse<T>` payload containing a bearer token and basic user information

#### Scenario: Invalid credentials
- **WHEN** invalid credentials are submitted
- **THEN** the API MUST return an unsuccessful response with an appropriate HTTP status code and backend message

#### Scenario: Current authenticated user
- **WHEN** an authenticated request asks for the current user
- **THEN** the API MUST return the current user's basic identity information

### Requirement: JWT bearer authentication protects authenticated API behavior
The backend SHALL validate JWT bearer tokens for endpoints that require an authenticated user and SHALL include the user's current Identity roles when issuing tokens.

#### Scenario: Missing token
- **WHEN** a request without a bearer token calls an authenticated endpoint
- **THEN** the API MUST reject the request as unauthorized

#### Scenario: Valid token
- **WHEN** a request with a valid bearer token calls an authenticated endpoint
- **THEN** the API MUST allow the request and resolve the authenticated user

#### Scenario: Role claims are issued
- **WHEN** the backend issues a JWT for a user with one or more Identity roles
- **THEN** the token MUST contain a role claim for every assigned role
