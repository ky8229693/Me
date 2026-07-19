## MODIFIED Requirements

### Requirement: Authentication APIs use the shared response envelope
The backend SHALL expose authentication endpoints under `/api` that return `ApiResponse<T>` payloads while preserving appropriate HTTP status codes, SHALL include the user's current roles and effective permissions in successful authentication user data, and SHALL manage refresh credentials only through an HttpOnly cookie.

#### Scenario: User registration
- **WHEN** a valid registration request is submitted
- **THEN** the API MUST create an Identity user, assign the default `User` role, create a refresh family and return a successful payload containing a registered access token, current roles and effective permissions
- **THEN** the API MUST set the refresh token in an HttpOnly cookie and MUST NOT include it in the JSON body

#### Scenario: User login
- **WHEN** valid credentials for an enabled account are submitted
- **THEN** the API MUST ensure an account without roles receives the default `User` role
- **THEN** the API MUST create a refresh family and return a registered access token, basic user information, current roles and effective permissions
- **THEN** the API MUST set the refresh token in an HttpOnly cookie and MUST NOT include it in the JSON body

#### Scenario: Invalid credentials
- **WHEN** invalid credentials are submitted
- **THEN** the API MUST return an unsuccessful response with an appropriate HTTP status code and backend message

#### Scenario: Disabled account login
- **WHEN** valid credentials for a disabled account are submitted
- **THEN** the API MUST reject login without disclosing whether the account is disabled

#### Scenario: Current authenticated user
- **WHEN** an authenticated request with an active Memory access session asks for the current user
- **THEN** the API MUST return the current user's basic identity information, current roles and effective permissions

#### Scenario: Refresh authenticated user
- **WHEN** a valid refresh cookie is rotated successfully
- **THEN** the API MUST return a new registered access token and the user's current identity information, roles and effective permissions

#### Scenario: Idempotent logout
- **WHEN** client submits logout with or without a valid access token
- **THEN** the API MUST revoke the refresh family when identifiable, revoke related Memory sessions, clear the cookie and return a shared success response

### Requirement: JWT bearer authentication protects authenticated API behavior
The backend SHALL validate short-lived JWT bearer tokens for protected endpoints, SHALL include current Identity roles, effective permissions, a unique `jti` and refresh family `sid`, and SHALL reject tokens whose Memory access session is not active.

#### Scenario: Missing token
- **WHEN** a request without a bearer token calls an authenticated endpoint
- **THEN** the API MUST reject the request as unauthorized

#### Scenario: Valid current token
- **WHEN** a request provides a cryptographically valid, unexpired bearer token whose `jti`, `sid` and user id match an active Memory entry
- **THEN** the API MUST allow the request and resolve identity, roles and permissions from token claims without Identity database lookup

#### Scenario: Role and Permission claims are issued
- **WHEN** backend issues an access JWT
- **THEN** token MUST contain a role claim for every current assigned role
- **THEN** token MUST contain one permission claim for every distinct effective Permission key resolved from those roles

#### Scenario: Access token reaches configured lifetime
- **WHEN** access token exceeds the configured default 15-minute lifetime
- **THEN** protected endpoint MUST reject it unless client first obtains a new access token through refresh

#### Scenario: Security-relevant user state changes
- **WHEN** a user's enabled state, roles, role names, effective Role Permissions or security stamp changes successfully
- **THEN** backend MUST revoke all Memory access sessions and persisted refresh families for affected user

### Requirement: Frontend auth client integrates with the API layer
The frontend SHALL keep access tokens only in JavaScript memory, SHALL retain the authenticated user's roles and effective permissions in runtime auth state, SHALL recover sessions through the HttpOnly refresh cookie, and SHALL coordinate a single refresh with at most one retry per failed business request.

#### Scenario: Login API call
- **WHEN** the frontend submits login credentials
- **THEN** it MUST use the auth API module, include cookie credentials, save returned access token only in memory and retain returned effective permissions in runtime state

#### Scenario: Page reload session recovery
- **WHEN** application starts without an in-memory access token
- **THEN** frontend MUST attempt refresh before deciding whether to show authenticated UI
- **THEN** successful recovery MUST restore current roles and effective permissions
- **THEN** old `web_demo.auth_token` localStorage data MUST be removed and MUST NOT be reused

#### Scenario: Authenticated API request
- **WHEN** frontend has an in-memory access token and sends an authenticated request
- **THEN** the shared API client MUST include the bearer token in the `Authorization` header

#### Scenario: Automatic token renewal
- **WHEN** eligible business request receives 401
- **THEN** shared client MUST join or start one refresh operation and retry original request at most once
- **THEN** successful refresh MUST replace runtime roles and effective permissions with the response values

#### Scenario: Logout state clearing
- **WHEN** the frontend logout flow is triggered
- **THEN** auth module MUST call backend logout with cookie credentials
- **THEN** local in-memory authentication, role and permission state MUST be cleared whether backend logout succeeds or fails

