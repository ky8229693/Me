## Why

`api-foundation` 已將 API 拆出 endpoint、service、middleware 與 configuration 邊界，現在已具備適合加入測試的結構。先補少量 API 測試基礎，可以保護目前的服務邏輯與 API contract，避免後續重構時不小心破壞既有行為。

## What Changes

- 建立 .NET API test project。
- 加入 service-level unit tests，先覆蓋 `InMemoryTodoService` 的核心行為。
- 加入少量 API integration tests，保護 `/`、`/api/health`、`/api/todos` 等基礎路由。
- 加入測試執行指令到專案文件與記憶檔。
- 本次不加入前端 unit tests、E2E tests、database tests 或 authentication tests。

## Capabilities

### New Capabilities

- `api-test-foundation`: API 測試基礎，涵蓋 service unit tests、少量 integration tests 與測試執行方式。

### Modified Capabilities

- None.

## Impact

- 新增 `tests/WEB_Demo.Api.Tests` 測試專案。
- 後端主專案可能需要少量調整以支援 `WebApplicationFactory`。
- CI 尚未建立；目前只要求本機 `dotnet test` 可執行。
