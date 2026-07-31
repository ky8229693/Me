## Context

後端目前使用 ASP.NET Core Minimal APIs，範例 endpoint 直接註冊在 `Program.cs`。專案已具備共用 `ApiResponse<T>` envelope、Swagger UI、HTTP-only 本機開發設定，以及一個會從 response `data` 讀取資料的 Vue client。

下一步基礎架構應保持輕量，同時為後續多模組功能預留清楚結構。

## Goals / Non-Goals

**Goals:**

- 將 endpoint 註冊與商業邏輯移出 `Program.cs`。
- 目前維持 Minimal APIs，使用 endpoint extension classes，而不是立即改 controllers。
- 將未處理例外集中到單一 middleware 流程。
- 所有 API payload 維持使用共用 `ApiResponse<T>` envelope。
- 從設定檔讀取 CORS origins。
- 使用 ASP.NET Core Health Checks，同時保留 `/api/health`。

**Non-Goals:**

- 本次不加入 authentication 或 authorization。
- 本次不導入資料庫或 EF Core。
- 除非使用者明確要求，否則不切換成 controllers。
- 本機開發不啟用 HTTPS。

## Decisions

1. 使用 `Endpoints/` extension classes，而不是 controllers。

   理由：專案目前仍小，且已使用 Minimal APIs。endpoint extension classes 可以縮小 `Program.cs`，又不必立即引入較重的 MVC/controller 結構。

   替代方案：controllers。controllers 適合較大型 API，但在目前階段會增加還不必要的儀式感。

2. 使用 `Services/` 放商業邏輯，並將目前 in-memory todo store 作為 demo service。

   理由：先分離服務邏輯，之後導入 EF Core 時會更自然。

   替代方案：繼續將 in-memory list 放在 endpoint handler。這最簡單，但會讓 endpoint handler 與 `Program.cs` 太有狀態。

3. 使用全域 exception handling middleware。

   理由：跨 endpoint 的錯誤格式應一致，不應每個 endpoint 都重複 try/catch。

   替代方案：各 endpoint 自行 try/catch。這會重複且容易漏掉。

4. 透過 `appsettings` 設定 CORS。

   理由：不同環境的前端來源不應需要改程式碼。

   替代方案：繼續硬寫 origins。原型階段可以接受，但部署環境一多就會脆弱。

## Risks / Trade-offs

- 資料夾增加可能讓初期看起來變重 -> 維持淺層結構，只加入目前 endpoint 需要的檔案。
- Minimal API endpoint class 後續可能變大 -> 功能模組增加時再依 feature 拆分。
- Health Checks 預設 payload 與目前格式不同 -> `/api/health` 使用 `HealthCheckService` 取得狀態，再包成 `ApiResponse<T>`。

## Migration Plan

1. 新增 endpoint、service、options、middleware 資料夾。
2. 將目前 todo 行為移到 service。
3. 將 route registration 移到 endpoint extension classes。
4. 在 endpoint 執行前加入 exception handling middleware。
5. 將 CORS origins 移到設定檔。
6. 加入 health check service 並保留 `/api/health`。
7. 建置後端與前端確認。

## Open Questions

- 當功能模組變大後，專案是否要從 Minimal APIs 改為 controllers？
- 導入 persistence 時，實際資料庫要使用 SQL Server 還是其他選項？
