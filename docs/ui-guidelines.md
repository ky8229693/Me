# UI Guidelines

本文件記錄 WEB_Demo 後台 UI 的實作原則。元件庫使用 PrimeVue，Sakai Vue 只作為 layout 與 CRUD interaction reference。

## Reference

- Sakai Vue repository：https://github.com/primefaces/sakai-vue
- Sakai Vue preview：https://sakai.primevue.org/
- PrimeVue theme：Aura

建立或調整後台 UI 前，優先查看 Sakai Vue 對應元件與版面寫法，再依本專案需求簡化，不直接複製整套 template。

## Layout

- 將 topbar、sidebar、menu 與主要內容拆成獨立元件，由上層 layout 集中管理導覽狀態。
- Sidebar 使用 layout state 搭配 CSS classes 控制。
- Desktop sidebar collapse 與 mobile overlay drawer 是不同語意，應使用分開的狀態。
- Desktop sidebar 收合時，主要內容同步擴展。
- Mobile sidebar 使用 overlay 與 mask，選擇選單或點擊 mask 後關閉。
- 響應式 breakpoint 與 transitions 應集中於 layout styles，避免散落在 feature views。

## Navigation and Authorization

- 導覽項目由資料模型產生，集中處理 id、label、icon 與 active state。
- Layout 可依目前使用者 roles 控制選單可見性。
- 隱藏選單只改善 UX，不是 authorization；API policy 始終是安全邊界。
- 新增 feature view 時應定義穩定的 navigation id，不使用顯示文字作為內部狀態值。

## PrimeVue CRUD Screens

- 常見控制項優先使用已安裝的 PrimeVue components，不重複建立自訂元件。
- 清單頁可依需求組合 toolbar、search、DataTable、Paginator、Tag 與 row actions。
- Mutation 操作應提供明確確認；可使用 Dialog、ConfirmDialog 或符合既有頁面的一致模式。
- 成功與失敗回饋使用 Toast 或頁面內訊息，並保留後端提供的可安全顯示錯誤語意。
- 每個資料畫面應處理 loading、empty、error 與 disabled states。
- Mutation 成功後優先使用後端回傳的最新資料更新畫面，避免前端自行推測 server state。

### Role Management

- Role Management 延續 Sakai Vue 的管理清單組合：Admin-only navigation、DataTable、Tag、InputText 與 row actions，再依本專案需求簡化。
- System roles 的 rename/delete 必須 disabled；使用中的 custom role 必須 disabled delete，並提示先到 User Management 移除指派。
- User Management 的 role options 必須由 Role API 載入；載入失敗時顯示錯誤並禁止儲存，不使用寫死清單 fallback。
- UI 應明示 custom role 本身不會自動授予 Permission；真正安全界線仍由後端 policy 決定。

## Visual Style

- 使用 PrimeVue styled mode、Aura theme 與現有 CSS variables。
- 介面保持實用、系統導向，不加入沒有需求的裝飾複雜度。
- 不預設導入 Sakai Vue 的完整 theme、layout configurator、額外 dependencies 或未使用頁面。
- 新增樣式前先確認 PrimeVue props、severity、spacing 與既有 utility classes 是否足夠。

## Testing

- 新增 UI interaction 時補 Vitest／Vue Test Utils tests。
- Layout tests 至少固定 toggle state class、navigation event 與授權顯示條件。
- Desktop 與 mobile 行為不同時，測試應分別驗證狀態轉換；瀏覽器層 responsive behavior 可由 Playwright 補充。
- Feature tests 應涵蓋 loading、empty、success、validation 與重要 conflict/error feedback。
