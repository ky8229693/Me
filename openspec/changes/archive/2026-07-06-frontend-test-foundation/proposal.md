## Why

前端目前只有 build 驗證，缺少可快速回歸的單元測試。`frontend-api-client-foundation` 已把 API 邊界抽出，現在適合建立 Vue/Vitest 測試基礎，先保護 API client 行為與核心 Todo UI flow。

## What Changes

- 新增前端單元測試工具鏈與 npm script。
- 使用 Vitest 作為 Vite 生態的 test runner。
- 使用 jsdom 提供 component tests 需要的 DOM environment。
- 使用 Vue Test Utils 測試 Vue component 行為。
- 新增 API layer 單元測試，涵蓋 `requestApi<T>()` 的 envelope parsing、HTTP failure、`success: false`、JSON request header。
- 新增少量 `App.vue` component tests，mock Todo API module 並驗證核心互動會呼叫對應 API function。
- 不建立 E2E tests、不導入 coverage threshold、不測真實 backend。

## Capabilities

### New Capabilities

- `frontend-test-foundation`: 定義 Vue frontend 的單元測試基礎、測試工具鏈、測試指令，以及第一批 API layer 和 component tests。

### Modified Capabilities

- None.

## Impact

- Affected code:
  - `src/WEB_Demo.Client/package.json`
  - `src/WEB_Demo.Client/vite.config.ts` or related Vitest config
  - New frontend test files under `src/WEB_Demo.Client/src`
- Dependencies:
  - Add dev dependencies for Vitest, jsdom, and Vue Test Utils.
- Systems:
  - Add a frontend test command such as `npm run test:unit`.
  - Frontend build should continue to pass with `npm run build`.
  - OpenSpec validation should continue to pass.
