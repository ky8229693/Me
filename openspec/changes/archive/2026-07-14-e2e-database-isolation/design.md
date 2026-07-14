## Context

目前 `npm run test:e2e` 由 Playwright `webServer` 啟動 API 與 Vue client，API 使用 `appsettings.Development.json` 的 `web_demo`。測試以唯一 Email 避免註冊衝突，但資料仍留在開發 database；`reuseExistingServer` 也允許 E2E 連到使用一般設定啟動的服務。

本變更需要協調 Node／Playwright、ASP.NET Core command mode、EF Core migrations 與 PostgreSQL database lifecycle。開發環境使用 Windows PowerShell，但 npm 流程亦應能在 CI 執行，不依賴本機另裝 `psql`、`createdb` 或 shell-specific scripts。

## Goals / Non-Goals

**Goals:**

- 每個完整 Playwright run 使用一個全新、唯一的 PostgreSQL database。
- 測試服務使用獨立 ports 與明確 E2E environment，不能誤重用一般開發服務。
- 在測試前自動建立 database、套用 migrations 並 bootstrap Admin。
- 無論 Playwright 成功、失敗或無測試，都在 runner 的 `finally` 流程嘗試刪除 database。
- 對 database 建立與刪除加入 fail-closed 安全檢查。

**Non-Goals:**

- 不為每個 individual test case 建立 database。
- 不透過 production 或匿名 HTTP endpoint 管理 database。
- 不加入逐帳號硬刪除 API、通用 database seeding framework 或平行 E2E workers。
- 不改變一般開發的 `web_demo` database 與 `5186`／`5173` URLs。

## Decisions

### 使用 Node runner 管理完整 E2E lifecycle

`npm run test:e2e` 呼叫 repo 內的 Node runner。Runner 產生唯一 database 名稱與 E2E ports，依序執行 prepare command、Playwright，並在 `finally` 執行 cleanup command。Playwright 與其 `webServer` processes 繼承 runner 提供的 environment variables。

這比只依賴 Playwright global teardown 更可靠：即使 Playwright configuration 載入或 browser 啟動失敗，外層 runner 仍掌握 database 名稱並可執行清理。替代方案是 PowerShell script，但不利 CI 與跨平台；直接使用 PostgreSQL CLI 則新增本機工具前置條件。

### 後端提供非 HTTP 的 E2E database lifecycle command

API application command mode新增 E2E database `prepare`／`cleanup` 操作。Command 使用 PostgreSQL maintenance connection 建立或刪除目標 database；prepare 建立 database 後以目標 connection 執行 EF Core migrations，再使用既有 bootstrap service 建立 Admin。一般 web host 不註冊匿名 lifecycle route。

所有 destructive operation 必須同時確認：environment 為 `E2E`、database 名稱符合 `web_demo_e2e_<unique-id>` allowlist pattern、名稱不是 `web_demo`／`postgres`，且 connection host 與 database 來自 runner 明確傳入的設定。任一條件不符即失敗，不嘗試建立或刪除。

替代方案是在測試後逐筆刪除 `.test` accounts；Identity relations 與未來業務資料容易遺漏，無法保證乾淨狀態，因此不採用。

### 每個 run 共用 database 並維持單 worker

整個 E2E suite 共用同一個新 database，Playwright 保持 `workers: 1`。測試資料仍使用唯一 Email／title，避免 test cases 互相依賴。每個 test 建立 database 會反覆執行 migrations，使本地 feedback 顯著變慢，現階段沒有足夠收益。

### 使用獨立 ports 且禁止重用服務

E2E API 與 client 使用與開發環境不同的固定 ports，預計為 API `5196`、client `5174`；Vite E2E proxy、Playwright `baseURL` 與直接 API request 一併由設定衍生。E2E `webServer.reuseExistingServer` 固定為 `false`，若 port 已被占用則測試明確失敗。

### Admin credentials 只存在於該次 process environment

Runner 為該次 run 提供 E2E Admin Email 與 password environment variables。Prepare command 只輸出非敏感結果；密碼不得寫入 tracked settings、report 或 logs。現有固定開發用 E2E password 將移出 Playwright config。

## Risks / Trade-offs

- [PostgreSQL 帳號沒有 CREATE/DROP DATABASE 權限] → prepare 立即失敗並提供所需權限的非敏感說明，文件列為明確前置條件。
- [測試 process 被強制終止，`finally` 無法執行] → database 名稱含 E2E prefix 與時間／隨機識別；後續 runner 可在安全條件下清理過期 E2E databases，但自動清理過期資料庫不納入第一版。
- [刪除時仍有 API connection] → runner 等待 Playwright 與 webServer processes 結束後 cleanup；cleanup 必要時終止僅限目標 E2E database 的剩餘 sessions，再刪除。
- [固定 E2E ports 衝突] → 禁止服務重用並 fail fast；動態 ports 可在後續需要平行 runs 時再導入。
- [新增 command mode 擴大破壞能力] → 多重 environment、名稱 allowlist 與明確 action guard，且不提供 HTTP endpoint。

## Migration Plan

1. 建立受安全條件限制的 E2E database lifecycle command 與單元／整合測試。
2. 建立 Node runner，將 connection、ports 與 Admin credentials 只傳給子 processes。
3. 調整 Playwright web servers、Vite proxy 與 E2E helpers 使用獨立 URLs，禁止 reuse。
4. 更新 npm scripts 與文件，驗證成功及故意失敗的 Playwright run 都會清理 database。
5. 若需回復，將 `test:e2e` 指回原 Playwright command；保留 lifecycle command 不會影響一般 web 啟動，但應一併移除以縮小介面。

## Open Questions

- CI 的 PostgreSQL service account 與 secret 注入方式需在 CI pipeline 建立時確定；本地第一版沿用現有 PostgreSQL host credentials，但不得在新增 tracked E2E settings 中複製密碼。
- 是否自動掃描並清理因強制終止留下的過期 `web_demo_e2e_*` databases，留待觀察實際殘留頻率後決定。
