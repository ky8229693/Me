# Local Production Deployment

這套環境用 Docker Desktop 在本機模擬 Production deployment。它不取代既有的 HTTP 開發流程，也不適合直接公開至網際網路。

## Architecture

```text
https://localhost
  └─ gateway (Nginx + Vue dist)
       ├─ non-API routes → Vue index.html fallback
       └─ /api/* → ASP.NET Core API → PostgreSQL named volume
```

限制：

- 只執行一個 API instance。
- API restart 會清除 memory access sessions；有效 refresh cookie 可重新建立 session。
- `.env` 是本機明文練習 secret，不是正式 secret manager。
- API 啟動時會套用 EF Core migrations。

## Prerequisites

- Docker Desktop 已啟動，且可執行 `docker compose version`。
- .NET 8 SDK 可執行 `dotnet dev-certs`。
- 本機 TCP port `443` 未被其他 IIS、Web server 或 container 使用。

## 1. Prepare Local Secrets

從 repository root 執行：

```powershell
Copy-Item .env.example .env
```

編輯 `.env`，至少替換：

- `POSTGRES_PASSWORD`
- `JWT_SIGNING_KEY`，使用至少 32 個不可預測字元

`.env` 已被 Git 忽略。不要將實際值貼入 issue、log 或 tracked settings。

## 2. Create and Trust the HTTPS Certificate

```powershell
.\deploy\scripts\new-localhost-certificate.ps1
```

腳本會建立被 Git 忽略的：

- `certs/localhost.crt`
- `certs/localhost.key`

Windows 可能顯示憑證信任確認。若要刻意替換既有本機憑證：

```powershell
.\deploy\scripts\new-localhost-certificate.ps1 -Force
```

## 3. Build and Start

```powershell
docker compose up --build -d
docker compose ps
```

開啟：

- Application：`https://localhost`
- Health：`https://localhost/api/health`

Production 不提供 Swagger。查看啟動或 migration 問題：

```powershell
docker compose logs --tail 200 api
docker compose logs --tail 200 gateway
docker compose logs --tail 200 database
```

## 4. Bootstrap the First Admin

以下方式只將一次性密碼放在目前 PowerShell process，結束後清除：

```powershell
$securePassword = Read-Host "Admin password" -AsSecureString
$passwordPointer = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($securePassword)
$env:ADMIN_BOOTSTRAP_PASSWORD = [Runtime.InteropServices.Marshal]::PtrToStringBSTR($passwordPointer)
try {
    docker compose run --rm -e ADMIN_BOOTSTRAP_PASSWORD api bootstrap-admin --email admin@example.com --password-env ADMIN_BOOTSTRAP_PASSWORD
}
finally {
    [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($passwordPointer)
    Remove-Item Env:ADMIN_BOOTSTRAP_PASSWORD -ErrorAction SilentlyContinue
}
```

若要將既有帳號提升為第一位 Admin：

```powershell
docker compose run --rm api bootstrap-admin --email user@example.com --confirm-existing
```

系統已有 Admin 後，日常角色與權限應由管理 UI/API 操作。

## 5. Smoke Test

至少驗證：

1. 註冊或登入成功，refresh cookie 包含 `Secure`、`HttpOnly` 與 `SameSite=Strict`。
2. 建立 Project 與 Issue。
3. 直接開啟並重新整理 `/projects/{projectId}/issues/{issueId}`。
4. 開啟不存在的 `/api/...`，確認回應不是 Vue HTML。
5. 執行 `docker compose restart`，確認 PostgreSQL 資料仍存在。
6. 重新登入或等待 refresh，確認 API restart 後可建立新的 memory session。

## Lifecycle Operations

查看狀態與持續追蹤 logs：

```powershell
docker compose ps
docker compose logs -f
```

重新 build 並套用更新：

```powershell
docker compose up --build -d
```

一般停止；保留 database volume：

```powershell
docker compose down
```

再次啟動：

```powershell
docker compose up -d
```

Rollback 應先切回已知可用的 Git commit，再重新 build：

```powershell
docker compose up --build -d
```

若新版包含不可向下相容的 migration，需先依該 migration 的專屬方案還原 database backup。

## Backup and Restore

環境運行時建立 custom-format backup：

```powershell
.\deploy\scripts\backup-local-database.ps1
```

或指定安全的檔名：

```powershell
.\deploy\scripts\backup-local-database.ps1 -FileName before_update.dump
```

備份位於 `backups/` 且不會被 Git 追蹤。還原前建議先建立目前狀態的另一份 backup。

還原會停止 API 與 gateway、清理目前 database objects，再載入指定 backup：

```powershell
.\deploy\scripts\restore-local-database.ps1 -FileName before_update.dump -ConfirmRestore
```

腳本結束後會重新啟動 API 與 gateway。接著檢查：

```powershell
docker compose ps
Invoke-RestMethod https://localhost/api/health
```

## Explicit Data Removal

下列指令會刪除 containers 及 PostgreSQL named volume，練習資料無法從 volume 復原。只在已確認 backup 或確定不保留資料時手動執行：

```powershell
docker compose down -v
```

它不會刪除 repository 的 `.env`、`certs/` 或 `backups/`。

## Existing Development Workflow

Compose 不會改變既有 HTTP-only 開發方式：

```powershell
cd src/WEB_Demo.Api
dotnet run
```

```powershell
cd src/WEB_Demo.Client
npm run dev
```

## Verification Status

Repository-level verification completed for this change:

- OpenSpec validation
- PowerShell script parser validation
- Compose YAML syntax and required service structure
- ASP.NET Core Release publish
- Vue production build
- Backend and frontend unit tests

The following checks require Docker Desktop and remain part of the operator smoke test:

- `docker compose config`
- API and Client image builds
- container health transitions
- HTTPS certificate mount and browser trust
- Secure refresh cookie through the gateway
- nested-route refresh through Nginx
- PostgreSQL volume restart persistence
- backup and restore against the running database
