## 1. Backend contracts and draft workflow

- [x] 1.1 Add project-draft request/response contracts and a strict LLM project-draft parser
- [x] 1.2 Add a user-bound, expiring, one-time in-memory project draft store
- [x] 1.3 Add protected draft and confirmation endpoints that reuse ProjectAdministrationService

## 2. Frontend chat experience

- [x] 2.1 Add typed API functions for creating and confirming project drafts
- [x] 2.2 Extend the specification assistant page with draft preview, cancel, confirm, and created-project feedback
- [x] 2.3 Add Traditional Chinese and English UI copy for the controlled workflow

## 3. Verification

- [x] 3.1 Add backend tests for parsing, draft ownership, expiration, one-time confirmation, and authorization behavior
- [x] 3.2 Add frontend API and view tests for preview, cancellation, confirmation, and failure feedback
- [x] 3.3 Run affected backend tests/build, frontend tests/build, and OpenSpec validation locally
