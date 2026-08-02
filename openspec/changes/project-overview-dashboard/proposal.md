## Why

目前專案 Overview 只顯示專案狀態、使用者角色與成員數，無法快速判斷單一專案的工作量、完成進度與近期風險。加入專案層級 Dashboard，可讓成員進入工作區後立即掌握議題進展，而不必先切換到議題清單自行篩選。

## What Changes

- 將專案 Overview 擴充為單一專案的進度 Dashboard。
- 顯示議題總數、進行中、已完成、逾期四項 KPI，並提供完成率。
- 顯示依工作流程狀態彙整的議題分布。
- 顯示最近更新的議題與即將到期的未完成議題，並可前往議題詳情。
- 保留專案狀態、目前使用者角色、成員數與既有快速連結。
- 所有新介面文字支援繁體中文與英文。
- 統計遵循既有 Project membership 存取邊界，封存專案仍可唯讀檢視。

## Capabilities

### New Capabilities

- `project-overview-dashboard`: 定義專案 Overview 的 KPI、狀態分布、近期議題、即將到期議題、導覽與雙語呈現。

### Modified Capabilities

<!-- 無既有 capability 的需求需要修改。 -->

## Impact

- 前端：Project Overview view、API client 型別與方法、i18n 訊息、元件測試及 E2E 測試。
- 後端：Projects 或 Issues endpoint、查詢服務、response contract 與 API 測試。
- 資料庫：優先使用既有 Project、Issue、Member 與 Label 資料，不新增資料表或 migration。
- 授權：沿用 Project membership 與既有 Project 存取規則，不提供 System Admin 跨專案 bypass。
