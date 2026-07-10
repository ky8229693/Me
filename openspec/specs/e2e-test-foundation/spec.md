# e2e-test-foundation Specification

## Purpose

定義使用 Playwright 從真實瀏覽器驗證認證與 Todo 主要整合流程的 E2E 測試基礎、資料隔離及失敗診斷方式。

## Requirements

### Requirement: 可重複執行的 E2E 測試環境
專案 MUST 提供 Playwright 設定與單一 npm script，以 Chromium 執行 E2E tests，並在測試開始前確認 API 與 Vue client 可供存取。

#### Scenario: 從單一指令執行 E2E tests
- **WHEN** 開發者在已滿足 PostgreSQL 前置條件的環境執行 E2E test script
- **THEN** 系統啟動或重用 `http://localhost:5186` 的 API 與 `http://127.0.0.1:5173` 的 Vue client
- **THEN** Playwright 使用 Chromium 執行設定的 E2E tests

#### Scenario: 應用服務無法啟動
- **WHEN** API 或 Vue client 在設定的 timeout 內無法使用
- **THEN** E2E test run MUST 以失敗狀態結束並指出無法使用的服務

### Requirement: E2E 測試資料隔離
E2E tests MUST 為認證帳號與 Todo title 產生唯一值，且 MUST NOT 依賴其他測試預先建立的瀏覽器狀態或業務資料。

#### Scenario: 重複執行測試
- **WHEN** 開發者在相同 PostgreSQL database 上多次執行 E2E tests
- **THEN** 每次執行使用不同的 Email 註冊帳號
- **THEN** 測試不因前一次執行留下的帳號或 Todo 而衝突

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
