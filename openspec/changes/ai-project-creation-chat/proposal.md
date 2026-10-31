## Why

目前規格助理只能回答文件問題，無法將自然語言意圖安全地轉成 PMS 操作。加入受控的對話式專案建立流程，可讓使用者先取得結構化草稿與預覽，再於明確確認後建立專案，作為未來多工具 AI 工作流的最小基礎。

## What Changes

- 擴充規格助理聊天室，使已登入使用者可用自然語言要求建立專案草稿。
- 由 LLM 將使用者文字解析為受限的專案名稱與描述；解析失敗或資料不足時不執行建立。
- 新增一次性專案建立草稿與確認 API，建立前回傳可檢查的預覽。
- 確認時沿用既有專案服務、權限與驗證，不允許 LLM 直接存取資料庫。
- 在聊天室顯示專案草稿、確認／取消操作與成功結果。

## Capabilities

### New Capabilities

- `ai-project-creation`: 透過規格助理對話產生、預覽並確認建立專案的安全流程。

### Modified Capabilities

- 無。

## Impact

- Backend：規格助理與專案 endpoints、LLM client contracts、一次性草稿儲存與既有專案建立服務整合。
- Frontend：規格助理聊天室訊息模型、專案草稿卡片與確認／取消互動。
- Tests：LLM 結構化解析、草稿所有權／過期／一次性確認、API 與 UI 流程。
- Dependencies：第一版沿用既有 OpenAI-compatible client，不新增 Semantic Kernel 套件；保留日後替換為 Kernel orchestration 的邊界。
