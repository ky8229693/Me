## Why

目前系統已有帳號、認證、系統角色與 Permission-based RBAC，但尚未具備承載 PMS 業務資料的專案邊界。先建立專案、成員與專案存取控制，可讓後續 Issue、文件、API、資產、報表及 AI 知識庫都沿用一致的資料隔離模型，並以小步方式持續擴充。

## What Changes

- 新增專案建立、查詢、更新與封存能力。
- 新增專案成員管理，讓既有系統使用者可加入多個專案。
- 採固定的專案角色 `Owner`、`Manager`、`Member`、`Viewer`，作為第一版專案層級授權模型。
- 將 `ProjectId` 設為後續 PMS 業務資料、搜尋、報表與 AI 檢索的主要隔離邊界。
- 建立關鍵專案及成員異動的操作歷程。
- 明確採用單一公司內部部署模式，不建立 Tenant、TenantId、租戶切換、計費或多租戶 SaaS 功能。
- 本次不包含 Issue、文件版本、OpenAPI 匯入、資產／Secret、AI RAG、動態報表及自訂工作流程。

## Capabilities

### New Capabilities

- `project-management`: 專案生命週期、基本資料、封存及專案清單存取行為。
- `project-membership`: 專案成員、固定專案角色、資源層級授權及成員異動歷程。

### Modified Capabilities

無。

## Impact

- Backend：新增 Projects feature 的 endpoints、contracts、services、資料模型、EF Core migration 與 resource-based authorization。
- Frontend：新增專案清單、專案基本資料與專案成員管理畫面，並依專案存取權限控制操作。
- Database：PostgreSQL 新增 projects、project memberships 與相關 audit records；不新增 tenant schema。
- Tests：新增專案服務、授權、API integration、前端元件及必要 E2E 測試。
- 後續功能必須以 Project 作為資料與權限邊界，但本 change 不實作後續 PMS 模組。
