# e2e-test-foundation Specification

## Purpose

定義使用 Playwright 從真實瀏覽器驗證認證與 Todo 主要整合流程的 E2E 測試基礎、資料隔離及失敗診斷方式。

## Requirements

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

### Requirement: 認證主要流程測試
E2E suite SHALL 透過使用者可見 UI 驗證註冊、登出及使用相同帳密重新登入的流程。

#### Scenario: 新使用者註冊
- **WHEN** 使用者切換至註冊模式並輸入有效且唯一的 Email 與密碼
- **THEN** 使用者可成功建立帳號
- **THEN** 應用程式顯示 authenticated dashboard

#### Scenario: 使用既有帳號重新登入
- **WHEN** 已註冊使用者登出並以相同 Email 與密碼登入
- **THEN** 應用程式再次顯示 authenticated dashboard

### Requirement: Todo 主要流程測試
E2E suite SHALL 以已登入使用者驗證 Todo 的新增、完成狀態切換與刪除流程。

#### Scenario: 建立 Todo
- **WHEN** 已登入使用者輸入唯一 Todo title 並提交新增
- **THEN** 新 Todo 顯示於 Todo list
- **THEN** pending summary 反映未完成項目

#### Scenario: 完成 Todo
- **WHEN** 使用者切換該測試所建立 Todo 的完成狀態
- **THEN** 該 Todo 顯示為完成
- **THEN** completed summary 反映完成項目

#### Scenario: 刪除 Todo
- **WHEN** 使用者刪除該測試所建立的 Todo
- **THEN** 該 Todo 不再顯示於 Todo list

### Requirement: 失敗診斷與操作文件
Playwright 設定 MUST 在測試失敗時保留足以診斷問題的 artifacts，且專案文件 MUST 說明 browser 安裝、一般執行與互動除錯指令。

#### Scenario: E2E test 失敗
- **WHEN** 任一 E2E test assertion 失敗
- **THEN** 測試輸出指出失敗的 test 與 assertion
- **THEN** Playwright 依設定保留 trace、screenshot 或等效診斷 artifact

#### Scenario: 開發者首次設定 E2E 環境
- **WHEN** 開發者閱讀專案 E2E 文件
- **THEN** 文件提供安裝 Chromium browser、執行 headless tests 與開啟互動除錯模式的指令
