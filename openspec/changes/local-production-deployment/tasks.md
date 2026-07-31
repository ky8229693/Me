## 1. Container Builds

- [x] 1.1 新增 API multi-stage Dockerfile 與 repository Docker ignore 規則
- [x] 1.2 新增 Client multi-stage Dockerfile，以 Vite production build 產生 Nginx 靜態內容

## 2. Gateway and Orchestration

- [x] 2.1 新增 Nginx HTTPS gateway 設定，支援 `/api` proxy 與 Vue Router fallback
- [x] 2.2 新增 Compose services、health checks、named database volume 與 Production environment injection
- [x] 2.3 新增 `.env.example`，並忽略 `.env`、本機 certificates 與 backups

## 3. Operations

- [x] 3.1 新增 Windows PowerShell localhost certificate 產生腳本
- [x] 3.2 新增 PostgreSQL backup 與 restore PowerShell scripts，包含輸入與目標安全檢查
- [x] 3.3 撰寫本機 Production 部署、Admin bootstrap、health、logs、update、rollback 與 data removal 文件

## 4. Verification

- [ ] 4.1 驗證 OpenSpec、Compose config、Release API publish 與 Client production build
- [x] 4.2 執行後端與前端測試，記錄無 Docker daemon 或憑證信任時無法自動驗證的手動項目
