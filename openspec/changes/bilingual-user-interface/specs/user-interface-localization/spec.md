## ADDED Requirements

### Requirement: 預設繁體中文並支援英文
前端應（SHALL）提供 `zh-TW` 與 `en` 兩種介面語系；當瀏覽器尚未保存有效語系選擇時，系統必須（MUST）使用 `zh-TW`，且缺少翻譯 key 時必須（MUST）fallback 至 `zh-TW`。

#### Scenario: 首次開啟系統
- **WHEN** 使用者首次開啟前端且沒有已保存的語系
- **THEN** 導覽、頁面標題、操作、狀態與訊息以繁體中文顯示

#### Scenario: 翻譯 key 缺漏
- **WHEN** 目前語系缺少使用者可見文字的翻譯 key
- **THEN** 系統顯示對應的繁體中文內容而非直接顯示 key

### Requirement: 使用者可切換並保存介面語系
已登入與未登入的使用者皆應（SHALL）可從明確標示且可鍵盤操作的語言控制項切換繁體中文或英文；選擇必須（MUST）保存在目前瀏覽器，並於重新整理、登出及再次登入後恢復。

#### Scenario: 切換為英文
- **WHEN** 使用者從語言控制項選擇 English
- **THEN** 當前畫面的 application-owned 與 PrimeVue 文字立即切換為英文，且不需要重新登入

#### Scenario: 重新開啟已選語系
- **WHEN** 使用者選擇英文後重新整理或重新開啟前端
- **THEN** 系統從瀏覽器保存值恢復英文介面

#### Scenario: 保存值無效
- **WHEN** 瀏覽器保存的語系不屬於 `zh-TW` 或 `en`
- **THEN** 系統忽略該值並安全使用繁體中文

### Requirement: 全站使用者介面文字本地化
系統應（SHALL）將登入、Topbar、Sidebar、Dashboard、Projects、Issues、Kanban、Members、Settings、Users、Roles、Dialogs、表單驗證、toast、empty/loading/error states 與確認操作等 application-owned 文字集中於型別可維護的 locale resources，不得（MUST NOT）讓切換語系後的主要工作流程混用硬編碼英文。

#### Scenario: 使用繁體中文操作專案
- **WHEN** 使用者以繁體中文依序開啟專案、Issue 清單、Kanban 與 Issue 詳情
- **THEN** 所有頁面標題、導覽、篩選、按鈕、空狀態及 frontend-owned 回饋均以繁體中文顯示

#### Scenario: 使用英文操作管理功能
- **WHEN** Admin 切換英文並開啟使用者與角色管理
- **THEN** 管理導覽、欄位、操作、validation 與結果訊息均以英文顯示

### Requirement: Domain value 保持穩定並本地化顯示
系統必須（MUST）保留 API、route query 與 database 使用的 Project status、Issue status、type、priority、Project role、system role 與 permission key 原始值，並只在顯示層依目前語系轉換標籤。

#### Scenario: 中文介面篩選工作流程狀態
- **WHEN** 使用者在繁體中文介面選擇「進行中」篩選
- **THEN** 前端顯示中文標籤但送往 API 與 route query 的值仍為 `InProgress`

#### Scenario: 切換語系不改變資料
- **WHEN** 使用者在包含既有英文 enum values 的 Issue 頁面切換語系
- **THEN** 畫面標籤立即更新且不送出資料 mutation request

### Requirement: PrimeVue locale 跟隨介面語系
系統應（SHALL）為 `zh-TW` 與 `en` 提供對應的 PrimeVue locale 設定，並在切換時同步更新元件內建的 paginator、filter、selection、empty、calendar 與 accessibility 文字。

#### Scenario: 切換含 PrimeVue 元件的頁面
- **WHEN** 使用者在 DataTable、Select 或 Dialog 可見時切換語系
- **THEN** 元件內建文字與 application-owned 文字使用相同語系

### Requirement: Development demo data 提供獨立的繁體中文專案
Development demo seed 應（SHALL）保留既有英文 `WEB_Demo Development` Project，並以不同的 deterministic identifiers 新增一個描述 `WEB_Demo` 實際開發歷程的繁體中文 Project、Issues、comments、history 與 audit 展示資料；中文版必須（MUST）保留五種 workflow 狀態、labels、assigned work 與 overdue work 所需的代表性資料。

#### Scenario: 在新資料庫建立中文 demo data
- **WHEN** 開發者對新資料庫執行 `seed-demo-data --owner-email <email>`
- **THEN** 系統同時建立英文 `WEB_Demo Development` 與獨立的繁體中文 `WEB_Demo 開發專案`，兩者皆可供 Dashboard、List、Kanban、labels、comments 及 history 展示

#### Scenario: 在既有英文 demo data 上新增中文版
- **WHEN** 資料庫已存在英文 demo Project 且再次執行 seed command
- **THEN** 系統保留英文 Project 原有內容並以不同 identifiers 建立缺少的繁體中文 demo Project

#### Scenario: 保留使用者修改與自訂資料
- **WHEN** 任一 demo Project 的既有 seed record 已被使用者修改或含有額外自訂資料
- **THEN** seed command 不覆寫使用者修改、不刪除額外資料、不重複建立中英文 seed records，且仍維持可重複執行
