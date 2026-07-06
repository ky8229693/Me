## 1. Test Tooling Setup

- [x] 1.1 Add Vitest, jsdom, and Vue Test Utils as frontend devDependencies.
- [x] 1.2 Add a frontend `test:unit` npm script for running unit tests.
- [x] 1.3 Configure Vitest to use the Vue plugin, TypeScript, and jsdom DOM environment.

## 2. API Client Unit Tests

- [x] 2.1 Add tests for successful `requestApi<T>()` envelope parsing.
- [x] 2.2 Add tests for non-success HTTP responses using the backend error message.
- [x] 2.3 Add tests for `ApiResponse<T>` responses where `success` is false.
- [x] 2.4 Add tests that verify JSON request bodies set `Content-Type: application/json` when no content type is provided.

## 3. Todo Component Tests

- [x] 3.1 Add `App.vue` component tests that mock the Todo API module.
- [x] 3.2 Verify initial mount calls `getTodos`.
- [x] 3.3 Verify creating a todo calls `createTodo` with the entered title.
- [x] 3.4 Verify toggle and delete interactions call the corresponding Todo API module functions.

## 4. Verification and Documentation

- [x] 4.1 Confirm frontend testing packages are listed only in `devDependencies`.
- [x] 4.2 Run the frontend unit test command.
- [x] 4.3 Run `npm run build` in `src/WEB_Demo.Client`.
- [x] 4.4 Validate the OpenSpec change with `npm run spec:validate frontend-test-foundation` or the project-equivalent validation command.
- [x] 4.5 Update project memory or developer documentation with the frontend unit test command if useful.
