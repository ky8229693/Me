## 1. E2E Database Lifecycle

- [x] 1.1 建立 E2E database lifecycle arguments、database name allowlist 與 environment safety validation
- [x] 1.2 實作非 HTTP 的 prepare command，建立唯一 PostgreSQL database、套用 EF Core migrations 並 bootstrap 該次 Admin
- [x] 1.3 實作非 HTTP 的 cleanup command，終止目標 E2E database sessions 後刪除 database，且拒絕任何非 E2E 目標
- [x] 1.4 新增 lifecycle tests，涵蓋合法名稱、environment／名稱拒絕、prepare 失敗與 cleanup 安全界線

## 2. E2E Runner 與服務隔離

- [x] 2.1 建立跨平台 Node E2E runner，產生唯一 database 名稱並以 process environment 傳遞 connection、ports 與一次性 Admin credentials
- [x] 2.2 讓 runner 依序執行 prepare、Playwright 與 `finally` cleanup，並保留 Playwright 原始 exit code
- [x] 2.3 調整 Playwright web servers 使用 E2E API `5196` 與 client `5174`，禁止 reuse existing server
- [x] 2.4 調整 Vite proxy、base URL、direct API requests 與 helpers 使用 E2E environment URLs
- [x] 2.5 更新 npm E2E scripts，移除 Playwright config 內 tracked Admin password

## 3. 自動化驗證與文件

- [x] 3.1 新增 runner tests，涵蓋唯一 database 命名、environment 傳遞、成功 cleanup 與 Playwright 失敗後 cleanup
- [x] 3.2 驗證正常 E2E run 會建立乾淨 database、通過既有測試並在結束後刪除 database
- [x] 3.3 以故意失敗情境驗證 diagnostics 保留、失敗 exit code 不被 cleanup 覆蓋且 database 仍被刪除
- [x] 3.4 更新 `docs/memory.md`，記錄 PostgreSQL 權限、獨立 ports、database naming guard 與殘留 database 手動處理方式
- [x] 3.5 執行後端 tests、前端 unit tests、Playwright E2E、production build 與 OpenSpec strict validation
