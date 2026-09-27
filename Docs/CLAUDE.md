# MintLandDemo - Claude Code 项目约束

## 项目背景
- Unity 2022.3 LTS + URP + C# + Input System + UGUI + TextMeshPro
- 单机 3D RPG 校招作品集 Demo
- 现有系统：QuestSystem / InventorySystem / ShopSystem / EventBus / GameRoot + GameRuntimeData
- 数据落地在 GameRoot.Instance.Context.Data（GameRuntimeData）

## 当前任务范围
正在开发 MintLand Dialogue Studio，分阶段推进。
当前阶段：P0 - 运行时核心（数据层 + Runner + 注册表），不接 UI，不改现有脚本。

## 架构总纲
Config 管模板，Runtime 管状态，Controller 管实体控制，
System 管游戏规则，Event 管跨系统通信，Service 管基础设施，UI 管表现。

## 分层红线（必须遵守）
- Runtime 脚本禁止引用 UnityEditor。
- 编辑器脚本统一放 Assets/Scripts/Dialogue/Editor/，用 #if UNITY_EDITOR 或独立 asmdef。
- 对话数据（DialogueDatabase / Node / Choice / Condition / Command）是纯 C# 数据类或 SO，不继承 MonoBehaviour，除 SO 外不依赖 UnityEngine（可用 [System.Serializable]）。
- Runtime 只读数据，不写回 SO Config。
- 禁止引入 ECS、复杂 DI、MVVM、Repository、Service Locator 等当前无实际需求的架构。
- 禁止引入第三方框架（Yarn Spinner、Ink、Odin、CsvHelper 等），需要时先讨论。
- 禁止重构与当前 Feature 无关的现有系统。
- 禁止把 Debug.Log 当作最终错误处理。

## 命名与目录规范
- 命名空间：`MintLandDemo.Dialogue`（Runtime）、`MintLandDemo.Dialogue.Editor`（Editor）。
- 目录：
  - `Assets/Scripts/Dialogue/Runtime/`：数据层、Runner、Registry、条件、指令
  - `Assets/Scripts/Dialogue/Editor/`：导入器、校验器、GraphView（后续阶段）
- 文件名与类名一致。
- 公开 API 用 `///` 注释，说明职责。
- 代码风格与现有脚本保持一致：中文注释、传统命名空间、显式 `private` 字段前缀 `_`。

## 现有系统接口（供指令/条件调用，不得修改）
- `QuestSystem`（MonoBehaviour）：
  - `void AcceptQuest(string questId)`
  - `void CompleteQuest(string questId)`
  - `QuestConfig GetQuestConfig(string questId)`
  - 任务状态在 `GameRuntimeData.quests`（List<QuestRuntime>，字段：questId / isAccepted / isCompleted / currentProgress / targetProgress）
- `InventorySystem`（static）：
  - `void AddItem(string itemId, int count)`
  - `bool HasItem(string itemId, int count)`
  - `bool RemoveItem(string itemId, int count)`
- `ShopSystem`（MonoBehaviour）：
  - `bool SellItem(string itemId, int count)`
  - `bool BuyGem(GemConfig gem)`
- `ShopUI`（MonoBehaviour，单例 Instance）：
  - `void OpenWithMode(ShopMode mode)`，ShopMode.Buy / ShopMode.Sell
- `ChoiceManager`（MonoBehaviour，单例 Instance）：
  - `void OnEndTrade()` - 商人对话「结束交易」按钮逻辑，保持不动
  - `void ShowChoices()` / `void HideChoices()`
- `EventBus`（static）：
  - `void Subscribe<T>(Action<T> handler) where T : struct`
  - `void Publish<T>(T eventData) where T : struct`
  - `void Unsubscribe<T>(Action<T> handler) where T : struct`
  - **所有事件必须是 struct**
- 金币：`GameRoot.Instance.Context.Data.Inventory.gold`
- 物品列表：`GameRoot.Instance.Context.Data.Inventory.items`（List<ItemEntry>，字段：itemId / count）

## 任务完成后自检
1. 代码编译通过。
2. 不引用 UnityEditor（Runtime 部分）。
3. 不修改任何现有脚本。
4. 不引入新依赖。
5. 命名空间、目录、命名符合规范。
6. 输出：新增文件清单 + 完整代码 + 自检结论。