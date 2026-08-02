## 1. Localization foundation

- [x] 1.1 安裝並註冊 `vue-i18n`，建立 `zh-TW`／`en` message resources、繁中 fallback 與 locale allowlist
- [x] 1.2 建立 locale controller，實作 `web_demo.locale` 初始化、切換、持久化及 `<html lang>` 同步
- [x] 1.3 建立並接入 `zh-TW`／`en` PrimeVue locale objects，確保切換時與 application locale 同步
- [x] 1.4 建立 Project status、Issue status/type/priority、Project role、system role 與 permission 的集中顯示翻譯 helpers

## 2. Shared layout and authentication

- [x] 2.1 在 Login 與 Topbar 加入可鍵盤操作的「繁體中文／English」語言選擇器，並驗證 desktop/mobile layout
- [x] 2.2 將 App、Topbar、Sidebar、navigation groups、Forbidden、Not Found 與共用 loading/error/confirm 文字移至 locale resources
- [x] 2.3 將 Login、register、session restore、logout 與 frontend-owned authentication validation/feedback 本地化

## 3. Project management interface

- [x] 3.1 將 Dashboard metrics、work lists、activity、empty/error states 與 domain values 本地化
- [x] 3.2 將 Projects list/create、Project workspace header/tabs/overview 與 project status 本地化
- [x] 3.3 將 Members 與 Settings 的欄位、roles、validation、confirmation、success/error feedback 本地化

## 4. Issue and Kanban interface

- [x] 4.1 將 Issue list、filters、pagination、labels management、empty/error states 與 action feedback 本地化
- [x] 4.2 將 Kanban columns、cards、transition controls、truncated warning 與 drag/move feedback 本地化，同時保留 machine values
- [x] 4.3 將 Issue create/detail/edit、comments、history、assignment、workflow actions 與 validation 本地化

## 5. Administration interface

- [x] 5.1 將 User Management 的 table、filters、dialogs、roles、status、validation 與 operation feedback 本地化
- [x] 5.2 將 Role/Permission Management 的 table、dialogs、permission categories、validation 與 operation feedback 本地化

## 6. Traditional Chinese demo data

- [x] 6.1 保留既有英文 catalog，新增使用獨立 deterministic identifiers、描述 `WEB_Demo` 開發歷程的繁體中文 Project、Issues、labels、comment、history 與 audit catalog
- [x] 6.2 擴充 seed service 於同一 transaction 確保中英文兩份 demo graph 可並存，且保留使用者修改與額外資料
- [x] 6.3 擴充 seed tests，驗證新資料庫同時具有中英文 Projects、中文版 Dashboard/Kanban 代表性、既有英文資料保留與重跑 idempotency
- [x] 6.4 更新 README 與 development guide，說明預設語系、切換保存、翻譯維護與中文 demo seed 行為

## 7. Verification

- [x] 7.1 更新 frontend test setup 並新增 locale allowlist、繁中預設、fallback、切換持久化、PrimeVue 同步與 locale key parity tests
- [x] 7.2 更新受影響 component tests，涵蓋繁中主要流程、英文切換與 domain machine value 不變
- [x] 7.3 新增 targeted E2E，驗證登入前後切換語系、重新整理保存及繁中 Project/Issue/Kanban 核心流程
- [x] 7.4 執行受影響 frontend/backend tests、Release/production builds、translation key audit 與 OpenSpec strict validation
