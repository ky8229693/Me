## 1. Semantic Kernel foundation

- [x] 1.1 Select and pin the compatible Semantic Kernel packages and map existing SpecAssistant model configuration into a Kernel chat-completion registration
- [x] 1.2 Add assistant orchestration options, bounded function-call execution, safe error mapping, and dependency-injection registration
- [x] 1.3 Add function invocation filters for authorization, telemetry, result-size limits, and mutation-tool blocking

## 2. Persistent conversation state

- [x] 2.1 Add conversation, message, context, pending action, and confirmation result entities with ownership, expiration, status, and idempotency constraints
- [x] 2.2 Add EF Core mappings and migration without changing existing PMS or pgvector data
- [x] 2.3 Implement conversation services for create, list, load, append, context update, bounded model history, and user isolation
- [x] 2.4 Implement atomic pending-action preparation, cancellation, expiration, and exactly-once confirmation lifecycle

## 3. Assistant plugins and orchestration

- [x] 3.1 Wrap existing RAG retrieval and grounded answering as the specification search plugin with normalized sources
- [x] 3.2 Add authorized project search and project-creation preparation plugin functions that reuse existing project validation
- [x] 3.3 Add authorized issue-creation preparation and assignable-user search plugin functions that return missing fields and ambiguity explicitly
- [x] 3.4 Implement the single message orchestration service with automatic tool selection, sequential tool results, missing-field follow-up, and structured response state
- [x] 3.5 Implement confirmation execution through existing ProjectAdministrationService and IssueAdministrationService with permission revalidation and idempotent results

## 4. Assistant API

- [x] 4.1 Add authenticated endpoints for conversation creation, listing, loading, messaging, context update, and cancellation
- [x] 4.2 Add explicit pending-action confirm endpoint and ensure mutation completion can only be reported from its structured result
- [x] 4.3 Preserve the existing assistant endpoints during migration and document the compatibility／rollback boundary

## 5. Global assistant interface

- [x] 5.1 Add typed frontend conversation, message, context, pending-action, confirmation, and tool-state API contracts
- [x] 5.2 Add a Topbar assistant trigger and AppLayout-owned PrimeVue Drawer with responsive full-width mobile behavior and accessible focus management
- [x] 5.3 Implement shared assistant conversation state so Drawer close, navigation, refresh, and Workspace transitions preserve the active conversation
- [ ] 5.4 Render page context, tool activity, follow-up questions, validated preview cards, confirm／cancel controls, errors, and authoritative completion results
- [x] 5.5 Upgrade the existing Spec Assistant route into the full Assistant Workspace while retaining RAG sources and index controls
- [ ] 5.6 Add localized Traditional Chinese and English assistant, accessibility, confirmation, and error copy

## 6. Verification and documentation

- [ ] 6.1 Add backend unit tests for plugin schemas, missing fields, ambiguity, tool limits, permission filters, context authorization, and safe errors
- [ ] 6.2 Add backend integration tests for conversation isolation／persistence, multi-step orchestration, pending-action conflicts, expiration, and exactly-once confirmation
- [ ] 6.3 Add frontend unit tests for Drawer／Workspace shared state, responsive behavior, context removal, follow-up messages, previews, confirmation, and failure recovery
- [ ] 6.4 Add a targeted Playwright scenario covering project creation followed by Issue preparation through one conversation
- [x] 6.5 Update development documentation for Semantic Kernel configuration, plugin safety rules, conversation storage, rollback, and model-provider limitations
- [ ] 6.6 Run affected backend tests/build, frontend tests/build, targeted E2E, migration checks, and OpenSpec validation locally or in CI as required
