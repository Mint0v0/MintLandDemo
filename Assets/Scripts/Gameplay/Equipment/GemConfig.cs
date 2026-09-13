using UnityEngine;

namespace MintLandDemo.Gameplay.Equipment
{
    /// <summary>
    /// 宝石模板（静态配置）。数值为镶嵌后的属性加成。
    /// </summary>
    [CreateAssetMenu(menuName = "MintLand/Gem/Gem Config")]
    public class GemConfig : ScriptableObject
    {
        public string gemName;
        public int attackBonus;
        public int defenseBonus;
        public int hpBonus;
        public int price;
    }
}
