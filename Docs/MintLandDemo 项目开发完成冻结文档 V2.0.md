**MintLandDemo\
游戏客户端校招作品集 Demo｜开发完成冻结文档**

*V2.0 开发完成稿｜2026-09-08*

---

# 0. 文档状态

- 项目名称：MintLandDemo
- 文档版本：V2.0
- 当前状态：**开发完成，进入冻结状态**
- 目标岗位：游戏客户端方向校招
- 开发方式：Unity + C# + AI 辅助开发（Claude Code）
- 项目周期：约 2 周（含架构设计、系统实现、UI 搭建、调试与优化）

---

# 1. 项目概览

MintLandDemo 是一个面向游戏客户端校招作品集的单机 3D RPG Demo。项目通过一条完整、可玩的 Gameplay 链路，系统性地展示了 Unity 客户端开发中的工程化能力、Gameplay 系统设计、数据驱动架构、事件驱动解耦与客户端基础设施意识。

| 维度 | 说明 |
| :--- | :--- |
| 项目类型 | 单机 3D RPG / MMO-style RPG Demo |
| 核心玩法 | 任务引导 → 战斗 → 经济 → 装备成长 → Boss 战 |
| 设计原则 | Config 管模板，Runtime 管状态，System 管规则，Event 管通信 |
| 展示目标 | 工程化能力，而非内容堆叠 |

---

# 2. 技术栈

| 类别 | 选择 |
| :--- | :--- |
| 引擎 | Unity 2022.3 LTS |
| 语言 | C# |
| 输入系统 | Unity Input System Package |
| UI | Unity UGUI + TextMeshPro |
| 数据配置 | ScriptableObject |
| 版本控制 | Git |
| AI 辅助 | Claude Code |
| 资源来源 | Unity Asset Store / Sketchfab（后续替换） |

---

# 3. 最终客户端架构

```
                    ┌──────────────────────┐
                    │        UI Layer      │
                    │  Panel / Button / HUD │
                    └──────────▲───────────┘
                               │ Event
                               │
┌──────────────────────────────┴───────────────────────────────────────┐
│                         Gameplay Layer                               │
│                                                                      │
│  Character │ Combat │ Quest │ Inventory │ Equipment │ Shop │ Scene  │
│                                                                      │
└──────────────────────────────▲───────────────────────────────────────┘
                               │
                    ┌──────────┴──────────┐
                    │      Event Layer    │
                    │      EventBus       │
                    └──────────▲──────────┘
                               │
┌──────────────────────────────┴───────────────────────────────────────┐
│                         Runtime Data Layer                           │
│                                                                      │
│ PlayerRuntime │ InventoryRuntime │ EquipmentRuntime │ QuestRuntime   │
│                                                                      │
└──────────────────────────────▲───────────────────────────────────────┘
                               │
                               │ Config
┌──────────────────────────────┴───────────────────────────────────────┐
│                           Data Layer                                  │
│                                                                      │
│ CharacterConfig │ EnemyConfig │ ItemConfig │ EquipmentConfig          │
│ GemConfig │ QuestConfig │ ShopConfig                                │
└──────────────────────────────────────────────────────────────────────┘

                    Infrastructure / Services
         ┌─────────────────────────────────────────┐
         │ Save │ Scene │ Navigation │ Audio       │
         └─────────────────────────────────────────┘
```

## 3.1 架构总纲

> **Config 管模板，Runtime 管状态，Controller 管实体控制，System 管游戏规则，Event 管跨系统通信，Service 管基础设施，UI 管表现。**

---

# 4. 核心系统实现状态

| 系统 | 职责 | 状态 |
| :--- | :--- | :--- |
| **GameRoot + GameContext** | 跨场景生命周期管理、数据容器 | ✅ 完成 |
| **EventBus** | 跨系统事件通信、解耦 | ✅ 完成 |
| **PlayerController** | 第三人称移动、视角、输入 | ✅ 完成 |
| **CombatSystem** | 攻击检测、伤害计算、冷却 | ✅ 完成 |
| **DamageCalculator** | 伤害公式（攻-防，最低 1） | ✅ 完成 |
| **QuestSystem** | 任务状态、进度、自动接续 | ✅ 完成 |
| **InventorySystem** | 物品增删查、金币管理 | ✅ 完成 |
| **EquipmentSystem** | 武器/宝石装备、卸下、切换 | ✅ 完成 |
| **AttributeSystem** | 最终属性计算（基础+装备+宝石） | ✅ 完成 |
| **ShopSystem** | 购买/出售、金币结算 | ✅ 完成 |
| **InteractionSystem** | NPC 对话、任务板交互 | ✅ 完成 |
| **DropSystem** | 击杀掉落、自动拾取 | ✅ 完成 |
| **SceneService** | 场景切换、异步加载 | ✅ 完成 |
| **SaveService** | JSON 序列化、存档读档 | ✅ 完成 |
| **NavigationSystem** | NavMesh 自动寻路 | ✅ 完成 |
| **MiniMapUI** | 2D 俯视小地图 | ✅ 完成 |

---

# 5. 完整 Gameplay 流程（已全部实现并验证）

1. 玩家进入新手村，与受伤村长对话。 ✅
2. 村长要求玩家获取治疗药水。 ✅
3. 玩家点击任务按钮，自动寻路前往 NPC1 获取"治疗药水"。 ✅
4. 玩家返回村长处交付治疗药水（自动扣除背包药水）。 ✅
5. 村长赠与玩家"宝剑"。 ✅
6. 触发任务：消灭 3 只史莱姆。 ✅
7. 玩家战斗并击杀 3 只史莱姆。 ✅
8. 史莱姆死亡后产生"史莱姆粘液"，玩家自动拾取。 ✅
9. 玩家前往 NPC2 商店出售史莱姆粘液并获得金币。 ✅
10. 玩家使用金币购买宝石。 ✅
11. 玩家打开装备界面，将宝石镶嵌到宝剑的两个宝石槽位。 ✅
12. 宝石改变最终 Attack / Defense / HP。 ✅
13. 玩家前往新手村任务栏接取森林任务。 ✅
14. 玩家通过传送门进入森林。 ✅
15. 玩家击败森林 Boss，Demo 主流程结束。 ✅
16. Boss 死亡后生成返回传送门。 ✅

---

# 6. 任务链设计

采用 **任务链（Quest Chain）** 模式，任务间自动接续，玩家无需手动接取。

```
quest_heal (寻找治疗药水)
    ↓ 拿到药水后自动完成
quest_deliver_heal (交付治疗药水)
    ↓ 交付后自动接续
quest_kill_slime (消灭 3 只史莱姆)
    ↓ 击杀 3 只后自动完成
quest_forest_boss (讨伐森林 Boss)
    ↓ 击败 Boss 后自动完成
Demo 完成 🎉
```

| 任务 ID | 任务名称 | 目标 | 目标 NPC | 后续任务 |
| :--- | :--- | :--- | :--- | :--- |
| quest_heal | 寻找治疗药水 | 从 NPC1 获取药水 | NPC1 | quest_deliver_heal |
| quest_deliver_heal | 交付治疗药水 | 将药水交给村长 | VillageChief | quest_kill_slime |
| quest_kill_slime | 消灭史莱姆 | 击杀 3 只史莱姆 | — | quest_forest_boss |
| quest_forest_boss | 讨伐森林 Boss | 击败 Boss | — | — |

---

# 7. 事件总线事件列表

| 事件 | 发布者 | 订阅者 |
| :--- | :--- | :--- |
| EnemyKilledEvent | EnemyController | QuestSystem, DropSystem |
| QuestAcceptedEvent | QuestSystem | QuestTrackerUI, QuestDetailUI |
| QuestCompletedEvent | QuestSystem | QuestTrackerUI, QuestDetailUI |
| ItemAddedEvent | InventorySystem, DialogueManager | QuestSystem, InventoryUI |
| GoldChangedEvent | ShopSystem | ShopUI |
| EquipmentChangedEvent | EquipmentSystem | AttributeSystem, SaveService |
| AttributeChangedEvent | AttributeSystem | —（预留 UI） |
| SceneChangedEvent | SceneService | MiniMapUI, QuestTrackerUI |
| DemoCompletedEvent | QuestSystem | GameRoot |

---

# 8. 核心交互流程

## 8.1 战斗链路

```
PlayerController (攻击输入)
    ↓
CombatSystem.PerformAttack()
    ↓
DamageCalculator.CalculateDamage()
    ↓
EnemyController.TakeDamage()
    ↓
EnemyController.Die()
    ↓
EnemyKilledEvent
    ├──→ QuestSystem (击杀计数)
    ├──→ DropSystem (生成掉落)
    └──→ 自动拾取 → InventorySystem
```

## 8.2 装备成长链路

```
ShopSystem.BuyGem()
    ↓
InventorySystem.AddItem()
    ↓
EquipmentUI (宝石槽 + 按钮)
    ↓
EquipmentSystem.EquipGem()
    ↓
EquipmentChangedEvent
    ↓
AttributeSystem.CalculateFinalAttributes()
    ↓
CombatSystem 使用最新 finalAttack
```

## 8.3 自动寻路链路

```
QuestTrackerUI (点击任务按钮)
    ↓
NavigationSystem.NavigateToQuest()
    ↓
NavMeshAgent.SetDestination()
    ↓
到达目标 → 恢复玩家控制
    ↓
玩家按 E 对话
```

---

# 9. UI 系统清单

| UI 面板 | 触发方式 | 功能 |
| :--- | :--- | :--- |
| DialoguePanel | 按 E 与 NPC 交互 | 对话显示、任务接取/交付 |
| ChoicePanel | NPC2 对话选项 | 购买/出售/结束交易 |
| ShopPanel | 选择购买/出售 | 宝石购买、物品出售 |
| InventoryPanel | 按 B 键 | 背包物品查看 |
| EquipmentPanel | 按 I 键 | 武器/宝石管理 |
| QuestTrackerPanel | 自动显示 | 进行中任务（实时追踪） |
| QuestDetailPanel | 点击"我的任务" | 全部任务（进行中/已完成） |
| MiniMapPanel | 自动显示 | 2D 俯视小地图 |
| DeveloperConsole | F1/F5 | 调试工具 |

---

# 10. 存档数据结构

```json
{
  "player": {
    "level": 1,
    "currentHp": 100,
    "baseAttack": 10,
    "baseDefense": 5,
    "baseMaxHp": 100,
    "finalAttack": 25,
    "finalDefense": 10,
    "finalMaxHp": 120
  },
  "inventory": {
    "gold": 650,
    "items": [
      { "itemId": "sword", "count": 1 },
      { "itemId": "slime_mucus", "count": 3 }
    ]
  },
  "equipment": {
    "weaponSlots": [
      { "config": "Sword", "gems": ["Ruby"] },
      { "config": null, "gems": [] }
    ],
    "activeWeaponIndex": 0
  },
  "quests": [
    { "questId": "quest_heal", "isAccepted": true, "isCompleted": true },
    { "questId": "quest_kill_slime", "isAccepted": true, "isCompleted": true },
    { "questId": "quest_forest_boss", "isAccepted": true, "isCompleted": false }
  ]
}
```

---

# 11. 项目目录结构

```
Assets/
├── Art/                    # 美术资源（待替换）
├── Audio/                  # 音频资源
├── Data/                   # ScriptableObject 配置
│   ├── Character/
│   ├── Enemy/
│   ├── Equipment/
│   ├── Gem/
│   ├── Item/
│   ├── Quest/
│   └── Shop/
├── Prefabs/                # 预制体
├── Scenes/                 # 场景
│   ├── 00_Bootstrap.unity
│   ├── 01_NewbieVillage.unity
│   └── 02_Forest.unity
├── Scripts/
│   ├── Core/
│   │   ├── Event/          # EventBus + 事件定义
│   │   ├── Game/           # GameRoot + GameContext
│   │   └── Utility/
│   ├── Gameplay/           # 核心游戏系统
│   │   ├── Character/
│   │   ├── Combat/
│   │   ├── Quest/
│   │   ├── Inventory/
│   │   ├── Equipment/
│   │   ├── Shop/
│   │   ├── Interaction/
│   │   └── Navigation/
│   ├── Controller/         # 实体控制器
│   │   ├── Player/
│   │   ├── Enemy/
│   │   └── NPC/
│   ├── Infrastructure/     # 基础设施
│   │   ├── Save/
│   │   ├── Scene/
│   │   └── Pool/           # 预留
│   └── UI/                 # UI 面板
│       ├── Common/
│       ├── Dialogue/
│       ├── Quest/
│       ├── Inventory/
│       ├── Equipment/
│       ├── Shop/
│       └── Character/
├── UI/                     # UI 资源
└── ThirdParty/             # 第三方资源
```

---

# 12. 技术亮点总结

## 12.1 数据驱动

```
ScriptableObject (Config)
       ↓
纯 C# Runtime 数据
       ↓
Gameplay System
```

- 配置与运行状态严格分离
- 数值调整无需修改代码
- 便于后续接入热更新

## 12.2 事件驱动解耦

```
CombatSystem
    ↓ EnemyKilledEvent
    ├──→ QuestSystem (击杀计数)
    ├──→ DropSystem (掉落生成)
    └──→ UI (面板更新)
```

- Gameplay 系统间零直接依赖
- 新增功能只需订阅事件
- 符合开闭原则

## 12.3 服务抽象

```csharp
IResourceService   // 资源加载抽象，后续可替换为 Addressables
ISaveService       // 存档抽象，JSON 序列化
ISceneService      // 场景切换抽象
```

- 为后续热更新、Addressables 预留替换空间
- 业务代码不依赖具体实现

## 12.4 跨场景数据持久化

```
00_Bootstrap (启动场景)
    ↓ 创建
GameRoot (DontDestroyOnLoad)
    ├── GameContext (纯 C# 数据袋)
    ├── EventBus
    ├── SaveService
    └── SceneService
```

- 场景切换不丢失任何运行时数据
- 存档恢复完整游戏状态

## 12.5 完整 Gameplay 闭环

> 经济（粘液出售） → 装备（宝石购买） → 属性（AttributeSystem 重算） → 战斗（DamageCalculator）

---

# 13. 与 AI 协作开发经验

| 原则 | 实践 |
| :--- | :--- |
| 架构先行 | 先冻结架构与规范，再进入实现 |
| 分步交付 | 每次只做一个小 Feature，验证通过再继续 |
| Review 驱动 | AI 生成代码后必须 Review 并在 Unity 运行验证 |
| 规范约束 | AI 必须遵守架构红线，不得擅自引入新框架 |

## 13.1 常用工作流

```
人：确定 Feature、目标、约束和验收标准
    ↓
AI：读取项目规范 → 分析现有代码 → 提出方案
    ↓
AI：生成代码（聚焦当前 Feature，不重构无关系统）
    ↓
人：Review Diff → 检查命名、依赖方向、生命周期
    ↓
Unity：编译并运行 → 验证核心流程
    ↓
AI：根据报错和测试结果修复
    ↓
人：确认 Definition of Done → Git Commit
```

---

# 14. 后续计划

| 阶段 | 内容 | 优先级 |
| :--- | :--- | :--- |
| 美术替换 | 玩家/NPC/怪物模型、场景环境、UI 美化 | 高 |
| 动画接入 | 角色移动/攻击/受击动画 | 高 |
| 录制视频 | 3~5 分钟完整流程演示 | 高 |
| README | 项目介绍 + 架构图 + 技术亮点 | 高 |
| 面试准备 | 架构决策、踩坑记录、系统设计问答 | 中 |

---

**------ MintLandDemo 项目开发完成冻结文档 V2.0 ------**