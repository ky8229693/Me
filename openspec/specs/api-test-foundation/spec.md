# api-test-foundation Specification

## Purpose
TBD - created by archiving change api-test-foundation. Update Purpose after archive.
## Requirements
### Requirement: API 測試專案需可執行
專案 SHALL 提供可透過 `dotnet test` 執行的 API 測試專案。

#### Scenario: 執行測試
- **WHEN** 開發者執行 API 測試指令
- **THEN** 測試專案 MUST build 並執行測試

### Requirement: Todo service 行為需被單元測試覆蓋
測試 SHALL 覆蓋 `InMemoryTodoService` 的核心 create、read、toggle、delete 行為。

#### Scenario: 預設 todo 清單
- **WHEN** 測試建立新的 `InMemoryTodoService`
- **THEN** `GetAll` MUST 回傳預設 todo items

#### Scenario: 新增 todo
- **WHEN** 測試呼叫 `Create`
- **THEN** service MUST 新增 todo 並產生可識別的 id

#### Scenario: 切換存在的 todo
- **WHEN** 測試呼叫 `Toggle` 且 id 存在
- **THEN** service MUST 切換 `IsDone` 並回傳更新後的 todo

#### Scenario: 切換不存在的 todo
- **WHEN** 測試呼叫 `Toggle` 且 id 不存在
- **THEN** service MUST 回傳 null

#### Scenario: 刪除 todo
- **WHEN** 測試呼叫 `Delete`
- **THEN** service MUST 依照 id 是否存在回傳 true 或 false

### Requirement: API foundation routes 需有 integration tests
測試 SHALL 透過 in-memory test server 驗證基礎 API routes。

#### Scenario: Swagger root redirect
- **WHEN** 測試呼叫 `GET /`
- **THEN** API MUST 回傳 redirect 到 `/swagger`

#### Scenario: Health endpoint
- **WHEN** 測試呼叫 `GET /api/health`
- **THEN** API MUST 回傳 HTTP 200 且 body 使用 `ApiResponse<T>` envelope

#### Scenario: Todo list endpoint
- **WHEN** 測試呼叫 `GET /api/todos`
- **THEN** API MUST 回傳 HTTP 200 且 body 使用 `ApiResponse<T>` envelope，`data` 內含 todo items

### Requirement: 測試指令需被記錄
專案 SHALL 在文件中記錄 API 測試執行方式。

#### Scenario: 查找測試指令
- **WHEN** 開發者查看專案指令文件
- **THEN** 文件 MUST 說明如何執行 API tests

