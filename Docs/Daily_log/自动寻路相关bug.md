## 自动寻路问题总结

这个功能从"点任务按钮 → 自动寻路 → 到 NPC 停下"到最终稳定运行，一共踩了 **6 个坑**，每个都很有代表性。

---

## 问题 1：自动寻路时没有跑步动画

**现象**：手动 WASD 播 Run，点任务按钮自动寻路时播 Idle，像滑板。

**根因**：`PlayerAnimationBridge` 只读 `CharacterController.velocity`，且只在玩家有 WASD 输入时才读。自动寻路时 WASD 没按，`Speed = 0`。

**解决**：`GetCurrentMoveSpeed()` 同时读两个速度源，取最大值。

```csharp
float inputSpeed = 0f;
Vector2 moveInput = _inputActions.Gameplay.Move.ReadValue<Vector2>();
if (moveInput.sqrMagnitude > 0.01f)
{
    Vector3 ccVel = new Vector3(_characterController.velocity.x, 0f, _characterController.velocity.z);
    inputSpeed = ccVel.magnitude;
}

float agentSpeed = 0f;
if (_navAgent != null && _navAgent.enabled && _navAgent.isOnNavMesh)
{
    Vector3 agentVel = new Vector3(_navAgent.velocity.x, 0f, _navAgent.velocity.z);
    agentSpeed = agentVel.magnitude;
}

return Mathf.Max(inputSpeed, agentSpeed);
```

**关键点**：手动移动必须用**输入过滤**（因为 `CharacterController.velocity` 停止 Move 后不会立刻归零）；自动寻路必须读 `NavMeshAgent.velocity`。

---

## 问题 2：跑到 NPC 面前没停下来

**现象**：玩家一直跑到贴住 NPC 才停，`[NavigationSystem] 到达` 从来没打印。

**根因**：`Update()` 里用 `_agent.hasPath` 判定到达。**NavMeshAgent 到达目的地后 `hasPath = false`**，判断条件永远为 false。

**解决**：改用玩家到目标的**真实距离**判定。

```csharp
float distToTarget = Vector3.Distance(_player.transform.position, _targetPosition);
if (distToTarget <= arrivalDistance) { OnReachedDestination(); }
```

**关键点**：`hasPath` / `remainingDistance` 是给"寻路逻辑"用的，判断"玩家到了没"应该用**位置距离**。

---

## 问题 3：`arrivalDistance = 0.5` 到不了

**现象**：`distToTarget` 从 14.5 一路降到 0.98 就再也不降了，永远差一点点。

**根因**：NPC 有 Collider，玩家被物理阻挡，到不了 0.5 米内。这是**物理碰撞和寻路判定之间**的冲突。

**解决**：`arrivalDistance` 从 `0.5` 改成 `1.5`。

**关键点**：目标是实体（NPC / 怪物）时，`arrivalDistance` 至少要比 Collider 半径大一点。

---

## 问题 4：跑向 NPC 时朝向不对（倒退、侧滑）

**现象**：角色移动方向正确，但面朝方向不对，像螃蟹一样侧着滑。

**根因**：`NavMeshAgent.updateRotation = false` 关闭了 Agent 自动旋转，但**没有任何地方接管旋转**。

**解决**：在 `DriveByNavMeshAgent()` 里手动旋转玩家。

```csharp
if (horizontalVel.sqrMagnitude > 0.01f)
{
    Quaternion targetRot = Quaternion.LookRotation(horizontalVel.normalized);
    transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, RotationSpeed * Time.deltaTime);
}
```

**关键点**：关闭一个系统（Agent 旋转）之后，**必须确保有另一个系统接管它的职责**。

---

## 问题 5：对话后玩家不能移动

**现象**：按 E 对话，结束后玩家原地跑，必须重新点任务按钮才能动。

**根因**：`QuestTrackerPanel` 上挂了 `UIPanelLock`。这是一个**常驻 HUD**，`OnEnable` 会加 `UI` 锁，但因为它一直可见，`OnDisable` 永远不触发，导致 `UI` 锁永久残留。

**解决**：移除 `QuestTrackerPanel` 上的 `UIPanelLock`。**常驻 HUD 不应该用显隐状态作为锁定依据。**

**关键点**：加了组件的地方，要搞清楚它的生命周期——一次性面板能加，常驻 HUD 不能加。

---

## 问题 6：Inspector 数值改了不生效

**现象**：脚本里 `arrivalDistance = 1.5f`，Inspector 也改成 1.5，运行时还是 0.5。

**根因**：GameRoot 是 **`DontDestroyOnLoad` 单例**，从 Bootstrap 场景启动。你改 `01_NewbieVillage` 场景的 GameRoot 实例，**运行时用的是 Bootstrap 里那个**，值完全无关。

**解决**：在 `Awake()` 里硬编码。

```csharp
private void Awake()
{
    // ...
    arrivalDistance = 1.5f;
    debugNavigation = false;
    // ...
}
```

**关键点**：**跨场景单例的值不要用 Inspector 配置**，代码硬编码最稳。

---

## 最终架构

```
点击任务按钮
    ↓
NavigationSystem.NavigateToQuest(questId)
    ↓
1. 目标在其他场景？→ SceneService.LoadScene → SceneChangedEvent → 继续
2. 目标在当前场景 → StartNavigation
    ├── 找 NPC，记录 _targetPosition
    ├── 配置 NavMeshAgent (updatePosition=false, updateRotation=false)
    ├── 加 Navigating 锁
    └── 每帧 Update：
        ├── 玩家按 WASD/攻击 → CancelNavigation
        ├── distance(player, target) <= 1.5 → OnReachedDestination
        └── PlayerController.DriveByNavMeshAgent 用 CC 推玩家 + 手动旋转
    ↓
到达 / 取消 / 对话触发 → ReleaseNavigationLock → 玩家恢复控制
```

**关键设计**：
- **唯一位移驱动**：玩家位置只由 `CharacterController.Move` 驱动，NavMeshAgent 只提供 `desiredVelocity`。
- **唯一旋转控制**：`DriveByNavMeshAgent` 手动 `LookRotation`。
- **统一锁系统**：`Navigating` 是 `PlayerLockReason` 的一个位，通过 `AddLock/RemoveLock` 管理。
- **对话优先**：`DialogueManager.StartDialogue` 里调用 `CancelNavigation`，避免状态叠加。

---

## 面试时怎么讲

> "我做的自动寻路用 NavMeshAgent 计算路径，但**位置和旋转都由 CharacterController 接管**——Agent 只提供 `desiredVelocity`，不直接改 transform。这样避免了 Agent 和 CC 抢位置的问题。
>
> 到达判定不依赖 Agent 的 `hasPath`（它在到达后会变 false），而是用**玩家到目标的真实距离**，因为目标 NPC 有 Collider，玩家只能靠近到约 1 米处。
>
> 取消机制上，玩家按 WASD 或攻击键随时可以打断寻路，通过 `PlayerLockReason.Navigating` 锁控制输入。所有锁用位掩码管理，可以叠加。"
>
> "过程中踩过一个 Unity 的经典坑：GameRoot 是 DontDestroyOnLoad 单例，从 Bootstrap 场景启动，所以在其他场景改它的 Inspector 值不生效。**跨场景单例的配置一律代码硬编码，不走 Inspector**。"

---

## 你学到的 6 个通用经验

1. **两个系统同时改 transform 一定打架**——选一个作为"位置驱动者"，另一个只提供速度输入。
2. **判断"到达"用距离，不用 `hasPath`**——后者在到达后会变 false。
3. **目标有 Collider 时，`arrivalDistance` 要留余地**——NPC / 怪物 / 宝箱都算。
4. **关闭系统的某个功能后，要确保有别的系统接管**——`updateRotation = false` 之后必须手动旋转。
5. **常驻 HUD 不要挂 `UIPanelLock`**——生命周期不匹配，锁会残留。
6. **跨场景单例的值不要用 Inspector 配置**——改用代码硬编码。

这套自动寻路 + 锁系统的组合，面试时能讲的东西非常多，是一个完整的"**逻辑驱动表现 + 表现反馈逻辑**"的典型案例。你现在整个 Demo 的 Player 表现层已经非常扎实了。