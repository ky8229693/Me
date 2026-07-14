# Development Guide

本文件集中記錄本機開發、資料庫、測試與常用工具指令。架構與開發規範請見 [`AGENTS.md`](../AGENTS.md)。

## Local URLs

- API：`http://localhost:5186`
- Swagger：`http://localhost:5186/swagger`
- Frontend：`http://127.0.0.1:5173`
- Vite proxy：`/api` → `http://localhost:5186`

Local development 使用 HTTP only。

## Backend

```powershell
cd src/WEB_Demo.Api
dotnet run
```

Build：

```powershell
cd src/WEB_Demo.Api
dotnet build
```

## Frontend

```powershell
cd src/WEB_Demo.Client
npm install
npm run dev
```

Unit tests 與 production build：

```powershell
cd src/WEB_Demo.Client
npm run test:unit
npm run build
```

## Backend Tests

```powershell
dotnet test WEB_Demo.sln
```

Backend tests 位於 `tests/WEB_Demo.Api.Tests/`，使用 xUnit、FluentAssertions 與 `Microsoft.AspNetCore.Mvc.Testing`。

## Playwright E2E

E2E 需要 local PostgreSQL 可用，且目前 PostgreSQL 帳號必須具有 `CREATE DATABASE`、終止目標 database sessions 與 `DROP DATABASE` 權限。測試使用 Chromium：

```powershell
cd src/WEB_Demo.Client
npm run test:e2e:install
npm run test:e2e
```

互動與逐步除錯：

```powershell
npm run test:e2e:ui
npm run test:e2e:debug
```

每次 `test:e2e`、`test:e2e:ui` 或 `test:e2e:debug` 都會建立唯一的 `web_demo_e2e_*` database、套用 migrations、建立一次性 Admin，並使用 API `5196` 與 frontend `5174`。一般開發服務不會被重用。

Runner 會在成功或失敗後嘗試刪除整個 E2E database，並保留 Playwright 原始 exit code與 trace、screenshot、video、HTML report。不要把 production database connection 傳給測試流程。

Local 預設從 `appsettings.Development.json` 讀取 PostgreSQL connection；CI 或不同環境可只在 process environment 提供 maintenance connection：

```powershell
$env:E2E_POSTGRES_CONNECTION="Host=localhost;Port=5432;Database=postgres;Username=<user>;Password=<secret>"
npm run test:e2e
Remove-Item Env:E2E_POSTGRES_CONNECTION
```

Database lifecycle command 只接受 `ASPNETCORE_ENVIRONMENT=E2E` 與符合 `web_demo_e2e_*` allowlist 的名稱，並明確拒絕 `web_demo`、`postgres`、`template0`、`template1`。若 process 被強制終止而留下 database，先確認名稱符合 allowlist，再使用具管理權限的 PostgreSQL 工具終止該 database sessions 並刪除；不要以 wildcard 或未檢查名稱的 script 批次刪除。

## PostgreSQL 與 EF Core Migrations

Local database connection 由 `src/WEB_Demo.Api/appsettings.Development.json` 的 `ConnectionStrings:DefaultConnection` 提供。不要在額外文件複製或保存密碼；部署環境應透過適當的 secret provider 注入。

Migration 工具使用 repo-local `dotnet-ef`：

```powershell
dotnet tool restore
dotnet tool run dotnet-ef database update --project src/WEB_Demo.Api/WEB_Demo.Api.csproj --startup-project src/WEB_Demo.Api/WEB_Demo.Api.csproj
```

## First Admin Bootstrap

建立新的第一位 Admin，密碼以互動方式隱藏輸入：

```powershell
cd src/WEB_Demo.Api
dotnet run -- bootstrap-admin --email admin@example.com
```

提升系統中既有帳號為第一位 Admin：

```powershell
cd src/WEB_Demo.Api
dotnet run -- bootstrap-admin --email admin@example.com --confirm-existing
```

Automation 可使用一次性環境變數，但不得寫入 tracked settings 或 logs：

```powershell
$env:ADMIN_BOOTSTRAP_PASSWORD="<one-time-secret>"
dotnet run -- bootstrap-admin --email admin@example.com --password-env ADMIN_BOOTSTRAP_PASSWORD
Remove-Item Env:ADMIN_BOOTSTRAP_PASSWORD
```

若系統已有其他 Admin，bootstrap command 會拒絕變更；日常角色管理應使用 authenticated Admin API。

## OpenSpec

```powershell
npm run spec:doctor
npm run spec:list
npm run spec:list-specs
npm run spec:validate
npm run spec:view
```

Active changes 與 archived changes 以 OpenSpec CLI 和 `openspec/changes/archive/` 為準，不在文件維護靜態清單。

## VS Code

- `.vscode/tasks.json` 與 `.vscode/launch.json` 提供 full-stack debug 設定。
- `Debug Full Stack` 可同時啟動 API 與 Vue client，停止時會清理兩個 processes。
- 若新安裝的 `dotnet` 或 Node 無法找到，重新開啟 terminal 或 VS Code 以刷新 PATH。

## Validation Checklist

提交行為變更前，依影響範圍執行：

```powershell
dotnet test WEB_Demo.sln
```

```powershell
cd src/WEB_Demo.Client
npm run test:unit
npm run build
```

```powershell
npm run spec:validate
```
