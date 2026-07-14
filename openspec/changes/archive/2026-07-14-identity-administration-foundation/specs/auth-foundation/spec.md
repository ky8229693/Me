## MODIFIED Requirements

### Requirement: Authentication APIs use the shared response envelope
The backend SHALL expose authentication endpoints under `/api` that return `ApiResponse<T>` payloads while preserving appropriate HTTP status codes and SHALL include the user's current roles in successful authentication user data.

#### Scenario: User registration
- **WHEN** a valid registration request is submitted
- **THEN** the API MUST create an Identity user, assign the default `User` role, and return a successful `ApiResponse<T>` payload containing the user's current roles

#### Scenario: User login
- **WHEN** valid credentials for an enabled account are submitted
- **THEN** the API MUST ensure an account without roles receives the default `User` role
- **THEN** the API MUST return a successful `ApiResponse<T>` payload containing a bearer token, basic user information, and current roles

#### Scenario: Invalid credentials
- **WHEN** invalid credentials are submitted
- **THEN** the API MUST return an unsuccessful response with an appropriate HTTP status code and backend message

#### Scenario: Disabled account login
- **WHEN** valid credentials for a disabled account are submitted
- **THEN** the API MUST reject login without disclosing whether the account is disabled

#### Scenario: Current authenticated user
- **WHEN** an authenticated request asks for the current user
- **THEN** the API MUST return the current user's basic identity information and current roles

### Requirement: JWT bearer authentication protects authenticated API behavior
The backend SHALL validate JWT bearer tokens for endpoints that require an authenticated user, SHALL include the user's current Identity roles and security stamp when issuing tokens, and SHALL reject tokens whose account state or security stamp is no longer current.

#### Scenario: Missing token
- **WHEN** a request without a bearer token calls an authenticated endpoint
- **THEN** the API MUST reject the request as unauthorized

#### Scenario: Valid current token
- **WHEN** a request with a valid bearer token for an enabled user has a security stamp matching the current Identity record
- **THEN** the API MUST allow the request and resolve the authenticated user

#### Scenario: Role claims are issued
- **WHEN** the backend issues a JWT for a user with one or more Identity roles
- **THEN** the token MUST contain a role claim for every assigned role

#### Scenario: Account is disabled after token issuance
- **WHEN** a token was issued before its Identity account was disabled
- **THEN** the backend MUST reject the token as unauthorized on the next protected request

#### Scenario: Security stamp changes after token issuance
- **WHEN** a token contains a security stamp that no longer matches the current Identity user
- **THEN** the backend MUST reject the token as unauthorized
