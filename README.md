# WEB_Demo

ASP.NET Core Web API 與 Vue 3 後台示範專案，使用 OpenSpec 進行 spec-driven development。

## Stack

- Backend：ASP.NET Core Web API on .NET 8 LTS
- Frontend：Vue 3、TypeScript、Vite
- UI：PrimeVue、PrimeIcons、Aura theme
- Database：PostgreSQL、Entity Framework Core
- Authentication：ASP.NET Core Identity、JWT
- Tests：xUnit、Vitest、Vue Test Utils、Playwright

## Project Structure

```text
WEB_Demo/
├─ src/
│  ├─ WEB_Demo.Api/       ASP.NET Core Web API
│  └─ WEB_Demo.Client/    Vue 3 frontend
├─ tests/
│  └─ WEB_Demo.Api.Tests/ Backend unit and integration tests
├─ openspec/              Specifications and change artifacts
└─ docs/                  Project context and guides
```

## Quick Start

Backend：

```powershell
cd src/WEB_Demo.Api
dotnet run
```

Frontend：

```powershell
cd src/WEB_Demo.Client
npm install
npm run dev
```

- API：`http://localhost:5186`
- Swagger：`http://localhost:5186/swagger`
- Frontend：`http://127.0.0.1:5173`

## Documentation

- [Project instructions](AGENTS.md)：技術棧、開發規範與驗證指令。
- [Development guide](docs/development.md)：本機啟動、資料庫、測試、Admin bootstrap 與工具操作。
- [UI guidelines](docs/ui-guidelines.md)：PrimeVue 與 Sakai Vue 參考原則。
- [Project memory](docs/memory.md)：長期架構決策、安全取捨與當前限制。
- [OpenSpec](openspec/)：主規格、active changes 與 archived changes。

## Validation

```powershell
dotnet test WEB_Demo.sln
```

```powershell
cd src/WEB_Demo.Client
npm run test:unit
npm run build
```

```powershell
npm run spec:validate
```
