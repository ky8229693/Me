## Why

目前 Playwright E2E tests 直接使用本機開發資料庫 `web_demo`，測試帳號會持續累積，且重用既有 API 時可能誤連開發環境。每次測試執行使用獨立且全新的 PostgreSQL database，可讓結果可重複、避免污染開發資料，並提供明確的安全邊界。

## What Changes

- 每次 E2E test run 產生唯一且帶有明確 E2E 標記的 PostgreSQL database 名稱。
- 在啟動 API 前建立 database、套用 EF Core migrations，並建立該次執行專用的第一位 Admin。
- E2E API 與 Vue client 使用獨立 ports，且不得重用一般開發服務。
- 整個 Playwright run 共用同一個新建 database；測試結束後不論成功或失敗都嘗試刪除該 database。
- 加入資料庫名稱與環境安全檢查，禁止 lifecycle 工具操作一般開發或 production database。
- 更新 E2E 執行文件與測試，固定 setup、teardown、失敗清理及診斷行為。

## Capabilities

### New Capabilities

無。

### Modified Capabilities

- `e2e-test-foundation`: 將 E2E 環境由重用開發 database 與服務，改為每次 run 建立、migration、bootstrap 並清除唯一 PostgreSQL database，且使用獨立服務 ports。

## Impact

- 修改 Playwright configuration、global setup／teardown 與 E2E support utilities。
- 後端需提供僅供本機／CI E2E lifecycle 使用且具有強制安全防護的 database 建立、migration 與刪除流程；不新增匿名 HTTP 管理 endpoint。
- E2E 執行環境仍需可連線至 PostgreSQL server，執行帳號需具備建立及刪除 database 的權限。
- 更新 npm scripts、環境變數與 `docs/memory.md`；一般 API `web_demo` database 與既有開發 URL 不受影響。
