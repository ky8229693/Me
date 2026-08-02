## 1. Command and Seed Catalog

- [x] 1.1 新增 `seed-demo-data --owner-email <email>` command parser、validation 與 result type
- [x] 1.2 建立集中式 deterministic seed catalog，定義 `WEB_Demo Development` Project、labels、Issues、comments、history 與 audit identifiers/content

## 2. Transactional Seed Service

- [x] 2.1 實作 Development environment guard 與 enabled existing owner lookup，失敗時不得寫入 demo business data
- [x] 2.2 實作 Project、Owner membership 與 Project labels 的 idempotent create/repair 行為
- [x] 2.3 實作跨五種 workflow 狀態的 Issues、label joins、assigned/overdue dates、comments、history 與 Project audit events
- [x] 2.4 使用 relational transaction 保存完整 seed graph，保留額外 user-created Projects、members、labels、Issues、comments 與 history

## 3. Application Integration and Documentation

- [x] 3.1 在 `Program.cs` 註冊 seed service，於 migration／foundation initialization 後處理 command 並在 HTTP startup 前退出
- [x] 3.2 更新 development guide 與 README，記錄 owner prerequisite、Development-only command、idempotency 與不建立預設帳密的安全行為

## 4. Verification

- [x] 4.1 新增 command parser 與 seed service tests，涵蓋 fresh seed、representative Dashboard/Kanban data、重跑 idempotency、missing owner、disabled owner 與 non-Development denial
- [x] 4.2 驗證既有 user-created data 在重跑後保留，並執行受影響的 backend tests、Release build 與 OpenSpec strict validation
