## Why

目前前端的 API response 型別、`fetch` 呼叫、envelope parsing 與 Todo CRUD 全部集中在 `App.vue`，畫面元件同時負責 UI 與 API 邊界細節。隨著前端功能增加，先建立薄型 API client foundation 可以讓後續功能有一致的 request、error 與型別入口。

## What Changes

- 新增前端 API client 基礎結構，集中處理 `/api` request 與 `ApiResponse<T>` envelope。
- 將 Todo API 呼叫從 `App.vue` 抽出到專用 API module。
- 將共用 API 型別與 Todo 型別移到可重用的 TypeScript module。
- 保留目前 Todo UI 與使用者行為，不重做畫面。
- 保留原生 `fetch`，不新增 axios、Pinia、auth token interceptor、retry/cache/cancellation 等尚未需要的架構。

## Capabilities

### New Capabilities

- `frontend-api-client-foundation`: 定義前端 API client 基礎，涵蓋共用 envelope parsing、HTTP/error handling、Todo API module 與 UI 呼叫邊界。

### Modified Capabilities

- None.

## Impact

- Affected code:
  - `src/WEB_Demo.Client/src/App.vue`
  - New files under `src/WEB_Demo.Client/src/api/`
- APIs:
  - Uses existing backend endpoints only:
    - `GET /api/todos`
    - `POST /api/todos`
    - `PATCH /api/todos/{id}/toggle`
    - `DELETE /api/todos/{id}`
- Dependencies:
  - No new npm dependencies.
- Systems:
  - Frontend build should continue to pass with `npm run build`.
