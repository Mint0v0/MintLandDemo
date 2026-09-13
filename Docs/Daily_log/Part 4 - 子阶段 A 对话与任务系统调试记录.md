### 开发日志：Part 4 - 子阶段 A 对话与任务系统调试记录

---

#### 问题描述

实现村长对话 → 接任务 → NPC1 给药水 → 回村长交任务 → 获得宝剑 → 接击杀任务 的完整流程时，**对话正常播放，但任务状态未写入 GameContext，后续对话无法触发分支**。

具体表现为：
- 村长第一轮对话正常显示，点击“下一步”正常推进。
- 任务 `quest_heal` 未被标记为 `Accepted`。
- 再次与村长对话，仍然重复第一轮对话（“年轻人，村子被史莱姆袭击了……”），无法触发第二轮对话（交药水、获得宝剑）。

---

#### 排查过程

**1. 检查 DialogueManager 日志**
- 日志显示 `[DialogueManager] 动作: AcceptQuest, QuestId: quest_heal`，说明对话节点执行到了 `AcceptQuest` 动作，DialogueManager 正常调用 QuestSystem。

**2. 检查 QuestSystem 日志**
- 在 `AcceptQuest` 方法中添加调试日志后，输出 `[QuestSystem] 1. 进入 AcceptQuest: quest_heal`
- 下一步输出 `[QuestSystem] 2. data = null`
- **初步结论**：`QuestSystem` 中的 `Data` 属性为 `null`，无法访问 `GameRuntimeData`。

**3. 分析 `Data` 属性**
- `QuestSystem.Data` 定义为：`private GameRuntimeData Data => GameRoot.Instance?.Context?.Data;`
- `GameRoot.Instance` 返回 `null` → `Data` 为 `null` → `AcceptQuest` 在 `if (data == null) return;` 处直接返回，任务未创建。

**4. 检查 GameRoot 是否被创建**
- 项目采用 **Bootstrap 场景架构**：`00_Bootstrap` 负责创建 `GameRoot`（DontDestroyOnLoad），`01_NewbieVillage` 为内容场景。
- 调试过程中，用户直接双击 `01_NewbieVillage` 场景进入编辑模式，然后点击 Play。
- **Unity 行为**：如果直接打开 `01_NewbieVillage` 并点击 Play，Unity 会以 `01_NewbieVillage` 作为启动场景，`00_Bootstrap` 不会被加载，`GameRoot` 永远不会被创建。
- **根本原因定位**：`GameRoot.Instance` 为 `null`，因为 `GameRoot` 从未被实例化。

**5. 检查 GameRuntimeData 构造函数**
- 在解决场景问题后，`data` 不再为 `null`，但 `AcceptQuest` 仍然在 `if (data.quests.Exists(...))` 处卡住。
- 日志显示 `[QuestSystem] 2. data = not null`，但没有后续日志（5~8 步）。
- 检查 `GameRuntimeData` 构造函数，发现 `quests = new List<QuestRuntime>();` 未被初始化，`data.quests` 为 `null`。
- 调用 `.Exists()` 时抛出 `NullReferenceException`，被 Unity 静默吞掉，导致方法提前退出。
- **第二个问题定位**：`GameRuntimeData` 构造函数缺少 `quests` 初始化。

---

#### 解决方案

**1. 修复场景启动方式（根本原因）**
- **操作**：从 `00_Bootstrap` 场景启动 Play 模式，再通过 Additive 模式加载 `01_NewbieVillage`。
- **具体步骤**：
  1. 在编辑模式下双击 `00_Bootstrap` 场景，确保其为当前打开场景。
  2. 点击 Play 进入运行模式，确认 Console 出现 `[GameRoot] Initialized successfully.`
  3. 在 Project 窗口中右键 `01_NewbieVillage.unity` → `Open Scene Additive`。
  4. Hierarchy 中出现 `DontDestroyOnLoad`（含 `GameRoot`）和 `01_NewbieVillage` 的场景内容。
- **验证**：`GameRoot.Instance` 不再为 `null`，`QuestSystem.Data` 能正确访问。

**2. 修复 GameRuntimeData 构造函数（第二个问题）**
- **文件**：`Assets/Scripts/Core/Game/GameRuntimeData.cs`
- **修改前**：
```csharp
public class GameRuntimeData
{
    public List<QuestRuntime> quests;  // 未初始化
    // ...
}
```
- **修改后**：
```csharp
public GameRuntimeData()
{
    Player = new PlayerRuntime();
    Inventory = new InventoryRuntime();
    Equipment = new EquipmentRuntime();
    quests = new List<QuestRuntime>();  // ✅ 已初始化
    activeQuest = null;
}
```

---

#### 修复后结果

- `AcceptQuest` 成功创建 `QuestRuntime` 并添加到 `quests` 列表。
- `QuestAcceptedEvent`、`ItemAddedEvent`、`QuestCompletedEvent` 正常发布。
- `QuestTrackerUI` 正确响应事件并更新 UI。
- 村长第二轮对话正常触发，完整流程跑通。

---

#### 技术收获

**1. Bootstrap 场景架构的必要性**
- `GameRoot` 必须由 `00_Bootstrap` 创建并标记 `DontDestroyOnLoad`，以保证跨场景数据持久化。
- 开发过程中必须从 `00_Bootstrap` 启动 Play 模式，否则 `GameRoot.Instance` 为 `null`，所有依赖 `GameRoot` 的系统都会失效。

**2. Additive 场景加载的正确用法**
- 在 Play 模式下加载内容场景时，必须使用 **Additive** 模式，否则会卸载当前场景并销毁 `DontDestroyOnLoad` 对象。
- 快捷键/操作：Project 窗口中右键场景文件 → `Open Scene Additive`。

**3. 纯 C# 数据类的初始化陷阱**
- `GameRuntimeData` 作为纯 C# 类，其集合类型字段（`List<T>`、`Dictionary<K,V>`）必须在构造函数中显式初始化。
- 否则调用 `.Add()`、`.Exists()` 等方法时会抛出 `NullReferenceException`，且 Unity 可能静默吞掉异常，增加调试难度。

**4. 调试方法**
- 在关键方法中添加分步日志（如 `1. 进入方法`、`2. data = ...`、`3. quests = ...`），快速定位 `return` 或异常发生的位置。
- 日志揭示了 `data = null` 和 `quests = null` 两个独立问题，避免将两者混为一谈。

---

#### 后续注意事项

- **始终从 `00_Bootstrap` 启动 Play 模式**，不要直接进入 `01_NewbieVillage`。
- **所有 `GameRuntimeData` 中的集合类型字段**，必须在构造函数中初始化。
- **Part 5 实现 SceneService 后**，自动从 `00_Bootstrap` 跳转到 `01_NewbieVillage`，无需手动 Additive 加载。

---

**记录时间**：2026-09-05
**相关文件**：`GameRoot.cs`、`QuestSystem.cs`、`GameRuntimeData.cs`、`DialogueManager.cs`