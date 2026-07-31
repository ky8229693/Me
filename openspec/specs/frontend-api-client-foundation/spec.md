## Purpose

Define the frontend API client foundation for shared backend communication and Todo API isolation.

## Requirements

### Requirement: Shared API client handles response envelopes
The frontend SHALL provide a shared API client helper that sends HTTP requests to backend `/api` routes and parses the shared `ApiResponse<T>` envelope.

#### Scenario: Successful API response
- **WHEN** a frontend API module calls a backend endpoint that returns a successful `ApiResponse<T>`
- **THEN** the shared API client MUST return the typed `data` payload to the caller

#### Scenario: Failed HTTP or envelope response
- **WHEN** a backend endpoint returns a non-success HTTP status or an `ApiResponse<T>` with `success` set to false
- **THEN** the shared API client MUST surface a consistent error using the backend message when available

### Requirement: Todo API calls are isolated from UI components
The frontend SHALL expose Todo API functions from a dedicated API module instead of calling `fetch` directly from `App.vue`.

#### Scenario: Todo list loading
- **WHEN** the Todo UI loads todo items
- **THEN** it MUST call a Todo API module function instead of directly calling `fetch('/api/todos')`

#### Scenario: Todo mutations
- **WHEN** the Todo UI creates, toggles, or deletes a todo item
- **THEN** it MUST call Todo API module functions for the corresponding backend endpoints

### Requirement: Frontend API types are reusable
The frontend SHALL define reusable TypeScript types for shared API responses and Todo API data used by the client.

#### Scenario: UI imports Todo data type
- **WHEN** a UI component needs the Todo item shape returned by the backend
- **THEN** it MUST import the reusable Todo type from the API layer instead of declaring a duplicate local type

### Requirement: Existing Todo user behavior is preserved
The frontend SHALL preserve existing Todo UI behavior while moving API transport details out of the component.

#### Scenario: Todo workflow remains available
- **WHEN** a user views, creates, toggles, refreshes, or deletes todo items
- **THEN** the UI MUST continue to use the existing backend endpoints and show equivalent success, loading, empty, and error states

### Requirement: API client foundation avoids new runtime dependencies
The frontend SHALL implement the initial API client foundation without adding a new HTTP client, state management, authentication, retry, cache, or cancellation dependency.

#### Scenario: Frontend dependencies remain stable
- **WHEN** the API client foundation is implemented
- **THEN** `package.json` MUST NOT add axios, Pinia, or other new runtime dependencies for this foundation
