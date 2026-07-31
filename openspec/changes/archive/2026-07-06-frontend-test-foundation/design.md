## Context

目前前端有 Vue 3、TypeScript、Vite、Element Plus，並已建立 `src/api/` API client foundation。現有驗證只有 `npm run build`，缺少可快速驗證 API client 與核心 component 行為的單元測試。

這個 change 建立第一批 frontend unit test foundation。範圍以低摩擦、高價值為主：先測 API layer 的純 TypeScript 行為，再補少量 `App.vue` component tests，避免一開始就被 Element Plus 的 DOM 細節拖住。

## Goals / Non-Goals

**Goals:**

- 加入 Vitest、jsdom、Vue Test Utils 作為前端單元測試基礎。
- 新增 `npm run test:unit` 指令。
- 測試 `requestApi<T>()` 的成功與失敗 envelope handling。
- 測試 JSON request body 會設定 `Content-Type: application/json`。
- 測試 `App.vue` 會呼叫 Todo API module 的核心 functions。
- 維持 `npm run build` 可通過。

**Non-Goals:**

- 不建立 Playwright 或瀏覽器 E2E tests。
- 不連線真實 backend。
- 不加入 coverage threshold。
- 不完整測試 Element Plus 每個子元件的內部 DOM。
- 不重構 UI 或 API client 行為。

## Decisions

### Use Vitest with jsdom

Vitest 與 Vite/Vue 專案整合成本低，支援 TypeScript、ESM 與 `vi.fn()` mocking。jsdom 提供 component tests 所需的 DOM environment。

Alternative considered: Jest. Jest 可行，但在 Vite + ESM 專案中通常需要更多轉譯與設定；Vitest 更貼近目前 stack。

### Use Vue Test Utils for component tests

Vue Test Utils 是 Vue component unit test 的標準工具。第一批 component tests 會 mock `src/api/todos`，只驗證 UI 行為是否呼叫正確 API function，以及必要的 loading/error/empty state。

Alternative considered: Testing Library for Vue. Testing Library 較接近使用者視角，但初期多一個 dependency 與查詢風格決策；目前先用 Vue Test Utils 即可。

### Focus API client tests first

`src/api/http.ts` 是後續前端 API 的共用邊界，測它比大量測單一 UI markup 更有回歸價值。測試應 mock `globalThis.fetch`，涵蓋成功 response、HTTP failure、`success: false`、JSON body header。

Alternative considered: only component tests. 只測 component 會讓 HTTP helper 的錯誤處理被間接覆蓋，debug 成本較高。

### Keep Element Plus friction low

`App.vue` 使用 Element Plus auto-import components。Component tests 可以選擇全域註冊 Element Plus，或 stub 掉非測試重點的 Element Plus components。第一批測試以可穩定驗證行為為優先，不依賴 Element Plus 內部 markup。

## Risks / Trade-offs

- 新增測試依賴需要安裝 npm packages → 使用 devDependencies，避免影響 production runtime bundle。
- Component tests 可能因 Element Plus DOM 變動而脆弱 → 優先 mock API module 與檢查可觀察行為，避免依賴內部 markup。
- jsdom 不等同真實瀏覽器 → 這批只做 unit/component tests；需要真實瀏覽器互動時另開 E2E foundation。
- 沒有 coverage threshold 可能無法量化測試完整度 → 初期先建立可維護測試習慣，coverage gate 待測試量穩定後再評估。
