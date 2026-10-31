## Why

專案的 OpenSpec 規格已分散在多個 capability 與 change 文件中，使用者需要能以自然語言快速找出相關規格並取得有來源依據的回答。第一階段先練習直接串接 LLM API 與基本文件檢索，建立後續 Embedding/RAG 與工作流框架的可演進基礎。

## What Changes

- 新增規格助理 API，接受自然語言問題並回傳答案、相關規格及來源檔案。
- 從專案 `openspec` Markdown 文件載入內容，以簡單且可測試的文字比對挑選候選內容，再直接呼叫可設定的 OpenAI-compatible LLM API。
- 答案只能根據提供的規格內容產生；沒有足夠依據時必須明確告知規格未定義。
- 新增 Vue 規格助理頁面，讓登入使用者輸入問題並查看答案與來源。
- 以伺服器端設定管理 LLM 端點、模型與 API Key，不將秘密送到瀏覽器或提交到版控。
- 為候選文件選取、API 合約、錯誤處理與前端互動補上相應測試。

## Capabilities

### New Capabilities

- `spec-assistant`: 定義自然語言規格問答、來源引用、無依據回答及 LLM 服務不可用時的行為。

### Modified Capabilities

- `application-navigation`: 增加登入使用者可進入規格助理頁面的導覽入口。

## Impact

- Backend：新增 `/api/spec-assistant/query`、規格文件檢索服務、LLM client、設定選項與服務註冊。
- Frontend：新增規格助理路由、導覽項目、API client 型別與問答介面。
- Configuration：新增 LLM 相關伺服器端設定；實際使用時需由環境變數提供 API Key。
- Tests：擴充後端單元／整合測試與前端元件測試；視跨前後端流程需要加入目標 E2E 驗證。
- 後續階段將以本能力為基礎替換檢索實作（Embedding/RAG），再導入 Semantic Kernel 或 LangChain 類工作流；本階段不加入向量資料庫、對話記憶或 Agent。
