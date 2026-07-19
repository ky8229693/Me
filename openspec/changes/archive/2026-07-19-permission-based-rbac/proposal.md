## Why

目前系統已能建立 Role 並指派給帳號，但自訂 Role 不會授予任何實際 API 存取能力，授權仍集中在寫死的 `Admin` role。加入 Role–Permission 對應後，才能形成可管理、可測試且可延伸的基礎 RBAC 閉環。

## What Changes

- 建立由程式碼維護、具有穩定 key 的固定 Permission catalog，並由資料庫保存 Role–Permission 關聯。
- 提供受保護的 Permission 查詢與 Role Permission 設定 API，以及整合至 Role Management 的 PrimeVue 操作介面。
- 建立 Permission-based authorization policy／handler，讓 API 依所需 Permission 做後端授權。
- 讓 access JWT 與前端登入狀態包含目前有效 Permissions，供 API policy 與前端導覽／操作可見性判斷。
- Permission 異動後撤銷受影響帳號的 access sessions 與 refresh families，使新權限必須透過重新驗證或 refresh 取得。
- 將 `Admin` 保留為不可鎖死的 system role，永遠具備完整 Permission catalog；`User` 與自訂 Roles 則使用明確 Role–Permission mappings。
- 加入後端 service、authorization integration、前端 component/API 與 E2E 測試。
- 第一階段不包含 user-specific Permission override、Role 繼承、多租戶、時間條件或 resource ownership 授權。

## Capabilities

### New Capabilities
- `permission-management`: 定義固定 Permission catalog、Role–Permission persistence、管理 API、安全不變量、session 撤銷與前端管理操作。

### Modified Capabilities
- `authorization-foundation`: 將管理 API 的授權能力由固定 `AdminOnly` role policy 擴充為可重用的 Permission-based policies，同時保留必要的 system Admin 安全邊界。
- `auth-foundation`: access token、auth response 與目前使用者資料加入有效 Permission，並規範 Permission 異動後的 session／refresh 撤銷行為。
- `role-management`: Role 管理畫面與 Role 查詢資料擴充 Permission 設定入口與摘要資訊。

## Impact

- Backend：ASP.NET Core authorization registration、JWT claims、Identity/EF Core model、migration、Role administration endpoints/services、session revocation 與 shared contracts。
- Frontend：auth state/types、集中式 API client、navigation/action guards，以及 Role Management 頁面。
- Database：新增 Permission catalog／Role–Permission 關聯資料與對應 migration；啟動初始化流程需安全同步固定 catalog 與 system Admin 權限。
- Tests：後端 unit/integration tests、前端 unit tests與既有 PostgreSQL E2E 流程。
