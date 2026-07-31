## Context

目前 Vue client 只有單一 `App.vue`，但該檔同時包含 UI state、Element Plus message、`ApiResponse<T>` 型別、`fetch` 呼叫、response envelope parsing 與 Todo CRUD。這在 demo 初期可接受，但後續只要新增更多 API 或畫面，就會讓 UI component 持續累積 API 邊界細節。

本 change 建立最小可用的 frontend API client foundation，讓畫面元件只呼叫 domain-level API functions，並把 HTTP/envelope/error handling 集中在一處。

## Goals / Non-Goals

**Goals:**

- 建立 `src/api/` 作為前端 API 邊界。
- 集中 `ApiResponse<T>` parsing 與 HTTP failure handling。
- 抽出 Todo API functions，讓 `App.vue` 不再直接呼叫 `fetch`。
- 保留目前 Todo UI、使用者操作流程與後端 endpoint contract。
- 保持實作簡單，不新增 npm dependency。

**Non-Goals:**

- 不導入 axios 或其他 HTTP client dependency。
- 不導入 Pinia/global store。
- 不設計 authentication token injection、401 refresh flow 或 authorization handling。
- 不實作 retry、cache、request cancellation、pagination 或 query builder。
- 不重做 UI、不調整後端 API、不改變 `ApiResponse<T>` envelope contract。

## Decisions

### Use native `fetch` with a small wrapper

保留 native `fetch`，新增 `requestApi<T>()` 之類的薄 wrapper 處理共用流程：

- 送出 `/api/...` request。
- 對 JSON body 設定 `Content-Type: application/json`。
- 解析後端 `ApiResponse<T>` envelope。
- 當 HTTP status 不成功或 envelope `success` 為 false 時丟出一致錯誤。

Alternative considered: axios. 目前需求只有 JSON CRUD 與 envelope parsing，axios 會增加依賴與設定，但沒有明顯收益。

### Separate shared API types from feature API modules

建議結構：

```text
src/api/
  types.ts
  http.ts
  todos.ts
```

- `types.ts`：共用 `ApiResponse<T>`、API error 型別，以及 Todo DTO 型別。
- `http.ts`：共用 request helper 與 envelope parsing。
- `todos.ts`：Todo-specific API functions。

Alternative considered: put everything in one `api.ts`. 目前檔案數較少可以運作，但分成 shared http 與 feature module 更容易隨功能增加而延伸。

### Keep UI state in `App.vue`

`App.vue` 仍保留 `todos`、`newTitle`、`isLoading`、`errorMessage` 與 Element Plus message。這次只移除 API transport 與 envelope parsing，避免同時導入 store 或重構 UI。

Alternative considered: Pinia store. 現在只有一個畫面，global store 會增加架構重量；等多頁面或跨元件共享狀態出現時再導入。

### Error handling remains user-facing but lightweight

HTTP wrapper 應盡量保留後端回傳的 `message` 與 `errorCode`，讓 UI 可以顯示合理錯誤。初期只需要丟出 `Error` 或簡單 `ApiError`，不需要完整錯誤分類系統。

Alternative considered: rich error hierarchy. 目前還沒有 auth、validation summary 或 domain-specific error flow，過早細分會讓實作複雜化。

## Risks / Trade-offs

- `fetch` wrapper 過薄，未涵蓋未來 auth/retry/cancellation → 保持 API surface 小，之後可在 `http.ts` 擴充，不影響 UI component。
- TypeScript 型別只描述 frontend 期望，不會自動驗證 runtime payload → 目前沿用既有後端 contract；若未來資料複雜化，再評估 schema validation。
- 將 Todo DTO 放在前端 module 會和後端 model 手動同步 → 現階段可接受；等 API surface 擴大後再評估 code generation 或 shared contract strategy。
- 不導入 store 代表 state 仍在 `App.vue` → 這符合目前單頁 demo；未來有跨畫面共享需求再建立 frontend state foundation。
