## 1. Test Project Setup

- [x] 1.1 建立 `tests/WEB_Demo.Api.Tests` xUnit 測試專案。
- [x] 1.2 加入 `WEB_Demo.Api` project reference。
- [x] 1.3 安裝 `FluentAssertions` 與 `Microsoft.AspNetCore.Mvc.Testing`。
- [x] 1.4 視需要調整 API project，讓 `WebApplicationFactory<Program>` 可用。

## 2. Service Unit Tests

- [x] 2.1 測試 `InMemoryTodoService.GetAll` 回傳預設資料。
- [x] 2.2 測試 `Create` 新增 todo 並產生 id。
- [x] 2.3 測試 `Toggle` 對存在 id 切換 `IsDone`。
- [x] 2.4 測試 `Toggle` 對不存在 id 回傳 null。
- [x] 2.5 測試 `Delete` 對存在與不存在 id 回傳正確結果。

## 3. API Integration Tests

- [x] 3.1 測試 `GET /` redirect 到 `/swagger`。
- [x] 3.2 測試 `GET /api/health` 回傳 HTTP 200 與 `ApiResponse<T>` envelope。
- [x] 3.3 測試 `GET /api/todos` 回傳 HTTP 200 與 todo data。

## 4. Documentation

- [x] 4.1 在 `AGENTS.md` 加入 API test 指令與慣例。
- [x] 4.2 在 `docs/memory.md` 記錄 `api-test-foundation` 範圍。
- [x] 4.3 視需要更新 `README.md`。

## 5. Verification

- [x] 5.1 執行 `dotnet test` 成功。
- [x] 5.2 執行 `npx openspec validate api-test-foundation` 成功。
