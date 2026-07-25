## Why

專案目前的 build、tests、OpenSpec 與 container 驗證都依賴開發者手動執行，容易在合併後才發現問題。建立最小 GitHub Actions CI foundation，可讓 pull request 與 `master` push 具備一致、可追蹤的品質檢查，同時為未來 CD 保留清楚邊界。

## What Changes

- 新增 GitHub Actions CI workflow，在 pull request 與 `master` push 自動執行。
- 驗證 ASP.NET Core restore、Release build 與 backend tests。
- 驗證 frontend clean install、unit tests 與 production build。
- 驗證所有 OpenSpec specs 與 active changes。
- 以隔離 PostgreSQL service 執行現有 Playwright E2E workflow。
- 驗證 Compose configuration，並 build API 與 Client container images，但不 push image。
- 使用最小 workflow permissions、dependency caches、job timeout 與 PR concurrency cancellation。
- 文件化 CI jobs、必要條件、失敗診斷與本機對應指令。
- 明確排除自動部署、registry publication、production secrets 與 self-hosted runner。

## Capabilities

### New Capabilities

- `ci-pipeline`: 定義 pull request 與主要分支的自動 build、test、OpenSpec、E2E 及 container verification 行為。

### Modified Capabilities

無。

## Impact

- 新增 `.github/workflows/` 下的 GitHub Actions workflow。
- 使用 GitHub-hosted Ubuntu runner、PostgreSQL service、.NET 8、Node.js 24、Playwright Chromium 與 Docker Compose。
- CI 不修改 application runtime contract、database schema 或部署環境。
- Container jobs 依賴 `local-production-deployment` 提供的 Dockerfiles 與 Compose definition；實作時應確認該 change 已可供 CI 使用。
