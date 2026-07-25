## Why

目前專案只有開發模式啟動方式，缺少可重現的本機 Production-like 部署環境。建立容器化的第一版部署基礎，可在不上雲的情況下練習正式建置、HTTPS、反向代理、資料持久化、secrets 注入及備份還原。

## What Changes

- 提供 Docker Compose，一次啟動 Vue 靜態網站、ASP.NET Core API、PostgreSQL 與 Nginx gateway。
- 由單一 `https://localhost` 入口提供前端，並將 `/api` 反向代理至 API。
- 支援 Vue Router history fallback，使 Project 與 Issue 深層路由可直接開啟及重新整理。
- 以 Docker volume 保存 PostgreSQL 資料，並提供 health checks 與服務相依條件。
- 以未提交的 `.env` 注入資料庫密碼及 JWT signing key，提供安全的 `.env.example`。
- 提供本機信任憑證的掛載方式，以及啟動、初始化 Admin、備份、還原、更新與停止操作文件。
- 保留既有 HTTP development workflow，不改變日常 `dotnet run` 與 `npm run dev`。

## Capabilities

### New Capabilities

- `local-production-deployment`: 定義可重現的本機 Production-like 容器部署、HTTPS gateway、持久化、設定注入與維運操作。

### Modified Capabilities

無。

## Impact

- 新增 repository root 的 Compose、環境變數範本與容器忽略設定。
- 新增 API 與 Client container build 定義、Nginx gateway 設定及本機憑證目錄約定。
- 新增 PostgreSQL 備份／還原 scripts 與部署文件。
- 不變更既有 API contract、資料模型或前端功能行為。
