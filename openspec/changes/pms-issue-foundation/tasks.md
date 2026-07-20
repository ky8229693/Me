## 1. 資料模型與資料庫

- [ ] 1.1 建立 Issue type、priority、status 與 history change type 的固定 enum／常數模型
- [ ] 1.2 建立 Issue、IssueHistory、IssueComment EF Core entities 與 Project／User 關聯
- [ ] 1.3 設定欄位長度、foreign key、刪除行為及 Project-scoped 查詢索引
- [ ] 1.4 建立並檢查 Issue foundation EF Core migration
- [ ] 1.5 更新 membership 移除流程，在同一 transaction 清除該成員的 Issue assignments 並寫入 history

## 2. Issue 應用服務與授權

- [ ] 2.1 建立 Issue request／response contracts、分頁結果與共用 validation 規則
- [ ] 2.2 建立 Project-scoped Issue authorization service，集中 Owner／Manager／Member／Viewer 規則
- [ ] 2.3 實作 Issue 建立、詳情、列表搜尋、篩選與分頁服務
- [ ] 2.4 實作依角色、建立人與 Draft 狀態限制的 Issue 欄位更新服務
- [ ] 2.5 實作 assignee 驗證、指派與取消指派服務
- [ ] 2.6 實作固定 workflow transition 驗證與狀態異動服務
- [ ] 2.7 實作 IssueHistory append-only 寫入與查詢
- [ ] 2.8 實作 IssueComment 建立與依時間排序查詢
- [ ] 2.9 確保 archived Project 的 Issue、workflow、assignment 與 comment mutation 全部被拒絕

## 3. Issue API

- [ ] 3.1 建立 Project-scoped Issue list、create 與 detail Minimal API endpoints
- [ ] 3.2 建立 Issue update 與 assignment endpoints
- [ ] 3.3 建立 workflow transition endpoint，回傳允許的下一步狀態
- [ ] 3.4 建立 Issue history 與 comment list／create endpoints
- [ ] 3.5 將 endpoints 註冊至 Program.cs，維持 ApiResponse envelope 與既有錯誤處理格式
- [ ] 3.6 檢查 Swagger 合約不暴露 Issue／comment delete 或 comment update 操作

## 4. 後端測試

- [ ] 4.1 新增 Issue validation、固定 workflow 與角色授權單元測試
- [ ] 4.2 新增 Issue create、read、update、filter 與 pagination API integration tests
- [ ] 4.3 新增跨 Project、Admin 非成員及 archived Project 防護測試
- [ ] 4.4 新增 assignee membership、Member self-assignment 與 member removal 清除指派測試
- [ ] 4.5 新增 Issue history transaction、失敗操作不留 history 與 comment immutable 行為測試

## 5. Vue Issue 工作區

- [ ] 5.1 建立集中式 Issue TypeScript models 與 API client
- [ ] 5.2 在 Project 詳情與導覽加入 Issues 入口，沿用既有 Project membership 邊界
- [ ] 5.3 建立 PrimeVue Issue DataTable、搜尋、type／status／priority／assignee filters 與伺服器端分頁
- [ ] 5.4 建立 Issue create UI，依目前 Project role 限制 assignee 選項
- [ ] 5.5 建立 Issue detail／edit UI，顯示欄位、assignee、history 與 comments
- [ ] 5.6 建立 workflow 操作 UI，只顯示 API 回傳且目前使用者可執行的 transitions
- [ ] 5.7 套用 Viewer、Member、Owner／Manager 與 archived Project 的唯讀／可編輯狀態
- [ ] 5.8 補上 loading、空資料、validation、403／404 與一般 API 錯誤回饋

## 6. 整體驗證與文件

- [ ] 6.1 執行 dotnet build 與完整 dotnet test，修正本階段造成的問題
- [ ] 6.2 執行 frontend npm run build，修正 TypeScript 與打包問題
- [ ] 6.3 以 Owner、Manager、Member、Viewer 及非成員帳號驗證主要 Issue 流程
- [ ] 6.4 更新 README／docs/memory.md，記錄 Issue foundation 範圍、固定 workflow 與延後項目
- [ ] 6.5 執行 openspec validate 並確認所有 acceptance scenarios 已有對應實作或測試
