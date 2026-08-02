# Continuous Integration

GitHub Actions workflow 位於 [`.github/workflows/ci.yml`](../.github/workflows/ci.yml)，會在以
`main` 為目標的 pull request，以及 push 至 `main` 時執行。相同 pull request 的新執行會取消舊執行。

## Jobs and local equivalents

| Check name | Purpose | Local equivalent |
| --- | --- | --- |
| `Backend` | 以一次性 PostgreSQL 16 service 執行 locked NuGet restore、Release API build、完整 .NET tests | 啟動可拋棄的 PostgreSQL，設定 `ConnectionStrings__DefaultConnection`，再執行 `dotnet restore WEB_Demo.sln --locked-mode`; `dotnet build src/WEB_Demo.Api/WEB_Demo.Api.csproj -c Release --no-restore`; `dotnet test WEB_Demo.sln -c Release --no-restore` |
| `Frontend` | clean npm install、unit tests、production build | 在 `src/WEB_Demo.Client` 執行 `npm ci`; `npm run test:unit`; `npm run build` |
| `OpenSpec` | 驗證 main specs 與 active changes | repository root 執行 `npm ci`; `npm run spec:validate` |
| `E2E` | 以一次性 PostgreSQL 16 service 執行 Chromium workflows | 設定安全的 `E2E_POSTGRES_CONNECTION`，再於 Client 執行 `npm ci`; `npx playwright install chromium`; `npm run test:e2e` |
| `Containers` | Compose interpolation 與 API／Client production image builds | 設定 `.env` 的練習值，再執行 `docker compose config --quiet`; `docker compose build api gateway` |

E2E 本機資料庫權限、安全命名 allowlist 與清理方式請見 [development guide](development.md#playwright-e2e)。
Container 本機必要條件與環境變數請見 [local production deployment](local-production-deployment.md)。

## Failure diagnosis

- `Backend`：查看 PostgreSQL health、restore、compiler 或 xUnit failure；integration tests 只能連線到一次性
  CI／本機測試 database。lock file 不一致時，以預期套件版本重新執行 `dotnet restore WEB_Demo.sln --use-lock-file`
  並 review lock file diff。
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

## Local verification policy

本機驗證採風險導向，不要求每個 OpenSpec change 完成後都重跑整套 CI：

- 開發過程執行受影響的 build、unit tests 與 integration tests。
- 未新增或修改 E2E spec／共用基礎時，不需在本機重跑未受影響的 E2E，交由 CI 驗證即可。
- 新增或修改 E2E spec 時，完成前至少在本機執行該受影響 spec 一次。
- 修改 E2E runner、authentication helpers、database lifecycle、global setup、Playwright config 或 Docker integration 等共用基礎時，因可能影響全部 specs，本機必須執行完整 E2E suite。
- 認證、路由、資料庫、前後端整合或 workflow 有變更時，本機執行受影響的 Playwright spec；其餘未受影響的 E2E 可由 CI 驗證。
- 只有 Dockerfile、Compose、Nginx、環境變數 interpolation、production build 或部署設定有變更時，才要求本機執行 Compose config 與 image builds。一般應用功能交由 CI `Containers` job 驗證。
- OpenSpec verification task 可接受本機或 CI 的等效成功證據，不應僅因未在本機重複執行未受影響的 CI check 而保持未完成。
- 合併前仍必須確認 `Backend`、`Frontend`、`OpenSpec`、`E2E` 與 `Containers` 全部通過；建議將它們設定為 branch protection required checks。

Required checks 與 branch protection 屬 GitHub repository administration，不由此 workflow 自動修改。
