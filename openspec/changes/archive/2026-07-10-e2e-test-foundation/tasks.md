## 1. Playwright 基礎設定

- [x] 1.1 在 Vue client 安裝 `@playwright/test` dev dependency 並更新 lockfile
- [x] 1.2 新增 headless、UI/debug 與 browser 安裝用 npm scripts
- [x] 1.3 建立 Playwright 設定，使用 Chromium、HTTP base URL、失敗診斷 artifacts 與合理 timeout
- [x] 1.4 設定 Playwright webServer 啟動或重用 ASP.NET Core API 與 Vite client
- [x] 1.5 將 Playwright output、report 與 test artifacts 加入 Git ignore

## 2. E2E 測試工具與認證流程

- [x] 2.1 建立唯一 E2E Email、密碼與 Todo title 的測試資料 helper
- [x] 2.2 建立透過 UI 註冊新帳號並進入 Dashboard 的 E2E test
- [x] 2.3 擴充認證測試，驗證登出後可使用相同帳密重新登入
- [x] 2.4 確認測試使用語意 selector，僅在必要時為 UI 加入穩定的 test id

## 3. Todo 主要流程

- [x] 3.1 建立已登入使用者新增唯一 Todo 的 E2E test
- [x] 3.2 驗證該 Todo 可切換為完成且 summary 正確更新
- [x] 3.3 驗證該 Todo 可刪除且不再顯示
- [x] 3.4 確認測試只操作自己建立的 Todo，並可在相同環境重複執行

## 4. 文件與驗證

- [x] 4.1 更新 README 或 `docs/memory.md`，記錄 PostgreSQL 前置條件、Chromium 安裝及 E2E 執行／除錯指令
- [x] 4.2 執行 Playwright E2E suite 並確認認證及 Todo 主要流程通過
- [x] 4.3 執行既有前端 unit tests 與 production build
- [x] 4.4 執行 `dotnet test WEB_Demo.sln`，確認後端測試無回歸
- [x] 4.5 執行 OpenSpec validation 並修正所有錯誤
