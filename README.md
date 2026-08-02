# WEB_Demo

ASP.NET Core Web API 與 Vue 3 後台示範專案，使用 OpenSpec 進行 spec-driven development。

目前提供 Identity/JWT 認證、permission-based RBAC、Project／成員管理、Issue workflow、Project labels、List／Kanban 工作區與個人 Dashboard。早期 Todo 示範 API 與 UI 已移除。

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

## Development Demo Data

先建立或註冊一個本機帳號，再於 Development 環境執行：

```powershell
cd src/WEB_Demo.Api
dotnet run -- seed-demo-data --owner-email owner@example.com
```

此 command 會保留並建立可展示 Dashboard、Kanban、labels、comments 與 history 的兩份獨立專案：英文 `WEB_Demo Development` 與繁體中文 `WEB_Demo 開發專案`。兩者使用不同的固定 identifiers，可安全重跑、不互相覆寫、不建立預設帳密，也不會在一般啟動或 Production／CI／E2E 自動執行。

## Interface Languages

前端支援繁體中文（`zh-TW`）與英文（`en`），首次開啟預設為繁體中文。登入頁與 Topbar 的語言選擇器會立即切換 application 與 PrimeVue 元件文字，並以 `web_demo.locale` localStorage key 保存於目前瀏覽器。

新增或修改畫面文字時，應同步更新 `src/WEB_Demo.Client/src/i18n/messages.ts` 的兩份 message tree；API、database、route query 使用的 status、type、priority 與 role machine values 不得翻譯。

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
