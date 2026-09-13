using System;
using System.Collections.Generic;
using MintLandDemo.Core.Game;

namespace MintLandDemo.Infrastructure.Save
{
    /// <summary>
    /// 存档 DTO（纯数据，JSON 可序列化）。
    /// 与 GameRuntimeData 的区别：不含 Dictionary 和 ScriptableObject 引用，
    /// 装备/宝石用字符串 ID 表示，加载时按名称回查 Config。
    /// </summary>
    [Serializable]
    public class SaveData
    {
        // 玩家
        public int level;
        public int baseAttack;
        public int baseDefense;
        public int baseMaxHp;
        public int currentHp;
        public float moveSpeed;
        public float rotationSpeed;

        // 背包
        public int gold;
        public List<ItemEntry> items;

        // 装备（字符串 ID）
        public List<string> equippedWeaponIds; // 旧：多槽位武器 ID（阶段2-4，向后兼容读档）
        public string equippedWeaponId;        // 旧：单武器 ID（向后兼容）
        public List<string> equippedGemIds;    // 旧：全局宝石 ID（向后兼容）

        // 装备（阶段5：每槽位独立）
        public List<WeaponSlotSaveData> weaponSlots;
        public int activeWeaponIndex;

        // 任务
        public List<QuestRuntime> quests;
        public string activeQuestId;

        // 场景
        public string currentScene;
    }

    /// <summary>
    /// 单个武器槽位的存档 DTO：武器名 + 宝石名列表（均为字符串，加载时按名称回查 Config）。
    /// </summary>
    [Serializable]
    public class WeaponSlotSaveData
    {
        public string weaponName = "";                        // 武器名（"" = 空槽）
        public List<string> gemNames = new List<string>();    // 该槽位的宝石名列表
    }
}
