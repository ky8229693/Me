# WEB_Demo

技術組合：

- Backend: ASP.NET Core Web API (.NET 8 LTS)
- Frontend: Vue 3 + TypeScript + Vite
- UI: Element Plus
- SDD: OpenSpec

## 專案結構

```text
WEB_Demo/
  src/
    WEB_Demo.Api/       ASP.NET Core Web API
    WEB_Demo.Client/    Vue 3 + TypeScript 前端
  tests/
    WEB_Demo.Api.Tests/ API 單元測試與 integration tests
```

## 後端

```powershell
cd src/WEB_Demo.Api
dotnet run
```

網址：

```text
API:     http://localhost:5186
Swagger: http://localhost:5186/swagger
```

目前 API：

```text
GET    /api/health
GET    /api/todos
POST   /api/todos
PATCH  /api/todos/{id}/toggle
DELETE /api/todos/{id}
```

## 前端

```powershell
cd src/WEB_Demo.Client
npm install
npm run dev
```

網址：

```text
http://127.0.0.1:5173
```

Vite proxy：

```text
/api -> http://localhost:5186
```

## 測試

```powershell
dotnet test WEB_Demo.sln
```

目前測試範圍：

- `InMemoryTodoService` 單元測試
- `/` redirect 到 `/swagger`
- `GET /api/health`
- `GET /api/todos`

## OpenSpec

```powershell
npm run spec:doctor
npm run spec:list
npm run spec:validate
```
