# Project Memory

最後更新：2026-07-06

這份文件記錄專案決策、目前狀態、已完成的 OpenSpec change，以及後續可優先處理的工作。開發規範以 `AGENTS.md` 為準；本文件只補充脈絡與歷史。

## 專案概況

- 專案名稱：`WEB_Demo`
- 後端：ASP.NET Core Web API on .NET 8 LTS
- 前端：Vue 3 + TypeScript + Vite
- UI library：Element Plus 與 `@element-plus/icons-vue`
- 開發模式：HTTP only，不主動導入 HTTPS
- API local URL：`http://localhost:5186`
- Swagger：`http://localhost:5186/swagger`
- Frontend local URL：`http://127.0.0.1:5173`
- Vite proxy：`/api` proxy 到 `http://localhost:5186`

## 已確認決策

- 使用 ASP.NET Core Web API 作為後端。
- Target Framework 使用 .NET 8 LTS。
- 前端使用 Vue 3 + TypeScript + Vite。
- UI 元件優先使用 Element Plus。
- 開發環境維持 HTTP only，避免 dev certificate 與 HTTPS 設定干擾初期開發。
- API 暫時使用 Minimal APIs；等規模真的需要時再評估 controllers。
- API routes 維持 `/api/*`。
- API root `/` redirect 到 `/swagger`。
- API response 使用共用 `ApiResponse<T>` envelope，但 HTTP status code 仍要正確表達結果。
- CORS allowed origins 從 `appsettings.*.json` 的 `Cors:AllowedOrigins` 讀取。
- 健康檢查使用 ASP.NET Core Health Checks，路由為 `/api/health`。
- Git commit 使用 Conventional Commits 規範，commit 內容說明使用中文。
- 資料庫選用 PostgreSQL；local 開發使用 DB `web_demo`、port `5432`、帳號 `postgres`、密碼 `1234`，目前 DB 尚未有 application tables。
- 登入系統資料表應優先透過 ASP.NET Core Identity + EF Core migrations 建立，不手刻登入 table。
- `auth-foundation` 決策：local registration 開放，且本批包含可見的登入/註冊 UI。
- EF Core migration 工具使用 repo-local `dotnet-ef`，先執行 `dotnet tool restore`，再用 `dotnet tool run dotnet-ef database update --project src/WEB_Demo.Api/WEB_Demo.Api.csproj --startup-project src/WEB_Demo.Api/WEB_Demo.Api.csproj` 套用 migration。
- 2026-07-06：已對 local PostgreSQL DB `web_demo` 套用 `InitialIdentitySchema` migration，建立 ASP.NET Core Identity tables。

## Backend 現況

- `Program.cs` 聚焦於 service registration、middleware、Swagger、CORS 與 endpoint module wiring。
- Feature routes 已拆到 `Endpoints/*Endpoints.cs`。
- Todo endpoint 位於 `Endpoints/TodoEndpoints.cs`。
- Health endpoint 位於 `Endpoints/HealthEndpoints.cs`，使用 `HealthCheckService`。
- Todo in-memory demo state 位於 `Services/InMemoryTodoService.cs`。
- Todo service 介面位於 `Services/ITodoService.cs`。
- 共用 response/error model 位於 `Models/`。
- Exception handling middleware 位於 `Middleware/ApiExceptionHandlingMiddleware.cs`。
- CORS options 位於 `Options/ApiCorsOptions.cs`。

## Frontend 現況

- 前端使用 Vue 3 Composition API + TypeScript。
- Element Plus components 透過 Vite plugins auto-import。
- API calls 已集中到 `src/WEB_Demo.Client/src/api/`，包含 shared HTTP helper、Todo API module 與共用型別。
- 前端單元測試使用 Vitest、jsdom、Vue Test Utils。
- 前端單元測試指令：`npm run test:unit`。

## OpenSpec 現況

- 目前沒有 active change。
- 已 archive：
  - `openspec/changes/archive/2026-07-05-api-foundation`
  - `openspec/changes/archive/2026-07-05-api-test-foundation`
  - `openspec/changes/archive/2026-07-06-frontend-api-client-foundation`
  - `openspec/changes/archive/2026-07-06-frontend-test-foundation`
- Main specs：
  - `openspec/specs/api-foundation/spec.md`
  - `openspec/specs/api-test-foundation/spec.md`
  - `openspec/specs/frontend-api-client-foundation/spec.md`
  - `openspec/specs/frontend-test-foundation/spec.md`
- 小型文件修正或機械調整可直接修改，不需要 OpenSpec change。
- 新功能或行為變更仍優先走 OpenSpec SDD。

## 已完成：api-foundation

`api-foundation` 完成第一批 API 基礎整理：

1. API 結構拆成 endpoint、service、options、middleware 等資料夾。
2. Todo routes 從 `Program.cs` 移到 endpoint extension。
3. Todo in-memory state 移到 service。
4. 建立 centralized exception handling middleware。
5. CORS origins 移到 appsettings。
6. 加入 ASP.NET Core Health Checks 與 `/api/health`。
7. 保留 Swagger UI、`/` redirect 到 `/swagger`、`ApiResponse<T>` envelope、HTTP-only local development。

明確延後到未來 change：

- EF Core / database
- Authentication / Authorization
- JWT / ASP.NET Core Identity
- Role / Permission
- Serilog 或更完整 logging infrastructure
- 大型 DTO restructuring
- Minimal APIs migration to controllers

## 已完成：api-test-foundation

`api-test-foundation` 完成第一批後端測試基礎：

1. 建立 `tests/WEB_Demo.Api.Tests` xUnit test project。
2. 加入 `WEB_Demo.Api` project reference。
3. 使用 FluentAssertions 與 `Microsoft.AspNetCore.Mvc.Testing`。
4. 測試 `InMemoryTodoService` 的 create/read/toggle/delete 行為。
5. 使用 `WebApplicationFactory<Program>` 測試基礎 API routes：
   - `GET /`
   - `GET /api/health`
   - `GET /api/todos`
6. 更新測試指令文件。

測試指令：

```powershell
dotnet test WEB_Demo.sln
```

明確延後到未來 change：

- Playwright / E2E tests
- Database tests
- Authentication / authorization tests
- Coverage threshold

## 已完成：frontend-api-client-foundation

`frontend-api-client-foundation` 完成第一批前端 API client 基礎：

1. 建立 `src/WEB_Demo.Client/src/api/` 作為前端 API boundary。
2. 新增 reusable API response 與 Todo TypeScript types。
3. 建立 native `fetch` shared request helper，統一解析 `ApiResponse<T>` envelope。
4. 統一處理 HTTP failure、envelope failure 與 backend message。
5. 建立 Todo API module，包含 list/create/toggle/delete。
6. 將 `App.vue` 從直接 `fetch` 改為呼叫 Todo API module。
7. 保留既有 Todo loading、empty、success、error UI 行為。

明確延後到未來 change：

- 新 HTTP client runtime dependency，例如 axios
- State management，例如 Pinia
- Authentication-aware API client
- Retry、cache、request cancellation

## 已完成：frontend-test-foundation

`frontend-test-foundation` 完成第一批前端單元測試基礎：

1. 加入 Vitest、jsdom、Vue Test Utils 作為 devDependencies。
2. 新增 `npm run test:unit` 前端單元測試指令。
3. 新增 Vitest 設定，使用 Vue plugin 與 jsdom。
4. 測試 shared API client 的成功 envelope、HTTP failure、envelope failure、JSON body content type。
5. 測試 Todo UI 初始載入、新增、toggle、delete 時會呼叫對應 Todo API module。
6. 保持前端 build 與 unit tests 可作為驗證指令。

測試指令：

```powershell
cd src/WEB_Demo.Client
npm run test:unit
```

## VS Code 狀態

- 已有 `.vscode/tasks.json` 與 `.vscode/launch.json`。
- `Debug Full Stack` 可同時啟動 API 與 Vue client。
- 停止偵錯時透過 `postDebugTask` 清理：
  - `api: stop`
  - `client: stop`

## 可優先處理的下一批工作

小修改可直接做：

- 維護 `docs/memory.md` 與 README 狀態。
- 修正文案、註解、文件錯字。
- 清理明顯過期的開發記錄。

需要 OpenSpec change 的候選項目：

- `api-logging-foundation`：導入較正式的 structured logging，例如 Serilog。
- `data-persistence-foundation`：導入 EF Core 與 SQL Server persistence。
- `auth-foundation`：導入 PostgreSQL-backed ASP.NET Core Identity、JWT 與登入基礎。
- `e2e-test-foundation`：建立 Playwright E2E 測試基礎。

## Tooling Notes

- .NET SDK 8.0.422 曾透過 winget 安裝。
- Node.js 24.18.0 LTS 曾透過 winget 安裝。
- 如果 `dotnet` 找不到，通常需要重啟 terminal 或 VS Code 讓 PATH 更新。
- 目前環境中 `git` 指令可能不在 PATH；需要版本狀態時先確認 Git 安裝或 PATH。
- 建立或修改含中文的檔案時，應明確使用 UTF-8；讀回驗證時使用 `Get-Content -Encoding UTF8`，避免 PowerShell 預設編碼或 console code page 造成亂碼誤判。
- 小型文件修正、註解調整、錯字修正、狀態同步、明顯機械性修改可直接處理，不需要建立 OpenSpec change；新功能或行為變更才優先走 OpenSpec SDD。
- 之後撰寫 OpenSpec change 時，spec/tasks 應納入合理的單元測試規劃；若暫不做測試，需在 proposal/design/tasks 中明確說明原因與延後範圍。
