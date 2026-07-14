## 1. Roles-aware Auth 與 Token 撤銷

- [x] 1.1 新增集中管理的 security stamp claim type，並擴充 `AuthUser`／前端 auth types 回傳 roles
- [x] 1.2 修改 JWT token service，加入目前 Identity security stamp 與既有 roles claims
- [x] 1.3 在 JWT `OnTokenValidated` 載入目前使用者，拒絕不存在、停用或 security stamp 不符的 token
- [x] 1.4 修改 register、login 與 `/api/auth/me` 回傳目前 roles，且停用帳號登入維持通用錯誤訊息
- [x] 1.5 新增後端 integration tests，涵蓋 current roles、停用帳號與 security stamp 更新後舊 token 失效
- [x] 1.6 更新前端 auth API／state unit tests，涵蓋 roles user data 與重新登入狀態

## 2. Admin Bootstrap

- [x] 2.1 建立 bootstrap command arguments 與非敏感結果 contracts，支援隱藏互動式密碼輸入
- [x] 2.2 建立 `AdminBootstrapService`，處理新帳號建立、既有 User 提升、`User`／`Admin` roles 與 security stamp
- [x] 2.3 在 application entry point 加入 bootstrap command mode，確保一般 web 啟動不暴露匿名 bootstrap route
- [x] 2.4 新增 bootstrap tests，涵蓋新 Admin、既有 User 提升、輸入失敗、部分失敗回復與已有 Admin 時拒絕
- [x] 2.5 文件化本機與部署 bootstrap 指令、密碼輸入方式及安全限制

## 3. Admin 帳號查詢與狀態管理

- [x] 3.1 新增管理帳號的 request／response／pagination contracts，排除 Identity 敏感欄位
- [x] 3.2 建立 scoped account administration service，實作 Email 搜尋、排序、有限 page size、明細與頁面 roles 載入
- [x] 3.3 實作帳號啟用／停用、security stamp 更新、自我停用及最後啟用 Admin 保護
- [x] 3.4 新增 `/api/admin/users` Minimal API endpoints，套用 `AdminOnly` policy、shared envelope 與 400／404／409 status mapping
- [x] 3.5 新增後端 tests，涵蓋搜尋分頁、敏感資料排除、User 403、帳號不存在、停用／啟用與安全衝突

## 4. 固定角色指派

- [x] 4.1 在 administration service 實作完整 roles 集合驗證與冪等 add/remove 差異更新
- [x] 4.2 實作強制保留 `User`、拒絕未知 role、自我降級及最後啟用 Admin 保護
- [x] 4.3 使用 transaction 協調最後 Admin 檢查、role mutation 與 security stamp 更新，失敗時保持原 roles
- [x] 4.4 新增 `PUT /api/admin/users/{id}/roles` endpoint 與 shared validation／conflict responses
- [x] 4.5 新增後端 tests，涵蓋提升 Admin、冪等重送、未知 role、缺少 User、自我降級、最後 Admin 與舊 token 失效

## 5. PrimeVue 帳號管理頁

- [x] 5.1 新增集中式 Admin users API client 與 TypeScript contracts，涵蓋清單、狀態及 roles 更新
- [x] 5.2 擴充 authenticated state 與後台 navigation，僅對 Admin 顯示帳號管理入口
- [x] 5.3 使用 PrimeVue 建立帳號管理頁，包含 loading、搜尋、分頁、空資料、錯誤與角色／狀態顯示
- [x] 5.4 建立啟用／停用確認操作與角色編輯操作，成功後以後端回傳資料更新畫面
- [x] 5.5 新增前端 unit tests，涵蓋 API client、Admin navigation、清單狀態、狀態切換、角色更新及衝突錯誤

## 6. E2E、文件與驗證

- [x] 6.1 擴充 Playwright helper，建立可辨識且唯一的 Admin／User 測試資料，不在程式碼保存 production credentials
- [x] 6.2 新增 Admin 帳號管理 E2E happy path，驗證登入、搜尋帳號、停用／啟用與角色提升
- [x] 6.3 新增最小安全 E2E，驗證一般 User 看不到入口且直接管理 request 收到 403
- [x] 6.4 更新 `docs/memory.md`，記錄 bootstrap、帳號管理、token 撤銷、已知效能取捨與後續 Permission 待辦
- [x] 6.5 執行 `dotnet build` 與 `dotnet test WEB_Demo.sln`
- [x] 6.6 執行前端 unit tests、Playwright E2E tests 與 production build
- [x] 6.7 執行 OpenSpec validation 並修正所有錯誤
