# Project Memory

這份文件是「用到才讀」的長期背景記憶。`AGENTS.md` 放日常工作規則；本檔放較完整的決策歷史、取捨與暫緩事項。

## 目前定位

- 專案名稱：`WEB_Demo`
- 目標：建立一個以 .NET 後端與 Vue 前端為主的 Web 專案骨架。
- 開發方式：採用 OpenSpec SDD 管理較大的需求與架構變更。
- 預設溝通語言：繁體中文。

## 技術選型決策

- 2026-07-05：後端選用 ASP.NET Core Web API。
  - 原因：使用者習慣 .NET，且 Web API 可支援 Vue/React/Blazor 等不同前端。
- 2026-07-05：後端 Target Framework 選用 .NET 8 LTS。
  - 原因：LTS 較適合長期維護與企業系統。
  - 備註：已透過 winget 安裝 .NET SDK `8.0.422`。
- 2026-07-05：前端選用 Vue 3 + TypeScript + Vite。
  - 原因：相較 React，Vue 對 .NET/後端背景開發者較容易上手，適合後台、表單、CRUD 類型系統。
- 2026-07-05：UI library 選用 Element Plus。
  - 原因：適合 Vue 3 後台與資料維護系統，上手快、元件完整。
  - 實作：使用 `element-plus`、`@element-plus/icons-vue`，並透過 Vite plugins 自動按需匯入。
- 2026-07-05：Node.js 更新到 `24.18.0` LTS。
  - 原因：OpenSpec 與部分前端工具要求 Node `>=20.19.0`，改用 24 LTS 避免版本警告。
- 2026-07-05：本機開發暫不使用 HTTPS。
  - 原因：避免 ASP.NET Core dev certificate 干擾開發流程。
  - 目前 API 使用 `http://localhost:5186`。

## API 決策

- 目前使用 ASP.NET Core Minimal APIs，不使用 Controllers。
  - 原因：專案仍小，Minimal APIs 足夠且更輕量。
  - 未來條件：當 API 模組變多、需要更傳統分層或 attribute routing 時，再開 spec 評估 Controllers。
- API routes 維持 `/api/*`。
- API root `/` 會 redirect 到 `/swagger`。
- Swagger UI 維持可用於本機開發。
- API response 使用共用 `ApiResponse<T>` envelope。
  - 成功與失敗 payload 都包含 `success`、`message`、`data`、`errorCode`。
  - HTTP status code 仍需正確使用，不採用永遠 `200 OK` 的做法。
- 2026-07-05：已套用 `api-foundation`。
  - `Program.cs` 現在只負責 services、middleware、Swagger、CORS、endpoint module wiring。
  - Todo routes 已移到 `Endpoints/TodoEndpoints.cs`。
  - Health route 已移到 `Endpoints/HealthEndpoints.cs`，並使用 ASP.NET Core `HealthCheckService`。
  - Todo in-memory state 與邏輯已移到 `Services/InMemoryTodoService.cs`。
  - 未處理例外由 `Middleware/ApiExceptionHandlingMiddleware.cs` 統一轉成 `ApiResponse<object>` failure payload。
  - CORS origins 已改由 `appsettings.Development.json` 的 `Cors:AllowedOrigins` 設定。

## OpenSpec 決策

- OpenSpec 已安裝套件：`@fission-ai/openspec@1.5.0`。
- OpenSpec 內容可以使用繁體中文。
- OpenSpec 固定結構標記需保留英文，例如：
  - `## ADDED Requirements`
  - `### Requirement:`
  - `#### Scenario:`
  - `- **WHEN**`
  - `- **THEN**`
- change id 與 capability id 維持英文 kebab-case。
- 目前 active change：
  - `openspec/changes/api-test-foundation`
- 已完成但尚未 archive 的 change：
  - `openspec/changes/api-foundation`

## api-foundation 範圍

`api-foundation` 只處理第一批 API 基礎架構：

1. API structure split into endpoints/services/contracts-style folders.
2. Centralized exception handling.
3. CORS origins moved to `appsettings`.
4. ASP.NET Core Health Checks.

同時保留既有行為：

- `ApiResponse<T>` response envelope。
- Swagger UI。
- `/` redirect 到 `/swagger`。
- HTTP-only local development。

刻意延後，不放進 `api-foundation`：

- EF Core / Database。
- Authentication / Authorization。
- JWT / ASP.NET Core Identity。
- Role / Permission。
- Serilog 或更完整 logging infrastructure。
- 大規模 DTO restructuring。
- 從 Minimal APIs 改成 Controllers。

這些項目之後應各自開獨立 OpenSpec change。

## api-test-foundation 範圍

2026-07-05：已套用 `api-test-foundation` OpenSpec change。

此 change 只處理第一批後端測試基礎：

1. 建立 `tests/WEB_Demo.Api.Tests` xUnit 測試專案。
2. 加入 `WEB_Demo.Api` project reference。
3. 安裝 `FluentAssertions` 與 `Microsoft.AspNetCore.Mvc.Testing`。
4. 測試 `InMemoryTodoService` 的 create/read/toggle/delete 行為。
5. 用 `WebApplicationFactory<Program>` 測少量 API foundation routes：
   - `GET /`
   - `GET /api/health`
   - `GET /api/todos`
6. 補上測試執行指令文件。

已新增：

- `WEB_Demo.sln`
- `tests/WEB_Demo.Api.Tests`
- `Services/InMemoryTodoServiceTests.cs`
- `Endpoints/ApiFoundationEndpointsTests.cs`

測試指令：

```powershell
dotnet test WEB_Demo.sln
```

刻意延後，不放進 `api-test-foundation`：

- 前端 unit tests。
- Playwright 或其他 E2E tests。
- Database tests。
- Authentication / authorization tests。
- Coverage threshold。

## VS Code 偵錯決策

- 已建立 `.vscode/tasks.json` 與 `.vscode/launch.json`。
- `Debug Full Stack` 同時啟動 API 與 Vue client。
- 停止偵錯後，透過 `postDebugTask` 清理：
  - `api: stop`
  - `client: stop`
- 原因：API 與 Vite dev server 若以 background task 或 `Start-Process` 啟動，停止偵錯不一定會自動關閉。

## 暫時注意事項

- PowerShell 顯示繁體中文 Markdown 時可能出現亂碼，但 OpenSpec validate 已確認文件有效。
- 若 `dotnet` 找不到，通常是 PATH 尚未刷新，重開終端機或 VS Code。
- 若前端或 OpenSpec 工具出現版本警告，先確認 `node --version` 是否為 `v24.18.0` 或更新的 LTS。
