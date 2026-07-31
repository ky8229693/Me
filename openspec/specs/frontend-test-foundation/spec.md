## Purpose

Define the frontend unit testing foundation for Vue, TypeScript, shared API client tests, and core Todo UI component tests.

## Requirements

### Requirement: Frontend unit test command
The frontend SHALL provide a unit test command that runs Vue and TypeScript unit tests through the Vite test stack.

#### Scenario: Run frontend unit tests
- **WHEN** a developer runs the frontend unit test command
- **THEN** the test runner MUST execute frontend unit tests in a DOM-capable test environment

#### Scenario: Unit test command is documented in package scripts
- **WHEN** a developer reads the frontend `package.json`
- **THEN** a unit test script MUST be available without requiring an ad hoc command

### Requirement: Shared API client has unit test coverage
The frontend SHALL include unit tests for the shared API client helper.

#### Scenario: Successful API envelope
- **WHEN** the mocked backend returns HTTP success with an `ApiResponse<T>` where `success` is true
- **THEN** the API client test MUST verify that the typed `data` payload is returned

#### Scenario: Failed HTTP response
- **WHEN** the mocked backend returns a non-success HTTP status with an error envelope
- **THEN** the API client test MUST verify that the helper throws an error using the backend message when available

#### Scenario: Failed envelope response
- **WHEN** the mocked backend returns HTTP success with an `ApiResponse<T>` where `success` is false
- **THEN** the API client test MUST verify that the helper throws an error using the backend message when available

#### Scenario: JSON request body
- **WHEN** the API client sends a request with a JSON body
- **THEN** the API client test MUST verify that `Content-Type: application/json` is set when not already provided

### Requirement: Todo UI has component test coverage
The frontend SHALL include component tests for core Todo UI behavior without calling the real backend.

#### Scenario: Initial Todo load
- **WHEN** the Todo component is mounted in a unit test
- **THEN** the test MUST verify that the Todo API module is called to load todo items

#### Scenario: Todo creation interaction
- **WHEN** the Todo creation flow is triggered in a unit test
- **THEN** the test MUST verify that the Todo API module is called with the entered title

#### Scenario: Todo mutation interactions
- **WHEN** Todo toggle or delete interactions are triggered in unit tests
- **THEN** the tests MUST verify that the corresponding Todo API module functions are called

### Requirement: Frontend tests avoid production dependencies
The frontend SHALL add testing packages as development dependencies only.

#### Scenario: Test packages are development-only
- **WHEN** frontend test dependencies are added
- **THEN** they MUST be listed under `devDependencies` and MUST NOT add new runtime dependencies

### Requirement: Frontend verification remains healthy
The frontend SHALL keep existing build verification passing after the test foundation is added.

#### Scenario: Build and unit tests pass
- **WHEN** the frontend test foundation is implemented
- **THEN** both the frontend build command and frontend unit test command MUST pass
