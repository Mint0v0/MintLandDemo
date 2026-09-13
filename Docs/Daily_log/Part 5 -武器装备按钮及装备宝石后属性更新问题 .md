## 问题一：点击「装备」按钮没有弹出武器列表

### 问题现象
在装备面板（I）中，点击武器槽位下方的「装备」按钮（空槽位时），没有弹出背包武器列表，无法选择武器进行装备。

### 排查过程
1. 在 `InventoryUI.cs` 的 `OpenWithFilter` 方法中加入 Debug 日志，确认 `OpenWithFilter` 被正确调用。
2. 日志显示 `RefreshFilteredList` 正常执行，`filteredListContainer` 已激活，`filteredButtonPrefab` 已配置。
3. 日志进一步显示：背包中有 `sword × 1`，但 `PassFilter` 对 `sword` 返回 `False`。
4. 定位到 `EquipmentSystem.IsWeaponItem` 方法：它使用 `c.equipmentName == weaponItemId` 进行**精确大小写匹配**，而背包中存的是小写 `"sword"`，`weaponConfigs` 中注册的是大写 `"Sword"`，导致匹配失败。

### 根本原因
`FindWeaponConfig` 和 `FindGemConfig` 使用 `==` 进行字符串比较，没有忽略大小写，导致物品 ID 和配置名的大小写不一致时无法匹配。

### 解决方案
将 `EquipmentSystem.cs` 中的字符串比较改为 `StringComparison.OrdinalIgnoreCase`：

```csharp
// 修改前
return weaponConfigs.Find(c => c != null && c.equipmentName == weaponItemId);

// 修改后
return weaponConfigs.Find(c => c != null && string.Equals(c.equipmentName, weaponItemId, StringComparison.OrdinalIgnoreCase));
```

同时需要在文件头部添加 `using System;`。

### 验证结果
`IsWeaponItem("sword")` 正确返回 `True`，装备按钮正常弹出武器列表，武器可以正常装备和卸下。


## 问题二：装备宝石后属性没有更新

### 问题现象
在装备面板中，点击宝石槽的 `+` 按钮，选择背包中的宝石进行镶嵌后，Console 日志显示 `[AttributeSystem] 属性重算` 已执行，且属性计算正确（例如攻击从 20 变为 25），但 UI 面板上的攻击/防御/血量数值没有变化。

### 排查过程
1. 检查 Console 日志，确认 `EquipmentSystem.EquipGem` 成功执行，`EquipmentChangedEvent` 已发布，`AttributeSystem.OnEquipmentChanged` 已响应并完成重算。
2. 检查 `EquipmentUI.RefreshUI()` 方法，发现它只读取了武器的加成：

```csharp
attackBonusText.text = $"攻击 +{(weapon != null ? weapon.attackBonus : 0)}";
```

3. 该逻辑没有计算该武器槽位上已镶嵌宝石的加成，因此即使 `AttributeSystem` 计算了正确的最终属性，UI 也不会显示宝石带来的数值变化。

### 根本原因
`EquipmentUI.RefreshUI()` 只负责显示武器本身的加成，没有将宝石加成纳入 UI 数值的计算范围。UI 显示的是“武器加成”，而不是“武器 + 宝石的最终加成”。

### 解决方案
修改 `EquipmentUI.RefreshUI()` 方法，在显示属性时遍历当前激活武器槽位的宝石列表，累加所有宝石的加成：

```csharp
int totalAttack = weapon != null ? weapon.attackBonus : 0;
int totalDefense = weapon != null ? weapon.defenseBonus : 0;
int totalHp = weapon != null ? weapon.hpBonus : 0;

if (activeSlot != null && activeSlot.gems != null)
{
    foreach (GemConfig gem in activeSlot.gems)
    {
        if (gem != null)
        {
            totalAttack += gem.attackBonus;
            totalDefense += gem.defenseBonus;
            totalHp += gem.hpBonus;
        }
    }
}

attackBonusText.text = $"攻击 +{totalAttack}";
defenseBonusText.text = $"防御 +{totalDefense}";
hpBonusText.text = $"血量 +{totalHp}";
```

### 验证结果
镶嵌 Ruby（攻击 +5）后，UI 攻击显示从 `+10` 变为 `+15`；镶嵌 Sapphire（防御 +5）后，UI 防御显示从 `+0` 变为 `+5`。属性面板与 `AttributeSystem` 的实际计算结果保持一致。


## 涉及文件

| 文件 | 改动内容 |
| :--- | :--- |
| `EquipmentSystem.cs` | `FindWeaponConfig` 和 `FindGemConfig` 的字符串比较改为忽略大小写；添加 `using System;` |
| `EquipmentUI.cs` | `RefreshUI()` 中属性显示增加宝石加成计算 |