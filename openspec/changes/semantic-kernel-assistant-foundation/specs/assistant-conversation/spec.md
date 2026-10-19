## ADDED Requirements

### Requirement: Assistant conversations persist per user
The system SHALL persist assistant conversations and messages under the authenticated user's identity so the same conversation can resume after navigation, refresh, or API restart.

#### Scenario: User resumes a conversation
- **WHEN** an authenticated user reopens a conversation they own
- **THEN** the system returns its messages, current context, and any active pending action

#### Scenario: User requests another user's conversation
- **WHEN** an authenticated user requests a conversation owned by another user
- **THEN** the system responds as though the conversation does not exist

### Requirement: Conversation maintains explicit work context
The system SHALL store an authorized project or issue context separately from model-generated text and SHALL allow the user to remove that context.

#### Scenario: Valid project page context is attached
- **WHEN** the client supplies a project identifier the authenticated user can access
- **THEN** the assistant stores and visibly reports that project as the conversation scope

#### Scenario: Unauthorized context is supplied
- **WHEN** the client supplies a project or issue the authenticated user cannot access
- **THEN** the system rejects or ignores that context without exposing protected data

### Requirement: Pending actions have a controlled lifecycle
The system MUST persist pending write actions with an owner, validated payload, preview, expiration, status, and idempotency identity, and MUST allow no more than one active pending action per conversation.

#### Scenario: New action conflicts with an existing pending action
- **WHEN** a conversation already has an active pending action and the user requests another write action
- **THEN** the assistant asks the user to confirm or cancel the existing action before preparing the new one

#### Scenario: Confirmation is submitted more than once
- **WHEN** confirmation for the same action is retried or submitted concurrently
- **THEN** the system executes the underlying mutation at most once and returns the authoritative result
