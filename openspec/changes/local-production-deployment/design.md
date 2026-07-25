## Context

專案目前以 `dotnet run`、Vite dev server 與本機 PostgreSQL 開發，尚無 production image、gateway 或可重現的整合部署。Production refresh cookie 強制 `Secure`，Vue Router history mode 需要 web host fallback，API 啟動時會執行 EF Core migrations。第一版應在 Windows 開發機透過 Docker Desktop 執行，同時不影響原有 HTTP 開發流程。

## Goals / Non-Goals

**Goals:**

- 以單一 Compose command 建立可重現的 Production-like 環境。
- 從 `https://localhost` 提供前端與 `/api`，驗證 Secure cookie 與 SPA routing。
- 使用正式 Release builds、非 Development 設定與 environment secrets。
- 保存 PostgreSQL 資料並提供基本備份、還原與操作說明。
- 讓各服務具備 health check，並按健康狀態決定啟動相依性。

**Non-Goals:**

- 不部署至雲端、公開網際網路或正式網域。
- 不建立正式 CA 憑證自動化、Kubernetes、multi-instance 或 zero-downtime deployment。
- 不將 in-memory JWT session registry 改為 distributed registry。
- 不改變 API、資料模型、authentication contract 或既有開發模式。

## Decisions

### 使用 Nginx 作為唯一 gateway

Nginx TLS endpoint 提供 Vue 靜態檔，並將 `/api/` 轉送至 API container。前後端同源可避免額外 CORS 與 refresh cookie 跨站問題，也能集中處理 SPA fallback。相較讓 API 同時提供前端檔案，此方案保持既有專案分離，未來也較容易搬到一般 reverse proxy。

### 使用 multi-stage container builds

API image 由 .NET SDK build/publish，再以 ASP.NET runtime 執行；Client image由 Node build Vue，再將 `dist` 複製至 Nginx image。Build context 使用 repository root，確保 solution 與 lock files 可被正確引用。

### 使用自簽 localhost 憑證的檔案掛載

Repository 只提供憑證產生指令與空目錄約定，不提交 private key。Gateway 將 `certs/localhost.crt` 與 `certs/localhost.key` 以唯讀方式掛載。使用者需將憑證加入本機信任；若只產生而未信任，瀏覽器仍可用警告方式測試。

### 使用 `.env` 注入 secrets

Compose 從 root `.env` 取得 PostgreSQL password 與 JWT signing key；Git 只保留無秘密的 `.env.example`。API 以 ASP.NET Core雙底線環境變數接收設定。第一版允許 issuer/audience 保持既有預設值。

### PostgreSQL volume 與工具 container

Named volume 保存 database。備份與還原 script 透過執行中的 PostgreSQL container 使用 `pg_dump`／`pg_restore`，備份檔存於被忽略的本機 `backups/`。還原是破壞性操作，文件要求先停止 API 並明確指定備份檔。

### API health check 經由 HTTP endpoint

Compose 使用 `/api/health` 判斷 API 健康；PostgreSQL 使用 `pg_isready`。Gateway 等待 API healthy，API 等待 database healthy。第一版不額外加入 database health check 至公開 endpoint，以避免改變既有 API health contract。

## Risks / Trade-offs

- [自簽憑證的信任步驟因作業系統而異] → 文件以 Windows PowerShell／Docker Desktop 為主要路徑，並清楚列出憑證生成與信任步驟。
- [API restart 會清除 in-memory access sessions] → 文件列為已知限制；使用者可由 refresh cookie 重新取得 session。
- [啟動時自動 migration 不適合多 instance] → 第一版限定單一 API instance。
- [`.env` 仍是本機明文 secret] → 僅供本機演練、加入 `.gitignore`，並要求使用隨機練習值。
- [還原會覆蓋現有資料] → script 要求明確參數並由使用者主動執行，文件先要求建立備份。

## Migration Plan

1. 從 `.env.example` 建立 `.env` 並設定隨機 secrets。
2. 產生並信任 localhost 憑證。
3. 執行 `docker compose up --build -d`。
4. 以 health endpoint、登入、nested route refresh 與資料重啟保存進行 smoke test。
5. 若需 rollback，執行 `docker compose down`；保留 named volume，可重新啟動原本開發流程。
6. 只有明確需要清除練習資料時才執行 `docker compose down -v`。

## Open Questions

無；第一版固定以 Docker Desktop、Nginx 與 PostgreSQL 實作。
