## Context

系統目前為單一部署的 ASP.NET Core Web API 與 Vue 3 管理介面，已具備 PostgreSQL、EF Core migrations、ASP.NET Core Identity、JWT、系統角色及 Permission-based RBAC。PMS 後續會逐步加入 Issue、文件、API、資產、報表及 AI，因此需要先建立穩定的 Project 資源邊界與專案成員授權。

本系統已確定不發展多租戶 SaaS。使用者屬於整個系統，可同時加入多個專案；Project 而非 Tenant 是業務資料、搜尋與 AI 檢索的隔離邊界。

## Goals / Non-Goals

**Goals:**

- 建立可供後續 PMS 模組引用的 Project 實體與生命週期。
- 建立使用者與專案的多對多 membership。
- 以固定專案角色提供容易理解且可測試的第一版資源層級授權。
- 保留關鍵專案與成員異動的可追溯紀錄。
- 沿用現有 Identity、JWT、Permission-based RBAC、EF Core 與前後端架構。

**Non-Goals:**

- 多租戶、Organization、TenantId、計費、方案或租戶切換。
- Issue、文件、OpenAPI、資產、Secret、附件、通知、報表或 AI 功能。
- 自訂專案角色、工作流程設計器或專案角色與 Permission 的管理介面。
- 將 Todo 納入 Project 或改為持久化。
- 允許 System Admin 未經 membership 直接閱讀全部專案內容。

## Decisions

### 1. Project 是業務資料隔離邊界

後續所有 PMS 業務資料均須直接或間接關聯 `ProjectId`。API 查詢不得只因使用者具有系統管理角色就略過專案 membership；若未來確有支援或稽核需求，應以新的明確能力另行規格化。

不採用 Tenant 模型，因為產品已確定為單一公司內部系統，多租戶會增加所有查詢、索引、部署與測試的複雜度而沒有當前價值。

### 2. 系統 Permission 與專案角色分層

現有 Permission-based RBAC 繼續負責系統層級能力，例如是否能建立專案。`ProjectMembership` 則負責特定 Project 的資源授權。

第一版專案角色固定為：

- `Owner`：更新與封存專案、管理所有成員角色。
- `Manager`：更新專案並管理 `Member`、`Viewer`，不得新增、降級或移除 `Owner`。
- `Member`：讀取專案；作為後續業務功能的一般協作者角色。
- `Viewer`：唯讀專案。

固定角色以程式常數及明確 authorization handler 實作，不建立另一套可編輯 Permission 系統。替代方案是立即支援自訂專案角色，但目前沒有足夠需求可定義角色組合，會使第一階段過度複雜。

### 3. 專案建立者自動成為 Owner

具備系統層級 `Projects.Create` permission 的使用者可建立專案，並在同一 transaction 內成為該專案的第一位 `Owner`。一個專案可有多位 Owner，但系統必須保護最後一位 Owner，避免產生無人可管理的專案。

### 4. 專案採封存而非刪除

Project 具有 `Active`、`Archived` 狀態。第一版不提供硬刪除；封存後保留讀取與歷程，但禁止一般資料與成員異動。Owner 可解除封存。這能保護未來 Issue、文件與稽核資料的引用完整性。

### 5. 資料模型與一致性

主要資料模型：

- `Project`：Id、Name、Description、Status、CreatedAt、UpdatedAt。
- `ProjectMembership`：ProjectId、UserId、Role、CreatedAt、UpdatedAt；`ProjectId + UserId` 唯一。
- `ProjectAuditEvent`：ProjectId、ActorUserId、Action、TargetType、TargetId、摘要資料與 CreatedAt。

專案建立與首位 Owner、成員異動與 audit event 必須使用資料庫 transaction 保持一致。Audit 只保存必要摘要，不保存 token、密碼或其他 Secret。

### 6. API 與前端整合

後端維持 Minimal API，新增 `Projects` feature 並以 `/api/projects` 為根路由；服務層負責規則，authorization handler 負責 membership 與角色判斷，endpoint 不重複實作授權邏輯。

前端新增專案清單、建立／編輯專案及成員管理畫面。選單可依系統 permission 隱藏建立操作，但 API 授權仍是唯一安全邊界。第一版不引入新的前端 state library。

成員新增使用 Email autocomplete。只有 active Project 的 Owner／Manager 可在輸入至少 2 個字元後搜尋帳號；API 最多回傳 10 筆啟用且尚未加入該 Project 的 Email，避免無限制列舉帳號。最終新增請求仍以完整 Email 重新查找與驗證，不信任前端候選結果。

### 7. 後續 PMS Roadmap

以下方向只記錄為後續 change，不屬於本次 tasks：

1. `pms-issue-foundation`：Feature、Bug、Change Request、固定流程、留言與歷程。
2. `pms-document-api-hub`：Markdown 文件版本、OpenAPI 匯入及 Issue／章節／API 關聯。
3. `pms-asset-hub`：環境與資產 metadata、Vault reference、敏感操作 audit；Secret 不進入 AI 管線。
4. `pms-ai-knowledge`：依 Project 權限過濾的 RAG、來源引用、重新索引與敏感資料防護。
5. `pms-reporting-impact-analysis`：固定報表、結構化依賴圖及受控自然語言查詢；不允許任意 Text-to-SQL 直接操作正式資料庫。

每個階段須建立獨立 OpenSpec change，並依上一階段的實際使用結果調整，不預先承諾完整範圍。

## Risks / Trade-offs

- [固定專案角色日後可能不足] → 保持 membership 與授權邏輯集中，待有實際角色需求後另立 change 擴充。
- [系統 Permission 與專案角色容易混淆] → API 明確區分「能否執行系統功能」與「能否存取指定 Project」，並為兩層授權分別測試。
- [封存規則可能讓後續模組行為不一致] → 由集中式 Project status authorization requirement 阻擋異動，後續模組沿用同一規則。
- [Audit payload 可能逐漸包含敏感資訊] → 採 action-specific allowlist，只記錄識別資訊與安全摘要。
- [未建立 Organization 可能無法立即支援部門分類] → 部門只是未來可選的分類維度，不影響 Project membership 與 ProjectId 邊界。

## Migration Plan

1. 新增 Project、ProjectMembership、ProjectAuditEvent schema 與索引。
2. 新增 `Projects.Create` 等必要系統 permission，透過既有 permission initializer 建立。
3. 部署後由具備 permission 的既有管理者建立第一個專案；不自動搬移或關聯 Todo 資料。
4. 若部署失敗，在尚無下游 PMS 資料前可回退應用版本並移除本 change migration；正式產生專案資料後應以前向修正為主，避免遺失 audit 與 membership。

## Open Questions

- 專案代碼、部門分類、預設專案、封存清單顯示方式及更細的角色能力，待第一階段實際使用後再決定。
- System Admin 是否需要具備緊急支援存取（break-glass access），若有需要必須另行規格化授權、原因輸入與稽核，第一版不提供。
