## Context

前端目前以 Vue 3 Composition API、TypeScript、PrimeVue Aura theme 建置，所有使用者可見文字分散在 layout、views、feature components 與 API error handling 中，尚無 locale store 或翻譯資源。Domain values（例如 `InProgress`、`Critical`、`Owner`）同時用於 API contracts、route query 與畫面顯示，不適合直接改成中文。Development demo seed 已使用 deterministic identifiers 與非破壞性 idempotent 寫入，因此新增中文版必須使用獨立 identifiers，並保留既有英文與使用者資料的安全承諾。

## Goals / Non-Goals

**Goals:**

- 以繁體中文作為首次使用的預設介面，完整支援英文切換與瀏覽器保存。
- 集中管理 application-owned translations，讓新增頁面可沿用一致的 key hierarchy 與 domain label helpers。
- 讓 PrimeVue 內建文案與目前介面語系同步。
- 保留 API、database、authorization、route 與 query 的穩定 machine values。
- 保留英文 demo dataset，另提供描述本專案開發內容的繁體中文 deterministic demo dataset，兩者可安全並存。

**Non-Goals:**

- 不翻譯使用者自行輸入的 Project、Issue、comment 或其他 business data。
- 不翻譯 Swagger、API JSON payload、server logs 或資料庫 machine values。
- 不加入第三種語系、後端 request culture negotiation、帳號層級 locale 欄位或 database schema。
- 不在此 change 重構全部 frontend state management 或導入 Pinia。

## Decisions

### 使用 `vue-i18n` Composition API 與模組化 locale resources

新增單一 i18n bootstrap module，提供 `zh-TW` 與 `en` message trees，依 layout、auth、dashboard、projects、issues、administration、common 與 domain 分區。元件使用 `useI18n()`，非元件 helper 接收 translator 或由明確的 localization helper 處理。

選擇 `vue-i18n` 是因其 Vue 3 Composition API、pluralization、參數插值與 runtime locale switching 均成熟；自行製作字典函式雖少一個 dependency，但很快會重複處理 fallback、插值與 reactive update。

### 預設 `zh-TW`，使用 localStorage 保存，不自動依瀏覽器切換

locale allowlist 固定為 `zh-TW` 與 `en`，storage key 使用 `web_demo.locale`。初始化先讀取 allowlisted storage value，否則使用 `zh-TW`；不依 `navigator.language` 自動選擇，確保主要繁體中文使用情境及 E2E 結果一致。切換後同步更新 i18n locale、PrimeVue locale、`document.documentElement.lang` 與 localStorage。

替代方案是保存到使用者資料庫，但這需要 schema/API/account setting 且無法涵蓋登入前頁面，本 change 不採用。

### 在 Topbar 提供文字清楚的語言選擇器

使用 PrimeVue Select 或 Menu 呈現「繁體中文／English」，具備 accessible label，登入頁亦提供同一控制項。Topbar 在 desktop 與 mobile 都保留入口，避免只使用國旗或無文字 icon 所造成的辨識與 accessibility 問題。

### Domain values 透過集中 helper 翻譯

建立依類別映射的 translation keys，例如 `domain.issueStatus.InProgress`。Select option 的 `value` 永遠保留 machine value，僅 `label` 本地化；Tag、table、history 與 detail view 使用相同 helper。未知值採安全 fallback 顯示原值，避免後端新增 enum 時 UI 空白。

不直接修改 API enum/string，因為那會破壞 route query、filters、tests 與持久化資料相容性。

### PrimeVue locale 由同一 locale controller 更新

建立兩份 PrimeVue locale objects，初始化及每次切換時透過 PrimeVue configuration 更新。application locale 與 component locale 由同一個 setter 控制，避免兩者不同步。

### API errors 採穩定 UI 訊息與安全 fallback

Frontend-owned validation、empty/loading、success 與 operation failure summary 全部使用 translation keys。對目前未提供 stable error code 的 API detail，UI 可顯示本地化 operation summary，並在需要診斷時附上 server detail；不以脆弱的完整英文句子比對作為長期翻譯機制。未來若需要逐項翻譯 server validation，應另行設計 stable API error codes。

### 中文 seed catalog 使用獨立 deterministic graph

既有英文 catalog、GUID、relationships 與內容保持不變。新增中文 catalog 使用另一組固定 Project、label、Issue、comment 與 history GUID，但沿用相同的狀態分布與相對日期策略。seed service 在同一 relational transaction 依序確保兩份 graph：

1. 缺少的英文或中文 deterministic record 依各自 catalog 建立。
2. 已存在的 deterministic record 不以另一語系覆寫，也不修改使用者變更過的文字。
3. 中英文 Project 使用相同指定 Owner，但具有獨立 membership、labels 與 Issues。
4. 額外 Projects、Issues、labels、joins、comments 與 history永不刪除。
5. 重跑 command 不新增重複資料，summary 分別回報兩份 demo projects 已就緒。

此方案讓使用者可直接比較英文與繁體中文測試資料，也完全避免將既有英文 demo project 轉換後失去原展示內容。

## Risks / Trade-offs

- [翻譯 key 數量大，容易遺漏硬編碼文字] → 使用 `rg` audit、missing-key warning、locale parity test 與主要 route component tests。
- [PrimeVue locale API 與 vue-i18n 更新時機不同] → 封裝單一 `setLocale`，測試切換後兩者與 `<html lang>` 同步。
- [API server detail 仍可能是英文] → 主要 UI feedback 顯示本地化 summary；stable API error code 留待後續 change。
- [中英文資料量增加可能讓 Dashboard 顯得重複] → 兩份資料使用清楚不同的 Project 名稱，並維持代表性但精簡的 Issue 數量。
- [全寬中文標籤可能比英文更長或更短] → 沿用 responsive layout，針對 Topbar、filters、dialogs 與 mobile navigation 做 targeted component/E2E 驗證。

## Migration Plan

1. 安裝並註冊 i18n，加入 locale controller、resources、domain helpers 與 PrimeVue locale。
2. 先轉換 layout、auth 與 common 元件，再依 Dashboard、Projects、Issues、Administration 分批移除硬編碼文字。
3. 更新 unit test mount helpers，使測試元件預設載入 i18n；新增 locale parity 與 switch persistence tests。
4. 新增中文 seed catalog 與雙 graph idempotency tests，對本機 Development DB 重跑 seed command以新增中文版並保留英文版。
5. 執行受影響 unit/backend tests與 production builds；語言切換核心流程補 targeted E2E，完整矩陣由 CI 執行。

若需 rollback，可將 UI 固定回 `zh-TW` 並移除 selector；machine values 與 schema 未變更。新增的中文 demo data 屬 Development 展示內容；英文 demo data 與正式資料不受影響。

## Open Questions

- 無；第一版固定支援 `zh-TW` 與 `en`，預設 `zh-TW`，並以瀏覽器 localStorage 保存。
