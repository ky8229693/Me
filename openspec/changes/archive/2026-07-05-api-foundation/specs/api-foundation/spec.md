## ADDED Requirements

### Requirement: Endpoint 註冊需模組化
API SHALL 透過 endpoint extension classes 註冊 feature routes，而不是將所有 route mappings 直接放在 `Program.cs`。

#### Scenario: Program 專注於 hosting 設定
- **WHEN** 應用程式啟動
- **THEN** `Program.cs` MUST 只負責 services、middleware、Swagger、CORS 與 endpoint modules 的組裝，不包含 feature handler bodies

### Requirement: API 回應需使用共用 envelope
API SHALL 使用共用 `ApiResponse<T>` envelope 回傳成功與失敗 payload，同時保留正確 HTTP status codes。

#### Scenario: 成功 API 回應
- **WHEN** client 呼叫成功的 `/api` endpoint
- **THEN** response body MUST 包含 `success`、`message`、`data`、`errorCode`

#### Scenario: 失敗 API 回應
- **WHEN** client request 驗證失敗或目標資源不存在
- **THEN** response body MUST 包含 `success` 為 `false`、錯誤訊息、`data` 為 null，以及 error code

### Requirement: 未處理例外需集中處理
API SHALL 透過集中式 exception handling 格式化未處理例外，而不是將原始 exception details 暴露給 client。

#### Scenario: 非預期例外
- **WHEN** endpoint 拋出未處理例外
- **THEN** client MUST 收到 HTTP 500 response，且 body 為 `ApiResponse<T>` failure payload

### Requirement: CORS origins 需可設定
API SHALL 從 application configuration 讀取允許的 frontend origins。

#### Scenario: Development CORS 設定
- **WHEN** API 在 local development 環境執行
- **THEN** Vue development origin MUST 被允許，且不硬寫在 endpoint 邏輯中

### Requirement: Health check route 需穩定
API SHALL 使用 ASP.NET Core Health Checks 在 `/api/health` 暴露健康狀態，並維持共用 response envelope。

#### Scenario: API 健康
- **WHEN** client 呼叫 `GET /api/health`
- **THEN** API MUST 回傳 HTTP 200，且 body 為成功的 `ApiResponse<T>` payload，內含 service health data

### Requirement: Swagger 需維持可用
API SHALL 在 local development 維持 Swagger UI 可用。

#### Scenario: Swagger root redirect
- **WHEN** browser 開啟 API root path `/`
- **THEN** API MUST redirect 到 `/swagger`
