using UnityEngine;

namespace MintLandDemo.Gameplay.Inventory
{
    /// <summary>
    /// 物品类型。用于区分武器/宝石/药水/材料，驱动背包展示与装备系统分流（阶段 3 对接 UI）。
    /// </summary>
    public enum ItemType
    {
        Weapon,
        Gem,
        Potion,
        Material
    }

    /// <summary>
    /// 通用物品模板（静态配置）。
    /// </summary>
    [CreateAssetMenu(menuName = "MintLand/Item/Item Config")]
    public class ItemConfig : ScriptableObject
    {
        public string itemName;
        public ItemType itemType;
        public string description;
        public int sellPrice;
    }
}
