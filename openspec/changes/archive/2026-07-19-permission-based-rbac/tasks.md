## 1. Permission Domain 與 Persistence

- [x] 1.1 盤點現有 Users、Roles、Todos 與管理 endpoints，定義第一批穩定 Permission keys、分類、display name 及 endpoint 對照表
- [x] 1.2 建立集中式 Permission catalog 與 key constants，加入重複 key、命名格式及未知 key fail-closed 的 unit tests
- [x] 1.3 建立 Permission 與 RolePermission EF Core entities、DbContext mappings、唯一鍵及外鍵行為
- [x] 1.4 產生並檢查 PostgreSQL migration，驗證升級與保留既有 Identity user/role 資料
- [x] 1.5 實作冪等 catalog initializer 與 system Admin 全權同步／解析行為，補 initializer integration tests

## 2. Permission Resolution 與 Authorization

- [x] 2.1 實作依使用者 Roles 解析去重有效 Permissions 的 service，涵蓋 Admin 隱含全權、User、自訂 Role 與未知 catalog 狀態
- [x] 2.2 建立 Permission claim type、requirement、dynamic policy provider 與 authorization handler，註冊至既有 ASP.NET Core pipeline
- [x] 2.3 為 Permission policy 補齊真實 authorization pipeline integration tests，涵蓋成功、403、401、未知 key 與 `AdminOnly` 保留行為
- [x] 2.4 依 endpoint 對照逐一將一般管理 APIs 改用 Permission policies，保留明確需要 system Admin 的高風險操作

## 3. Token 與 Session Security

- [x] 3.1 擴充 JWT issuance，在 login、register 與 refresh 時計算並寫入去重 Permission claims
- [x] 3.2 擴充 AuthUser／AuthResponse contracts 與 `/api/auth/me`，回傳目前有效 Permissions
- [x] 3.3 更新 backend auth tests，驗證 token claims、response payload、refresh 後 Permission 更新與舊 token fail-closed
- [x] 3.4 規範部署切換時的既有 session 撤銷流程，確保沒有 Permission claims 的舊 token 不會取得 Permission-protected 存取

## 4. Role Permission 管理 API

- [x] 4.1 實作 Permission catalog、Role Permission 查詢 contracts、services 與 shared-envelope endpoints
- [x] 4.2 實作以完整集合冪等更新 Role Permissions 的 transaction 流程，涵蓋不存在 Role、未知 key、重複提交與 rollback
- [x] 4.3 保護 system Admin 不可削減完整 Permissions，並為衝突與資料不變性加入 service/integration tests
- [x] 4.4 Role Permission 異動成功時更新所有受影響 users 的 security stamps，並撤銷其 Memory sessions 與 refresh families
- [x] 4.5 擴充 Role 查詢 payload，提供有效 Permission 摘要且不暴露 Identity／persistence 內部欄位

## 5. Frontend Auth 與 Permission UX

- [x] 5.1 擴充 TypeScript auth types 與 runtime auth state，於 login、refresh、startup recovery 與 logout 正確維護 Permissions
- [x] 5.2 建立集中式 `hasPermission` 判斷並將導覽、頁面與操作可見性由寫死 Admin role 漸進改為 Permission keys
- [x] 5.3 擴充 Permission／Role API client 與錯誤處理，禁止載入失敗時使用寫死 fallback catalog
- [x] 5.4 在 Role Management 使用 PrimeVue 實作 catalog 分組、Permission 摘要、完整集合編輯、確認及成功／失敗回饋
- [x] 5.5 對 system Admin 顯示完整且不可削減的 Permissions，並對唯讀或未授權狀態提供明確 disabled／hidden UX
- [x] 5.6 補 frontend unit tests，涵蓋 auth Permission state、navigation/action guards、Role Permission 編輯與 API error state

## 6. End-to-End 驗證與文件

- [x] 6.1 新增 backend persistence 與 management integration tests，使用隔離 PostgreSQL 驗證 mappings、transaction、撤銷與 shared responses
- [x] 6.2 新增 Playwright 情境：Admin 配置自訂 Role、帳號重新登入取得新 Permissions、允許授權操作並拒絕未授權操作
- [x] 6.3 執行 `dotnet test WEB_Demo.sln`、API `dotnet build`、frontend unit tests 與 `npm run build`，修正所有 regressions
- [x] 6.4 執行 OpenSpec validate／doctor 並更新開發文件，記錄 Permission key 命名、endpoint 對照與 Admin break-glass 原則

