## Context

專案已有 backend unit/integration tests、frontend unit tests、Playwright E2E、OpenSpec validation，以及進行中的 container deployment foundation，但驗證目前只在本機手動執行。Repository 使用 GitHub，CI 應優先採 GitHub-hosted Ubuntu runner，以免初期引入 self-hosted runner 的信任、維護與可用性負擔。

## Goals / Non-Goals

**Goals:**

- 對 pull request 與 `master` push 提供一致且可重現的必要檢查。
- 將快速 backend、frontend 與 OpenSpec checks 和較慢的 E2E、container checks 分離，讓失敗容易定位。
- 以現有專案指令作為 CI 與本機驗證的共同來源。
- 使用最小 GitHub token permissions、immutable action references、timeouts 與 concurrency cancellation。
- 在 E2E 失敗時保存 Playwright 診斷 artifacts。

**Non-Goals:**

- 不 push container images、建立 release 或部署任何環境。
- 不使用 production database、secrets 或 credentials。
- 不建立 self-hosted runner、branch protection 或 repository ruleset；這些屬 repository administration。
- 不新增 coverage threshold、code quality SaaS、dependency auto-merge 或 security scanning policy。

## Decisions

### 使用單一 CI workflow 與多個 jobs

Workflow 分為 `backend`, `frontend`, `openspec`, `e2e`, `containers`。前三項可平行快速回饋；E2E 與 container verification 保持獨立，避免環境建置互相污染。相較多個 workflow，單一 workflow 可提供清楚且穩定的 required-check 集合。

### GitHub-hosted Ubuntu runner

Ubuntu runner具備 Docker tooling，且容易建立 PostgreSQL service。它不依賴開發者本機 WSL2，也不需要讓 GitHub 存取私人電腦。未來 CD 若需要本機 WSL2 runner，必須另立 change 評估信任邊界。

### E2E 使用隔離 PostgreSQL service

Workflow 啟動一次性 PostgreSQL service，將 maintenance connection 透過 job-level environment 傳給現有 E2E runner。Password 只用於該 ephemeral job，不使用 repository secret。Playwright Chromium 與系統相依套件由官方 CLI 安裝，失敗時上傳 report、trace、screenshots 與 videos。

### Container job只驗證、不發佈

Container job設定非敏感 placeholder environment，執行 `docker compose config` 並 build API 與 Client images。它不啟動需要 localhost certificate 的完整 Compose stack，也不登入 registry。Runtime smoke test留在 `local-production-deployment`，避免 CI 管理自簽 private key。

### 依 lock files 還原並使用 caches

.NET 使用 locked project dependency graph及 NuGet cache；Node 使用 `npm ci` 與 `package-lock.json` cache。Cache只加速依賴下載，不快取 build outputs或測試結果，避免陳舊產物掩蓋問題。

### 最小權限與供應鏈約束

Workflow明確設定 `permissions: contents: read`。第三方及 GitHub-maintained actions應 pin 至 immutable commit SHA，旁註可讀版本，並透過受控更新維護。Workflow不執行來自 pull request 的任意 secrets，也不使用 `pull_request_target`。

## Risks / Trade-offs

- [完整 E2E 增加 CI 時間] → 保持獨立 job、設定 timeout，並讓快速 checks平行執行。
- [Container foundation 尚未完成時 CI container job會失敗] → 實作此 change 前先完成或合併 `local-production-deployment` 必要檔案。
- [GitHub-hosted runner image更新造成差異] → 固定 .NET／Node／PostgreSQL major versions並使用 lock files。
- [Actions pin SHA 增加維護成本] → 在 workflow 註明 release tag，後續以受控 dependency update更新。
- [E2E artifacts可能包含測試資料] → 只使用一次性帳號與 database，設定有限保存期且不輸入 production secrets。

## Migration Plan

1. 確認本機部署 change 的 Compose 與 Dockerfiles 已存在。
2. 新增 CI workflow並在 feature branch推送。
3. 觀察所有 jobs於 pull request成功，修正 runner-specific差異。
4. 文件化 check names；需要時由 repository administrator設為 required checks。
5. Rollback只需移除或停用 workflow file，不影響 application runtime或資料。

## Open Questions

無；CD、registry publication、security scans與 branch protection留待獨立 changes。
