## Why

目前前端只有單層導覽，Users、Roles 與 Projects 沒有依領域分類；Project 詳情又以大型 Dialog 承載 Members 與 Issues，Issue 詳情再開第二層 Dialog。這讓使用者難以辨識所在層級、無法以網址返回或分享工作內容，也無法合理容納後續文件、API、報表與 AI 模組。

## What Changes

- 導入 Vue Router，以 URL 作為 Dashboard、管理功能、Project workspace 與 Issue 詳情的導覽狀態來源。
- 將全站側欄分為「工作台」、「專案管理」與「系統管理」，並依既有權限隱藏不可存取項目。
- 將 Project 清單與 Project workspace 分離；點擊 Project 導航到完整頁面，不再開啟 Project Dialog。
- 在 Project workspace 提供 Overview、Issues、Members、Settings 的固定 contextual navigation 與 breadcrumb。
- 將 Issue 清單留在 Project workspace，Issue 詳情改為具獨立 URL 的完整頁面。
- 保留 Dialog 僅用於短暫且可快速完成的操作，例如建立、確認與簡短選擇；禁止主要詳情頁形成 nested modal。
- 保留現有 Project、membership 與 Issue API 行為，不改變 resource authorization 規則。
- 暫不加入看板、甘特圖、文件中心、API 中心、報表或 AI 導覽項目；等相應 capability 實作時再加入 workspace。

## Capabilities

### New Capabilities

- `application-navigation`: URL-based routing、權限感知的分組側欄、breadcrumb、直接開啟與重新整理恢復導覽狀態。
- `project-workspace-navigation`: Project 完整工作區、Overview／Issues／Members／Settings contextual navigation，以及封存與角色狀態的頁面呈現。
- `issue-page-navigation`: Project-scoped Issue 清單與具獨立 URL 的 Issue 詳情頁、返回清單行為及 Dialog 使用邊界。

### Modified Capabilities

無。

## Impact

- Frontend dependencies：新增並設定 Vue Router。
- Frontend architecture：重構 `App.vue`、layout menu model、ProjectsView 與 IssueWorkspace，新增 route-level Project／Issue views。
- UX：既有主功能由 in-memory `activeView` 改為 URL；Project／Issue 詳情不再由 Dialog 表示。
- Security：導覽可見性仍屬 UX，API 與 Project membership 繼續作為安全邊界。
- Backend／Database：預期不需 API 或 schema 變更。
- Tests：更新既有 component tests，新增 router、deep-link、權限導覽及 modal boundary 測試。
