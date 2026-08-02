## Why

目前介面、驗證訊息與開發示範資料以英文為主，無法符合主要繁體中文使用者的操作情境。系統需要以繁體中文作為預設語系，同時保留完整英文介面，讓展示、測試與未來擴充具備一致的在地化基礎。

## What Changes

- 導入前端 i18n 基礎設施，支援 `zh-TW` 與 `en`，預設使用繁體中文。
- 在 Topbar 提供可存取的語言切換功能，並於瀏覽器保存使用者選擇。
- 將全站導覽、登入、Dashboard、Projects、Issues、Kanban、Members、Settings、Users、Roles、對話框、空狀態、錯誤與成功提示等使用者可見文字納入翻譯資源。
- 將固定的 Project、Issue workflow、type、priority、role 與 permission 顯示文字在前端本地化，但保留既有 API value、route、query 與資料庫值。
- 設定 PrimeVue 元件的繁體中文與英文 locale，使 paginator、filter、empty、calendar 等內建文字跟隨目前語系。
- 保留既有英文 `WEB_Demo Development` demo project，並新增一份使用獨立 deterministic identifiers、描述 `WEB_Demo` 開發歷程的繁體中文 Project、Issues、comments、history 與 audit 展示資料。
- 新增語系初始化、切換、持久化、固定值翻譯與中文 seed data 的自動化測試。

## Capabilities

### New Capabilities

- `user-interface-localization`: 定義繁體中文／英文介面、預設語系、語言切換與保存、PrimeVue locale、固定 domain value 顯示翻譯，以及繁體中文 Development demo data 的行為。

### Modified Capabilities

<!-- 無既有 main spec requirement 需要修改；development demo seed data 尚為獨立未封存 change。 -->

## Impact

- Frontend：Vue app bootstrap、Topbar、layout navigation、views、feature components、表單 validation、toast/message、測試 utilities 與 locale resources。
- Dependencies：新增 Vue 3 相容的 `vue-i18n` runtime dependency。
- Backend：擴充 deterministic Development demo seed catalog 與 seed service，不修改 endpoints、contracts、enum values、authorization 或 database schema。
- Data：英文與中文 demo projects 使用不同固定 identifiers 並可並存；重跑 seed 不得破壞或覆寫任一既有 demo 或 user-created data。
- Documentation：記錄支援語系、預設語系、fallback 與新增翻譯 key 的維護方式。
