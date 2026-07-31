# Continuous Integration

GitHub Actions workflow 位於 [`.github/workflows/ci.yml`](../.github/workflows/ci.yml)，會在以
`master` 為目標的 pull request，以及 push 至 `master` 時執行。相同 pull request 的新執行會取消舊執行。

## Jobs and local equivalents

| Check name | Purpose | Local equivalent |
| --- | --- | --- |
| `Backend` | locked NuGet restore、Release API build、完整 .NET tests | `dotnet restore WEB_Demo.sln --locked-mode`; `dotnet build src/WEB_Demo.Api/WEB_Demo.Api.csproj -c Release --no-restore`; `dotnet test WEB_Demo.sln -c Release --no-restore` |
| `Frontend` | clean npm install、unit tests、production build | 在 `src/WEB_Demo.Client` 執行 `npm ci`; `npm run test:unit`; `npm run build` |
| `OpenSpec` | 驗證 main specs 與 active changes | repository root 執行 `npm ci`; `npm run spec:validate` |
| `E2E` | 以一次性 PostgreSQL 16 service 執行 Chromium workflows | 設定安全的 `E2E_POSTGRES_CONNECTION`，再於 Client 執行 `npm ci`; `npx playwright install chromium`; `npm run test:e2e` |
| `Containers` | Compose interpolation 與 API／Client production image builds | 設定 `.env` 的練習值，再執行 `docker compose config --quiet`; `docker compose build api gateway` |

E2E 本機資料庫權限、安全命名 allowlist 與清理方式請見 [development guide](development.md#playwright-e2e)。
Container 本機必要條件與環境變數請見 [local production deployment](local-production-deployment.md)。

## Failure diagnosis

- `Backend`：查看 restore、compiler 或 xUnit failure；lock file 不一致時，以預期套件版本重新執行
  `dotnet restore WEB_Demo.sln --use-lock-file` 並 review lock file diff。
- `Frontend`：從 Vitest failure、Vue TypeScript error 或 Vite build error 對應處理；CI 固定使用 `npm ci`，
  因此 dependency 變更必須一併提交 `package-lock.json`。
- `OpenSpec`：輸出會指出無效的 change、requirement 或 scenario；修正 artifact 後重新執行全量 validation。
- `E2E`：先看 job log；失敗或取消時，下載保存 7 天的 `playwright-diagnostics-*` artifact，內含可用的
  HTML report、trace、screenshots 與 videos。CI database 是一次性 service，不應替換為 development 或 production connection。
- `Containers`：先處理 Compose interpolation/config error，再處理各 Dockerfile build log。CI placeholder
  只供 config/build，不是可部署 secret。

## Trust and delivery boundary

Workflow 只有 `contents: read` 權限，所有 Actions 使用 immutable commit SHA，且每個 job 都有 timeout。
CI 不使用 `pull_request_target`、production secrets 或 deployment credentials。

此 workflow 僅執行 continuous integration：不登入 container registry、不 push images、不建立 release、不部署環境，
也不設定 branch protection。Repository 管理員可在觀察 pull request 全部通過後，將以下穩定 check names 設為 required：

- `Backend`
- `Frontend`
- `OpenSpec`
- `E2E`
- `Containers`

Required checks 與 branch protection 屬 GitHub repository administration，不由此 workflow 自動修改。
