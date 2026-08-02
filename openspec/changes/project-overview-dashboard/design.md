## Context

Project workspace 已載入 Project 與 members，但 Overview 尚未讀取議題摘要。既有 Issue 資料已包含 status、priority、assignee、due date、created/updated timestamps 與 labels；專案存取由 membership 控制。此功能跨越 API 查詢、contract、前端 API client、Overview UI、i18n 與測試。

## Goals / Non-Goals

**Goals:**

- 以單一 API request 提供專案總覽所需的聚合資料。
- 在 Overview 清楚呈現 KPI、完成率、狀態分布、近期與即將到期議題。
- 沿用既有授權、issue workflow、雙語與 responsive UI 規則。
- 避免新增資料表、第三方圖表套件或前端逐頁抓取全部議題。

**Non-Goals:**

- 自訂 Dashboard widgets、日期區間或統計篩選器。
- 趨勢圖、burn-down、工時、通知或跨專案比較。
- 背景預計算、快取、materialized view 或新的資料庫 schema。

## Decisions

### 提供專用的 project overview read endpoint

新增 `GET /api/projects/{projectId}/overview`，由後端在既有 Project membership 範圍內聚合資料並回傳專用 contract。相較由前端呼叫 issue list 後自行統計，專用 endpoint 不受分頁影響、降低 request 數，且讓 KPI 定義保持一致。

### 明確定義摘要資料

- `totalCount`：專案全部議題數。
- `inProgressCount`：狀態為 `InProgress` 的議題數。
- `completedCount`：狀態為 `Done` 的議題數。
- `overdueCount`：具有 due date、尚未 `Done`，且 due date 早於 API 查詢時間的議題數。
- `completionRate`：`completedCount / totalCount * 100`，無議題時為 0，由後端回傳整數百分比。
- `statusCounts`：固定 workflow 每個狀態皆回傳，即使數量為 0。
- `recentIssues`：依 `UpdatedAt` 由新到舊取前 5 筆。
- `upcomingIssues`：未完成、due date 不早於查詢時間，依 due date 由近到遠取前 5 筆。

後端回傳穩定的 enum/value 與 UTC timestamp；顯示文字和日期格式由前端依目前語系處理。

### 使用既有 PrimeVue 與 CSS 呈現

KPI 使用卡片，完成率使用 PrimeVue ProgressBar，狀態分布使用輕量 CSS bar/list，議題清單使用可點擊的 RouterLink。這能沿用 Aura theme，且不為小型資料視覺化引入 chart dependency。

### 保留既有 Overview 資訊

Project status、目前角色、member count 與 quick links 保留並重新編排，使新版仍包含原有工作區資訊。載入摘要失敗時顯示可重試錯誤，不讓錯誤 KPI 冒充 0。

## Risks / Trade-offs

- [每次開啟 Overview 都即時計算統計，議題量大時查詢成本增加] → 以 server-side projection、count 與 top-5 query 限制資料量；有實際效能問題後再加 index 或 cache。
- [以 API server 現在時間判定逾期可能與瀏覽器時區觀感不同] → due date 持續使用既有 UTC 儲存語意，前端只負責在目前 locale 顯示。
- [狀態分布用 CSS 而非圖表套件，互動性較少] → 第一版聚焦可讀性與零新 dependency；趨勢或互動圖表另立 change。
- [Project workspace 與 overview endpoint 可能短暫先後完成] → Overview 自行管理 loading/error state，route 切換時只呈現目前 projectId 的回應。

## Migration Plan

1. 新增 read contract、service query 與授權 endpoint，無 database migration。
2. 新增後端 unit/integration tests。
3. 擴充前端 API client、i18n 與 Overview UI，保留現有 route。
4. 新增前端 unit test 與專案 Overview E2E。
5. 若需回退，只需移除新 endpoint 與 UI；既有資料不受影響。

## Open Questions

無；第一版統計定義與顯示上限已固定，後續需求另以 OpenSpec 擴充。
