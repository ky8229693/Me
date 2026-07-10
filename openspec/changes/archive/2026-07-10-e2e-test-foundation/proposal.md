## Why

目前專案已有後端與前端單元測試，但缺少從瀏覽器驗證登入、註冊與 Todo 操作整合流程的自動化測試。建立 Playwright E2E 基礎可在後續加入權限與資料持久化前，先固定現有的主要使用者流程並降低跨層整合回歸風險。

## What Changes

- 導入 Playwright 作為前端 E2E 測試工具，提供可重複執行的設定與 npm scripts。
- 建立由測試流程啟動或連接 API 與 Vue 開發伺服器的本機測試環境。
- 建立獨立且可重複執行的測試帳號策略，避免測試彼此依賴。
- 加入登入／註冊與 Todo 主要操作流程的第一批瀏覽器測試。
- 記錄安裝瀏覽器、執行測試與除錯失敗案例的指令。
- 保留既有 HTTP-only 開發設定，不改變正式 API 或前端功能行為。

## Capabilities

### New Capabilities

- `e2e-test-foundation`: 定義 Playwright E2E 執行環境、主要使用者流程、自動化隔離與測試操作方式。

### Modified Capabilities

- 無。

## Impact

- 前端新增 Playwright dev dependency、設定檔、E2E tests 與 npm scripts。
- 本機 E2E 執行需要可用的 PostgreSQL、ASP.NET Core API 與 Vue/Vite client。
- 測試會透過現有 register/login API 建立測試帳號，並透過瀏覽器操作現有 UI。
- CI 若導入 E2E，需要額外安裝 Playwright browser 與啟動 PostgreSQL；本 change 先完成 repo 內可執行的基礎。
