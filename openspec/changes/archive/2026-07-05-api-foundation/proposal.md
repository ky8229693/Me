## Why

目前 API 專案仍偏原型階段，endpoint、response、CORS、health check 等邏輯大多集中在 `Program.cs`。現在先建立輕量基礎架構，可以避免後續功能增加時反覆重整 API 結構。

## What Changes

- 建立可維護的 API 結構，拆分 endpoint 註冊、服務邏輯、契約與共用基礎設施。
- 新增集中式例外處理，並維持使用共用 `ApiResponse<T>` 回傳格式與正確 HTTP status code。
- 將 CORS 允許來源移到設定檔，不再硬寫於 `Program.cs`。
- 將目前手寫的 health endpoint 改為使用 ASP.NET Core Health Checks，同時保留穩定的 `/api/health` 路由。
- 本機開發維持 HTTP-only，除非使用者明確要求才啟用 HTTPS。

## Capabilities

### New Capabilities

- `api-foundation`: API 共用基礎架構，涵蓋 endpoint 組織、錯誤處理、CORS 設定、回應格式與 health checks。

### Modified Capabilities

- None.

## Impact

- 影響後端 `src/WEB_Demo.Api` 底下的檔案。
- 既有 `/api` 路由應維持穩定。
- 前端既有 API 呼叫應繼續使用 `ApiResponse<T>` envelope 中的 `data`。
- Swagger UI 應維持於 `/swagger`，且 `/` 繼續導向 `/swagger`。
