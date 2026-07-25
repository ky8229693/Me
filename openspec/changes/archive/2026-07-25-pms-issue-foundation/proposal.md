## Why

Project foundation 已提供穩定的資料與權限邊界，但目前尚無法在專案內記錄、指派及追蹤 Feature、Bug 與 Change Request。建立第一版固定流程的 Issue 管理，可讓團隊開始用系統處理實際工作，並為後續文件／API 關聯、報表與 AI 知識庫累積結構化歷程。

## What Changes

- 新增 Project-scoped Issue，類型固定為 `Feature`、`Bug`、`ChangeRequest`。
- 新增標題、描述、優先級、建立人、負責人、期限與時間戳記。
- 新增固定狀態流程：`Draft` → `Approved` → `InProgress` → `QA` → `Done`，並支援受控退回／重新開啟。
- 新增 Issue 狀態與重要欄位變更歷程。
- 新增 Issue 留言，保留作者與建立時間。
- 新增 Project 內 Issue 清單、關鍵字搜尋、類型／狀態／優先級／負責人篩選及分頁。
- 沿用 Project membership：Owner／Manager 可管理全部 Issue；Member 可建立、留言及處理指派給自己的 Issue；Viewer 唯讀。
- Assignee 必須是目前的 Project member；封存 Project 的 Issue 全部唯讀。
- 本次不包含自訂 workflow、附件、通知、子任務、標籤、工時、看板、甘特圖、文件／API 關聯、SLA、報表或 AI。

## Capabilities

### New Capabilities

- `issue-management`: Project-scoped Issue 的資料模型、類型、優先級、建立、查詢、更新、搜尋篩選與分頁。
- `issue-workflow`: 固定狀態、角色與負責人可執行的狀態轉換，以及不可竄改的變更歷程。
- `issue-collaboration`: Project member 指派規則、留言與相關唯讀限制。

### Modified Capabilities

無。

## Impact

- Backend：新增 Issues feature 的 entities、EF Core migration、services、Minimal APIs 與 Project resource authorization 整合。
- Frontend：新增 Project Issue 清單、建立／編輯、篩選、詳情、狀態操作與留言介面。
- Database：新增 Issues、IssueHistory、IssueComments 與必要索引；所有資料直接關聯 ProjectId。
- Security：所有 Issue 查詢與異動都必須先通過 Project membership 與 Project status 檢查。
- Tests：新增 workflow、角色限制、跨 Project 隔離、查詢分頁、留言及封存唯讀的 backend、frontend 與 E2E 測試。
