## 1. Structure

- [x] 1.1 在 API 專案建立 `Endpoints`、`Services`、`Options`、`Middleware` 資料夾。
- [x] 1.2 將 todo route registration 從 `Program.cs` 移到 feature endpoint extension。
- [x] 1.3 將 todo 商業邏輯與 in-memory state 移到 todo service。

## 2. Shared Infrastructure

- [x] 2.1 新增集中式 exception handling middleware，並回傳 `ApiResponse<object>` failure payload。
- [x] 2.2 擴充 API error codes，加入 internal server error 與 configuration-related failures。
- [x] 2.3 維持 `/` redirect 到 `/swagger`，並維持 development 環境啟用 Swagger。

## 3. Configuration

- [x] 3.1 新增 API options，用於設定 allowed CORS origins。
- [x] 3.2 將 Vue development origin 移到 `appsettings.Development.json`。
- [x] 3.3 由 configuration 註冊 CORS policy，不再硬寫 origins。

## 4. Health Checks

- [x] 4.1 註冊 ASP.NET Core Health Checks。
- [x] 4.2 使用 `HealthCheckService` 實作 `GET /api/health`。
- [x] 4.3 透過共用 `ApiResponse<T>` envelope 回傳 health data。

## 5. Verification

- [x] 5.1 API project build 成功。
- [x] 5.2 Vue client build 成功。
- [x] 5.3 手動驗證 `/`、`/swagger`、`/api/health`、`/api/todos` 回應。
- [x] 5.4 若 API foundation 慣例有變更，更新 `AGENTS.md`。
