## MODIFIED Requirements

### Requirement: API foundation routes 需有 integration tests
測試 SHALL 透過 in-memory test server 驗證基礎 API routes，並以目前的 PMS feature endpoint 測試取代已移除的 Todo endpoint。

#### Scenario: Swagger root redirect
- **WHEN** 測試呼叫 `GET /`
- **THEN** API MUST 回傳 redirect 到 `/swagger`

#### Scenario: Health endpoint
- **WHEN** 測試呼叫 `GET /api/health`
- **THEN** API MUST 回傳 HTTP 200 且 body 使用 `ApiResponse<T>` envelope

#### Scenario: Removed Todo endpoint
- **WHEN** 測試呼叫舊的 `GET /api/todos`
- **THEN** API MUST NOT expose a Todo route

## REMOVED Requirements

### Requirement: Todo service 行為需被單元測試覆蓋
**Reason**: Todo 示範服務與產品 API 已移除。

**Migration**: 以 Issue、label、Dashboard service 與 endpoint tests 維持核心業務覆蓋。

