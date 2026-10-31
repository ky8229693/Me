## 1. Backend contracts and configuration

- [x] 1.1 Add specification assistant request/response contracts and server-side LLM options with validation-safe defaults
- [x] 1.2 Add OpenSpec document and retrieval result models plus replaceable retriever and LLM client interfaces
- [x] 1.3 Register the specification assistant services and configuration without requiring a real credential at application startup

## 2. Retrieval and direct LLM integration

- [x] 2.1 Implement bounded OpenSpec Markdown discovery, section chunking, lexical scoring, and normalized source metadata
- [x] 2.2 Implement the OpenAI-compatible Chat Completions client with grounded prompts, timeout/cancellation, response parsing, and safe failures
- [x] 2.3 Implement the authenticated `/api/spec-assistant/query` endpoint with input validation, no-match behavior, `ApiResponse<T>`, and service-unavailable mapping

## 3. Frontend experience

- [x] 3.1 Add specification assistant API types and centralized API client call
- [x] 3.2 Add the authenticated specification assistant route and Workbench sidebar navigation with localized labels
- [x] 3.3 Build the PrimeVue specification assistant page with input, loading, answer, related-spec, source, empty, and retry states

## 4. Automated verification and documentation

- [x] 4.1 Add backend unit tests for lexical retrieval, path bounds, source metadata, no-match behavior, and direct LLM response/error handling
- [x] 4.2 Add backend integration tests for authentication, validation, successful grounded response, missing configuration, and safe upstream failures
- [x] 4.3 Add frontend API, navigation, and view tests covering successful, loading, validation, empty, and error states
- [x] 4.4 Document local LLM configuration using environment variables or user secrets without committing credentials
- [x] 4.5 Run affected backend tests/build, frontend tests/build, targeted Playwright flow if practical, and OpenSpec validation locally or CI
