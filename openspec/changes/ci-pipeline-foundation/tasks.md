## 1. Workflow Foundation

- [ ] 1.1 新增 GitHub Actions CI workflow，設定 pull request／`master` push triggers、read-only permissions、concurrency cancellation 與 job timeouts
- [ ] 1.2 將使用的 GitHub Actions pin 至受信任 release 的 immutable commit SHA，並保留可讀版本註解

## 2. Fast Verification Jobs

- [ ] 2.1 新增 backend Release build 與完整 .NET tests job，設定 .NET 8 與 NuGet dependency cache
- [ ] 2.2 新增 frontend `npm ci`、unit tests 與 production build job，設定 Node.js 24 與 npm cache
- [ ] 2.3 新增 OpenSpec 全量 validation job

## 3. Integration Verification

- [ ] 3.1 新增隔離 PostgreSQL service 與 readiness check，提供非 production E2E maintenance connection
- [ ] 3.2 安裝 Playwright Chromium dependencies並執行現有 E2E runner
- [ ] 3.3 在 E2E failure或取消時上傳 Playwright report、trace、screenshots與videos，設定有限保存期

## 4. Container Verification

- [ ] 4.1 新增 Compose config validation，使用明確的非敏感 CI placeholder values
- [ ] 4.2 Build API 與 Client production images，確認 workflow不登入 registry、不 push image且不執行 deployment

## 5. Documentation and Validation

- [ ] 5.1 文件化 CI jobs、check names、本機對應指令、失敗診斷與 CD 排除範圍
- [ ] 5.2 執行本機等價命令與 workflow static validation
- [ ] 5.3 在 GitHub pull request觀察完整 workflow通過，並記錄需由管理員另行設定的 required checks
