using System.Collections.Generic;
using MintLandDemo.Gameplay.Equipment;

namespace MintLandDemo.Core.Game
{
    /// <summary>
    /// 运行时动态数据根容器。
    /// 纯 C# 类：不继承 MonoBehaviour，不引用任何 Unity 引擎类型（Transform/GameObject 等）。
    /// 跨场景持久化数据统一挂在 GameContext.Data 下。
    /// </summary>
    [System.Serializable]
    public class GameRuntimeData
    {
        public PlayerRuntime Player;
        public InventoryRuntime Inventory;
        public EquipmentRuntime Equipment;

        public List<QuestRuntime> quests = new List<QuestRuntime>();
        public QuestRuntime activeQuest; // 当前进行中的主线任务（V1 简化）
        public string currentScene = "01_NewbieVillage"; // 当前所在场景（存档恢复用）

        public GameRuntimeData()
        {
            Player = new PlayerRuntime();
            Inventory = new InventoryRuntime();
            Equipment = new EquipmentRuntime();
            quests = new List<QuestRuntime>();
            activeQuest = null;
        }
    }

    /// <summary>
    /// 玩家运行时数据。字段分三层：
    /// - baseXxx：未受装备/宝石加成前的原始值（静态基准）。
    /// - finalXxx：AttributeSystem 计算后的最终值（缓存结果）。
    /// - currentHp：战斗时会变化的当前血量（动态状态）。
    /// </summary>
    [System.Serializable]
    public class PlayerRuntime
    {
        // 等级（V1 暂不实现升级加属性，仅占位）
        public int level = 1;

        // 基础属性（未受装备/宝石加成前的原始值）
        public int baseAttack = 10;
        public int baseDefense = 5;
        public int baseMaxHp = 500;

        // 最终属性（装备/宝石加成后的缓存结果，由 AttributeSystem 写入）
        public int finalAttack;
        public int finalDefense;
        public int finalMaxHp;

        // 当前动态状态（战斗时会变化）
        public int currentHp = 500;

        // 移动参数（Part 2 使用）
        public float moveSpeed = 5f;
        public float rotationSpeed = 10f;
    }

    /// <summary>
    /// 背包运行时数据。items 为「物品 ID → 数量」的列表（V1 简单实现）。
    /// 用 List&lt;ItemEntry&gt; 而非 Dictionary，以兼容 JsonUtility 序列化（Part 5 存档）。
    /// </summary>
    [System.Serializable]
    public class InventoryRuntime
    {
        public int gold = 500;
        public List<ItemEntry> items = new List<ItemEntry>();
    }

    /// <summary>
    /// 背包条目（itemId → 数量）。纯 C# 数据，可被 JsonUtility 序列化。
    /// </summary>
    [System.Serializable]
    public class ItemEntry
    {
        public string itemId;
        public int count;
    }

    /// <summary>
    /// 任务运行时数据。任务进度落地在 GameContext，而非 MonoBehaviour 临时变量。
    /// </summary>
    [System.Serializable]
    public class QuestRuntime
    {
        public string questId;
        public string questName;
        public string description;
        public int currentProgress;
        public int targetProgress;
        public bool isAccepted;
        public bool isCompleted;
    }

    /// <summary>
    /// 装备运行时数据。持有对静态 Config 的引用（模板），而非复制数值。
    /// 阶段 5：武器与宝石按「槽位」组织，每个槽位独立持有武器 Config 与宝石列表；
    /// activeWeaponIndex 表示当前激活（影响最终属性）的武器槽位。
    /// </summary>
    [System.Serializable]
    public class EquipmentRuntime
    {
        public List<WeaponSlotData> weaponSlots = new List<WeaponSlotData>()
        {
            new WeaponSlotData(),  // 槽位 0（武器1）
            new WeaponSlotData()   // 槽位 1（武器2）
        };
        public int activeWeaponIndex = 0;

        /// <summary>武器槽位上限（V1 固定 2 槽）。</summary>
        public int MaxWeaponSlots => 2;

        /// <summary>当前激活的武器槽（activeWeaponIndex 越界时兜底到槽位 0）。</summary>
        public WeaponSlotData ActiveWeaponSlot
        {
            get
            {
                if (weaponSlots == null || weaponSlots.Count == 0) return null;
                int idx = (activeWeaponIndex >= 0 && activeWeaponIndex < weaponSlots.Count) ? activeWeaponIndex : 0;
                return weaponSlots[idx];
            }
        }

        /// <summary>判断指定武器槽位是否为空（越界/空引用均视为空）。</summary>
        public bool IsWeaponSlotEmpty(int slotIndex)
        {
            return slotIndex < 0 || slotIndex >= weaponSlots.Count || weaponSlots[slotIndex].config == null;
        }
    }

    /// <summary>
    /// 单个武器槽位数据。持有武器 Config（null = 空槽）与该槽位独立的宝石列表。
    /// 宝石数量受 config.gemSlotCount 限制（V1 为 2）。
    /// </summary>
    [System.Serializable]
    public class WeaponSlotData
    {
        public EquipmentConfig config;
        public List<GemConfig> gems = new List<GemConfig>();

        public int gemSlotCount => config != null ? config.gemSlotCount : 0;
    }
}
