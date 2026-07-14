## MODIFIED Requirements

### Requirement: 可重複執行的 E2E 測試環境
專案 MUST 提供單一 npm script，以 Chromium 執行 E2E tests；每次 run MUST 使用獨立 ports 啟動專用 API 與 Vue client，且 MUST NOT 重用一般開發服務。

#### Scenario: 從單一指令執行 E2E tests
- **WHEN** 開發者在可建立 database 的 PostgreSQL 環境執行 E2E test script
- **THEN** 系統以 E2E environment 與獨立 ports 啟動專用 API 及 Vue client
- **THEN** Playwright 使用 Chromium 執行設定的 E2E tests

#### Scenario: 一般開發服務已經執行
- **WHEN** 一般開發 API 或 Vue client 已在 `5186` 或 `5173` 執行
- **THEN** E2E run MUST NOT 重用該服務
- **THEN** E2E services 使用自己的 database connection 與 ports

#### Scenario: E2E 服務無法啟動
- **WHEN** 專用 API 或 Vue client 在設定的 timeout 內無法使用
- **THEN** E2E test run MUST 以失敗狀態結束並指出無法使用的服務
- **THEN** runner 仍 MUST 嘗試清理該次建立的 database

### Requirement: E2E 測試資料隔離
每次 E2E run MUST 建立並使用一個全新且唯一命名的 PostgreSQL database，在執行測試前套用目前 EF Core migrations 並準備必要的 Admin；測試完成後 MUST 嘗試刪除整個 database。

#### Scenario: 開始 E2E run
- **WHEN** 開發者啟動 E2E test script 且 PostgreSQL 前置條件成立
- **THEN** runner 產生符合 E2E allowlist pattern 的唯一 database 名稱
- **THEN** 系統建立 database、套用目前 migrations 並建立該次執行所需的第一位 Admin
- **THEN** API 只連線至該次建立的 database

#### Scenario: 重複執行測試
- **WHEN** 開發者連續執行多次 E2E test runs
- **THEN** 每次 run 使用不同且初始狀態乾淨的 PostgreSQL database
- **THEN** 測試結果不依賴前一次執行留下的帳號或業務資料

#### Scenario: E2E run 成功
- **WHEN** Playwright tests 全部完成且成功
- **THEN** runner 在服務停止後刪除該次 E2E database
- **THEN** test command 保留成功 exit code

#### Scenario: E2E run 失敗
- **WHEN** setup、服務啟動或任一 Playwright test 失敗
- **THEN** runner MUST 保留原始失敗 exit code 與診斷 artifacts
- **THEN** runner 仍 MUST 在可識別已建立 database 時嘗試刪除該 database

#### Scenario: Database lifecycle 安全檢查失敗
- **WHEN** lifecycle command 收到非 E2E environment、一般開發 database 名稱或不符合 allowlist pattern 的目標
- **THEN** command MUST 拒絕建立、清空或刪除任何 database
- **THEN** command 以失敗狀態結束並回傳不含 credentials 的錯誤訊息
