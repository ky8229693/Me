## ADDED Requirements

### Requirement: Authenticated user can generate a project draft from conversation
The system SHALL allow an authenticated user with project creation permission to submit a natural-language project request and receive a structured project draft without creating persistent project data.

#### Scenario: Valid project request
- **WHEN** an authorized user submits text from which a valid project name and optional description can be extracted
- **THEN** the system returns a user-bound project draft with a unique identifier, preview fields, and expiration time

#### Scenario: Model cannot produce a valid draft
- **WHEN** the model is unavailable or its output does not satisfy the project draft contract
- **THEN** the system returns a safe error and does not create a project or draft with unvalidated data

### Requirement: Project creation requires explicit confirmation
The system MUST create a project only after the owning user explicitly confirms a valid, unexpired draft, and MUST execute creation through the existing project administration service.

#### Scenario: Owner confirms a valid draft
- **WHEN** the owning user confirms an unexpired draft and still has project creation permission
- **THEN** the system creates exactly one project using the previewed fields and returns the created project

#### Scenario: Draft is confirmed more than once
- **WHEN** a user confirms a draft that has already been successfully consumed
- **THEN** the system rejects the request without creating another project

#### Scenario: Another user confirms the draft
- **WHEN** a different authenticated user attempts to confirm a draft
- **THEN** the system responds as though the draft does not exist and creates no project

#### Scenario: Draft has expired
- **WHEN** the owning user attempts to confirm an expired draft
- **THEN** the system rejects the request and creates no project

### Requirement: Chat interface exposes controlled project creation
The specification assistant page SHALL let users request a project draft, inspect its name and description, and explicitly confirm or cancel it.

#### Scenario: User previews and confirms a project
- **WHEN** the user requests a project draft and selects confirm on the returned preview
- **THEN** the interface shows progress and then displays the created project result

#### Scenario: User cancels a project draft
- **WHEN** the user selects cancel on a draft preview
- **THEN** the interface removes the pending preview without calling the confirmation endpoint
