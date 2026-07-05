## Context

目前後端是 ASP.NET Core Minimal APIs，已完成 `api-foundation`：`Program.cs` 保持 host wiring，feature routes 在 `Endpoints/`，todo 邏輯在 `Services/`，API response 使用 `ApiResponse<T>`。

測試應先覆蓋低成本、高價值的範圍，不追求完整 coverage。

## Goals / Non-Goals

**Goals:**

- 建立可執行的 .NET 測試專案。
- 使用 xUnit 作為 test framework。
- 使用 FluentAssertions 提升 assertion 可讀性。
- 使用 `Microsoft.AspNetCore.Mvc.Testing` 做少量 integration tests。
- 測試目前最重要的 API foundation 行為。

**Non-Goals:**

- 不建立前端 unit tests。
- 不建立 Playwright 或其他 E2E tests。
- 不測 database，因為目前尚未導入 persistence。
- 不測 authentication / authorization，因為目前尚未導入登入與權限。
- 不要求 coverage threshold。

## Decisions

1. 使用 xUnit。

   理由：.NET 生態常見，與 `dotnet test` 整合自然。

2. 使用 FluentAssertions。

   理由：讓 service 行為與 API response assertion 更直覺。

3. 使用 `WebApplicationFactory<Program>` 做 integration tests。

   理由：可以在不手動啟動 API server 的情況下測試 routing、middleware、Swagger redirect 與 response contract。

4. 先測 service 與少量 endpoint，不測 Vue。

   理由：目前風險主要在後端 foundation，前端仍是簡單示範頁。

## Risks / Trade-offs

- 新增測試專案會增加 NuGet 依賴 -> 範圍限制在 xUnit、FluentAssertions、ASP.NET Core testing。
- Integration tests 可能需要主專案暴露 `Program` partial class -> 以 minimal change 處理，不影響 runtime 行為。
- 測試過少可能漏掉部分情境 -> 先保護核心 contract，後續功能各自補測試。

## Migration Plan

1. 新增 `tests/WEB_Demo.Api.Tests`。
2. 加入主專案 reference 與測試套件。
3. 撰寫 `InMemoryTodoService` unit tests。
4. 撰寫 API integration tests。
5. 更新測試指令文件。
6. 執行 `dotnet test`。
