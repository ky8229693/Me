# Project Memory

最後更新：2026-07-10

本文件記錄專案目前狀態、重要決策、已完成的 OpenSpec change，以及後續工作。開發規範以 `AGENTS.md` 為準；本文件補充專案脈絡與歷史。

## 專案概況

- 專案名稱：`WEB_Demo`
- 後端：ASP.NET Core Web API on .NET 8 LTS
- 前端：Vue 3 + TypeScript + Vite
- UI：PrimeVue、`primeicons`、`@primeuix/themes`（Aura theme）
- 資料庫：PostgreSQL
- 開發模式：HTTP only
- API：`http://localhost:5186`
- Swagger：`http://localhost:5186/swagger`
- Frontend：`http://127.0.0.1:5173`
- Vite proxy：`/api` 轉送至 `http://localhost:5186`

## 已確認決策

- API routes 維持 `/api/*`，根路徑 `/` redirect 至 `/swagger`。
- API 暫時使用 Minimal APIs；等規模確實需要時再評估 controllers。
- API response 使用共用 `ApiResponse<T>` envelope，同時保留正確的 HTTP status code。
- `Program.cs` 聚焦 service registration、middleware、Swagger、CORS 與 endpoint wiring。
- CORS allowed origins 從 `appsettings.*.json` 的 `Cors:AllowedOrigins` 讀取。
- 健康檢查使用 ASP.NET Core Health Checks，路由為 `/api/health`。
- 開發環境維持 HTTP only，不主動導入 HTTPS。
- PostgreSQL 是預設資料庫；local 使用 DB `web_demo`、port `5432`、帳號 `postgres`、密碼 `1234`。
- 認證使用 ASP.NET Core Identity、EF Core、PostgreSQL 與 JWT；資料表透過 EF Core migrations 建立。
- Local registration 開放，前端提供登入及註冊 UI。
- 前端使用 Vue 3 Composition API、TypeScript 與 PrimeVue。
- Git commit 使用 Conventional Commits，內容說明使用中文。
- 新功能或行為變更優先使用 OpenSpec SDD；小型文件、錯字與機械性修改可直接處理。

## Backend 現況

- Feature routes 位於 `Endpoints/*Endpoints.cs`：
  - `AuthEndpoints.cs`：註冊、登入、目前使用者。
  - `TodoEndpoints.cs`：Todo CRUD。
  - `HealthEndpoints.cs`：健康檢查。
- ASP.NET Core Identity 使用 `AppDbContext` 與 PostgreSQL 儲存帳號資料。
- JWT bearer authentication 已註冊，API 可驗證 Access Token。
- `InitialIdentitySchema` migration 已於 2026-07-06 套用至 local PostgreSQL `web_demo`。
- Todo 資料目前仍由 `InMemoryTodoService` 保存，尚未持久化至資料庫。
- 共用 response/error model 位於 `Models/`。
- 集中式例外處理位於 `Middleware/ApiExceptionHandlingMiddleware.cs`。
- CORS 與 JWT 設定類別位於 `Options/`。

## Frontend 現況

- PrimeVue plugin 與 Aura theme 在 `src/WEB_Demo.Client/src/main.ts` 註冊。
- PrimeVue components 在各 Vue SFC 中按需 import。
- 全域樣式位於 `src/WEB_Demo.Client/src/styles/`，並使用 Tailwind CSS、SCSS 與 PrimeVue styled mode。
- `App.vue` 管理登入狀態，依狀態顯示 `LoginView` 或後台 layout。
- 後台 layout 已拆成 topbar、sidebar、menu 與主要內容區。
- API calls 集中於 `src/WEB_Demo.Client/src/api/`：
  - `http.ts`：shared fetch helper、`ApiResponse<T>` 解析、錯誤處理及 Bearer Token。
  - `auth.ts` / `authToken.ts`：登入、註冊、目前使用者及 Token 管理。
  - `todos.ts`：Todo API calls。
- 前端測試使用 Vitest、jsdom 與 Vue Test Utils。

## 測試與驗證

Backend：

```powershell
dotnet build
dotnet test WEB_Demo.sln
```

目前後端測試涵蓋：

- `InMemoryTodoService` create/read/toggle/delete。
- API root、health、Todo 等基礎 routes。
- 認證成功及未授權存取情境。

Frontend：

```powershell
cd src/WEB_Demo.Client
npm run test:unit
npm run build
```

目前前端測試涵蓋：

- Shared API client 的成功及錯誤 envelope、JSON body 與 Bearer Token。
- Auth API helper。
- Todo UI 的載入、新增、toggle 與刪除行為。

E2E（需先啟動 local PostgreSQL 並套用 Identity migration）：

```powershell
cd src/WEB_Demo.Client
npm run test:e2e:install
npm run test:e2e
```

Playwright 會自動啟動或重用 API 與 Vue client。互動及逐步除錯指令：

```powershell
npm run test:e2e:ui
npm run test:e2e:debug
```

目前 E2E 測試涵蓋 Chromium 上的註冊、登出、重新登入，以及 Todo 新增、完成與刪除流程。失敗時的 trace、screenshot、video 與 HTML report 會保留在 client 的測試輸出目錄。

## OpenSpec 現況

- 目前沒有 active change。
- 已 archive：
  - `openspec/changes/archive/2026-07-05-api-foundation`
  - `openspec/changes/archive/2026-07-05-api-test-foundation`
  - `openspec/changes/archive/2026-07-06-frontend-api-client-foundation`
  - `openspec/changes/archive/2026-07-06-frontend-test-foundation`
  - `openspec/changes/archive/2026-07-06-auth-foundation`
  - `openspec/changes/archive/2026-07-10-e2e-test-foundation`
- Main specs：
  - `openspec/specs/api-foundation/spec.md`
  - `openspec/specs/api-test-foundation/spec.md`
  - `openspec/specs/frontend-api-client-foundation/spec.md`
  - `openspec/specs/frontend-test-foundation/spec.md`
  - `openspec/specs/auth-foundation/spec.md`
  - `openspec/specs/e2e-test-foundation/spec.md`

## 已完成的基礎建設

### API foundation

- Endpoint、service、options、middleware 等結構已拆分。
- Todo in-memory state 已移至 service。
- 已加入集中式例外處理、CORS 設定與 Health Checks。
- 保留 Swagger、`ApiResponse<T>` envelope 與 HTTP-only local development。

### API test foundation

- 已建立 xUnit test project。
- 使用 FluentAssertions 與 `Microsoft.AspNetCore.Mvc.Testing`。
- 已建立 service unit tests 與基礎 API integration tests。

### Frontend API client foundation

- 已建立共用 `fetch` helper 與 TypeScript API types。
- 已集中 Todo API calls，統一處理 HTTP 及 envelope errors。
- Shared helper 已支援 JWT Bearer Token。

### Frontend test foundation

- 已建立 Vitest、jsdom 與 Vue Test Utils 測試環境。
- 已加入 shared API client、auth client 與 Todo UI tests。

### Auth foundation

- 已導入 ASP.NET Core Identity、EF Core、Npgsql 與 JWT bearer authentication。
- 已建立 Identity migration 並套用至 local PostgreSQL。
- 已完成 register、login、current-user endpoints。
- 已完成前端登入、註冊、登出、Token 儲存與 authenticated API request。
- 已加入後端及前端認證基礎測試。

## 尚未完成／候選下一步

以下項目尚未納入 active change；實作前應依範圍建立新的 OpenSpec proposal：

1. `todo-persistence-foundation`
   - 將 Todo 從 `InMemoryTodoService` 移至 PostgreSQL。
   - 建立 Todo entity、EF Core migration 與 database tests。
   - 評估 Todo 是否需要與登入使用者建立 ownership 關係。
2. `authorization-foundation`
   - 規劃 Role、Permission 與 authorization policies。
   - 補充更完整的 authentication/authorization tests。
3. `api-logging-foundation`
   - 導入正式的 structured logging，例如 Serilog。
4. 前端後續能力
   - 規模需要時再評估 Pinia。
   - 視需求加入 request cancellation、retry 或 cache。
5. 測試治理
   - 規劃 database tests 與 coverage threshold。

暫不優先：

- Minimal APIs migration to controllers。
- 大型 DTO restructuring。
- Axios 等新的 HTTP client runtime dependency。

## Frontend Layout Reference

- 前端 UI 已於 2026-07-10 從 Element Plus 遷移至 PrimeVue。
- 預設使用 PrimeVue styled mode 與 Aura theme。
- Admin/dashboard layout 參考 Sakai Vue：
  - GitHub：https://github.com/primefaces/sakai-vue
  - Preview：https://sakai.primevue.org/
- Sakai Vue 僅作為 sidebar、topbar、dashboard、table 與 CRUD 畫面的設計參考。
- 不預設複製完整 Sakai template；應漸進整合，保留既有 API modules、tests 與專案結構。

## VS Code 與工具備註

- `.vscode/tasks.json` 與 `.vscode/launch.json` 已提供 full-stack debug 設定。
- `Debug Full Stack` 可同時啟動 API 與 Vue client；停止時會清理 API 與 client processes。
- .NET SDK 8.0.422 與 Node.js 24.18.0 LTS 曾透過 winget 安裝。
- 若 `dotnet` 找不到，通常需要重啟 terminal 或 VS Code 以刷新 PATH。
- EF Core migration 使用 repo-local `dotnet-ef`：

```powershell
dotnet tool restore
dotnet tool run dotnet-ef database update --project src/WEB_Demo.Api/WEB_Demo.Api.csproj --startup-project src/WEB_Demo.Api/WEB_Demo.Api.csproj
```

- 中文文件使用 UTF-8；PowerShell 驗證時可使用 `Get-Content -Encoding UTF8`。
- OpenSpec spec/tasks 應包含合理的單元測試規劃；若延後測試，需在 proposal、design 或 tasks 中清楚記錄。
