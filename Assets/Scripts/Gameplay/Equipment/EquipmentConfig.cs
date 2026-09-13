using UnityEngine;

namespace MintLandDemo.Gameplay.Equipment
{
    /// <summary>
    /// 装备模板（静态配置）。V1 宝剑的槽位数为 2。
    /// </summary>
    [CreateAssetMenu(menuName = "MintLand/Equipment/Equipment Config")]
    public class EquipmentConfig : ScriptableObject
    {
        public string equipmentName;
        public int attackBonus;
        public int defenseBonus;
        public int hpBonus;
        public int gemSlotCount;
    }
}
