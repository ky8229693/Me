# Development Guide

本文件集中記錄本機開發、資料庫、測試與常用工具指令。架構與開發規範請見 [`AGENTS.md`](../AGENTS.md)。

## Local URLs

- API：`http://localhost:5186`
- Swagger：`http://localhost:5186/swagger`
- Frontend：`http://127.0.0.1:5173`
- Vite proxy：`/api` → `http://localhost:5186`

Local development 使用 HTTP only。

若要在本機練習 Production build、HTTPS gateway、容器化 PostgreSQL、備份與還原，請見 [`local-production-deployment.md`](local-production-deployment.md)。該流程與本節的日常 HTTP development workflow 相互獨立。

GitHub Actions jobs、本機等價指令、失敗診斷與 required checks 請見
[`continuous-integration.md`](continuous-integration.md)。

## Specification Assistant (Phase 1)

第一階段使用文字比對檢索 OpenSpec Markdown，再由 API 直接呼叫 OpenAI-compatible Chat Completions endpoint。模型設定只存在後端；不要將 API Key 寫入 `appsettings*.json`、前端環境變數或版控。

使用 .NET user secrets 設定：

```powershell
cd src/WEB_Demo.Api
dotnet user-secrets init
dotnet user-secrets set "SpecAssistant:BaseUrl" "https://api.openai.com/v1/"
dotnet user-secrets set "SpecAssistant:Model" "your-model-name"
dotnet user-secrets set "SpecAssistant:ApiKey" "your-api-key"
```

也可以使用環境變數：

```powershell
$env:SpecAssistant__BaseUrl = "https://api.openai.com/v1/"
$env:SpecAssistant__Model = "your-model-name"
$env:SpecAssistant__ApiKey = "your-api-key"
dotnet run
```

未設定模型時，應用程式仍會正常啟動；只有 `/api/spec-assistant/query` 在找到候選規格並需要模型回答時會回傳 `503 SPEC_ASSISTANT_UNAVAILABLE`。

### Phase 2: Embedding semantic retrieval

生成模型負責「根據候選規格回答」，Embedding 模型只負責「將文字轉為向量並找出語意相近段落」。兩者可以使用不同 endpoint 與模型。第二階段預設使用 `Hybrid` 模式，合併語意檢索、字面檢索與受控業務詞彙；向量保存在 API process memory，應用重啟後狀態回到 `Unbuilt`，首次查詢或手動重建會重新建立索引。

未來使用 Ollama 時可設定：

```powershell
cd src/WEB_Demo.Api
dotnet user-secrets set "SpecAssistant:RetrievalMode" "Hybrid"
dotnet user-secrets set "SpecAssistant:EmbeddingBaseUrl" "http://localhost:11434/v1/"
dotnet user-secrets set "SpecAssistant:EmbeddingModel" "your-embedding-model"
```

本機 Ollama 不需要 `SpecAssistant:EmbeddingApiKey`。若使用遠端 OpenAI-compatible embedding provider，再以 user secrets 設定：

```powershell
dotnet user-secrets set "SpecAssistant:EmbeddingApiKey" "your-embedding-api-key"
```

可調整的檢索參數包括 `EmbeddingBatchSize`、`MaxIndexedChunks`、`TopK` 與 `MinimumSimilarity`。開發時若尚未準備 Embedding provider，可暫時切回第一階段文字檢索：

```powershell
dotnet user-secrets set "SpecAssistant:RetrievalMode" "Lexical"
```

### 第三階段：PostgreSQL pgvector

第三階段預設使用 `SpecAssistant:VectorStore=PostgreSql`，並將 `bge-m3` 的 1024 維向量持久化在既有 `web_demo` 資料庫。PostgreSQL server 必須先安裝 pgvector，並在資料庫啟用：

```sql
CREATE EXTENSION IF NOT EXISTS vector;
```

套用 migration：

```powershell
dotnet ef database update --project src/WEB_Demo.Api --startup-project src/WEB_Demo.Api
```

規格助理頁面的「同步規格」會比較 stable chunk ID 與 SHA-256 content hash，只為新增或變更 chunks 呼叫 Embedding，刪除已不存在的 rows，並保留未變更向量。API 重啟後會直接重用相同模型與維度的資料。

`bge-m3` 必須設定 `SpecAssistant:EmbeddingDimension=1024`。更換成不同維度的模型前，需要新增相容 migration；不同模型或維度的資料不會混合查詢。

需要回復記憶體索引時，可設定：

```powershell
dotnet user-secrets set "SpecAssistant:VectorStore" "Memory" --project src/WEB_Demo.Api
```

資料表可保留，回復模式不會修改既有 pgvector rows。

索引 API：

- `GET /api/spec-assistant/index`：查詢模式與索引狀態。
- `POST /api/spec-assistant/index/rebuild`：重新讀取 OpenSpec 並原子替換索引；失敗時保留上一個有效 snapshot。

認證預設使用 15 分鐘 access token 與 7 天固定到期 refresh token。Development 的 refresh cookie 因 HTTP only 不設 `Secure`；Production 一律設為 `Secure`，因此正式環境必須由 HTTPS 提供服務。Cookie 為 `HttpOnly`、`SameSite=Strict`，且 path 限制為 `/api/auth`。

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

登入後 Dashboard 會彙整目前使用者可存取的 active Projects，顯示 assigned／overdue Issues、workflow 狀態摘要與最近活動。Project Issues 頁可透過 URL query 的 `view=list|board` 切換 List 與 Kanban，並保留 search、type、status、priority、assignee 與 labels filters。

## Development demo data

Demo seed 只允許在 `Development` environment 透過明確 command 執行。先透過 UI 註冊帳號，或使用既有 Admin bootstrap command 準備 owner，再執行：

```powershell
cd src/WEB_Demo.Api
dotnet run -- seed-demo-data --owner-email owner@example.com
```

Command 會套用目前 migrations，建立 `WEB_Demo Development` Project、六個 Project labels，以及橫跨 Draft、Approved、InProgress、QA 與 Done 的示範 Issues。資料包含 assigned、overdue、comments 與 history，可直接展示 Dashboard 和 Kanban。

- Command 可重複執行；固定 seed records 不會重複建立。
- 額外建立的 Projects、members、labels、Issues、comments 與 history 不會被刪除。
- 不會建立帳號、預設密碼或匿名 seed endpoint。
- Production、CI、E2E 與一般 API 啟動不會自動寫入 demo data。

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

本機驗證原則：新增或修改 E2E spec 時至少執行該 spec 一次；若修改 runner、authentication helper、database lifecycle、global setup、Playwright config 或 Docker integration，則執行完整 E2E suite。未影響 E2E 的一般變更不需重跑整套，交由 CI 即可。

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

## 介面語系

- 支援 `zh-TW` 與 `en`，無有效保存值時固定使用繁體中文。
- 登入頁與 Topbar 可切換語系；選擇保存在 localStorage 的 `web_demo.locale`。
- i18n messages 集中於 `src/WEB_Demo.Client/src/i18n/messages.ts`，新增 key 時必須保持中英文 key parity。
- PrimeVue locale 與 `<html lang>` 由同一 locale controller 同步更新。
- Project／Issue status、type、priority、role 與 permission key 僅翻譯顯示 label，API、route query 與 database value 維持不變。

Development demo seed command 會建立並保留兩份獨立資料：英文 `WEB_Demo Development` 與繁體中文 `WEB_Demo 開發專案`。兩份資料使用不同 deterministic identifiers，可並存且重跑不會互相覆寫或刪除自訂資料。

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

## Frontend Routes and SPA Hosting

The Vue client uses history-mode routing. Main routes include `/dashboard`, `/projects`,
`/projects/:projectId/*`, `/admin/users`, and `/admin/roles`.

Vite development mode provides history fallback automatically. A production web host must:

- forward `/api/*` requests to the ASP.NET Core API;
- serve static client assets normally;
- return the client `index.html` for every other unmatched application route.

Without this fallback, directly opening or refreshing a nested Project or Issue URL returns a web-server 404 before Vue Router can resolve it.
# Permission-based RBAC

Permission definitions are code-owned in `Authorization/AppPermissions.cs`; do not create permission keys directly in the database or UI. Current endpoint mapping:

| Permission | Scope |
| --- | --- |
| `users.read` | Read user directory and account details |
| `users.manage` | Enable accounts and assign roles |
| `roles.read` | Read role directory and permission summaries |
| `roles.manage` | Create, rename, and delete custom roles |
| `permissions.manage` | Read the catalog and update Role–Permission mappings |

`Admin` is the break-glass system role and always resolves the complete catalog. Use custom roles for least-privilege daily administration. Role–Permission changes update affected users' security stamps and revoke their access sessions and refresh families.

Deployments that introduce Permission policies must apply the EF migration before startup. Startup applies pending relational migrations and synchronizes the fixed catalog. Existing access tokens without Permission claims fail closed on Permission-protected endpoints; users must refresh or sign in again.
# Semantic Kernel 小助手

全域小助手使用 `SpecAssistant` 區段的 OpenAI 相容設定：`BaseUrl`、`Model` 與
`ApiKey`。建議透過 .NET User Secrets 或部署環境變數提供金鑰，不要提交到版本庫。

小助手的對話、訊息、頁面情境與待確認動作儲存在 PostgreSQL。Semantic Kernel
只能呼叫查詢或「準備動作」工具；建立專案與議題必須由使用者按下確認後，才會透過
既有的 `ProjectAdministrationService`／`IssueAdministrationService` 執行並重新檢查權限。
每段對話同時只允許一個待確認動作，預設 15 分鐘到期，確認採冪等處理。

可用下列設定限制模型工具迴圈與輸出大小：

```json
{
  "SpecAssistant": {
    "AssistantMaxToolCalls": 8,
    "AssistantHistoryMessages": 30,
    "AssistantPendingActionMinutes": 15,
    "AssistantMaxToolResultCharacters": 8000
  }
}
```

舊的 `/api/spec-assistant/*` 查規格與 project draft API 暫時保留，若新助手有問題，
前端可退回既有規格助手頁面；移除舊 API 應另開變更處理。
