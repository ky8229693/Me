## 1. API Module Structure

- [x] 1.1 Create `src/WEB_Demo.Client/src/api/` for frontend API boundary code.
- [x] 1.2 Add reusable API and Todo TypeScript types in `src/WEB_Demo.Client/src/api/types.ts`.

## 2. Shared HTTP Client

- [x] 2.1 Implement a native `fetch`-based shared request helper in `src/WEB_Demo.Client/src/api/http.ts`.
- [x] 2.2 Handle `ApiResponse<T>` envelope parsing, HTTP failures, unsuccessful envelope responses, and backend messages consistently.
- [x] 2.3 Ensure JSON requests set `Content-Type: application/json` when a request body is provided.

## 3. Todo API Module

- [x] 3.1 Add `src/WEB_Demo.Client/src/api/todos.ts` with `getTodos`, `createTodo`, `toggleTodo`, and `deleteTodo`.
- [x] 3.2 Use existing backend routes without changing endpoint paths or payload contracts.

## 4. UI Integration

- [x] 4.1 Refactor `App.vue` to import Todo types and API functions from `src/api/`.
- [x] 4.2 Remove direct `fetch` usage and local `ApiResponse<T>` parsing from `App.vue`.
- [x] 4.3 Preserve existing loading, empty, success, and error UI behavior.

## 5. Verification

- [x] 5.1 Confirm `package.json` has no new runtime dependencies for this foundation.
- [x] 5.2 Run `npm run build` in `src/WEB_Demo.Client`.
- [x] 5.3 Validate the OpenSpec change with `npm run spec:validate frontend-api-client-foundation` or the project-equivalent validation command.
