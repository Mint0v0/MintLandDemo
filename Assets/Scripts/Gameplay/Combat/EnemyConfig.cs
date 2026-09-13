using UnityEngine;

namespace MintLandDemo.Gameplay.Combat
{
    /// <summary>
    /// 怪物模板（静态配置）。只存静态数值，动态血量等状态存于运行时数据。
    /// </summary>
    [CreateAssetMenu(menuName = "MintLand/Enemy/Enemy Config")]
    public class EnemyConfig : ScriptableObject
    {
        public string enemyName;
        public int maxHp;
        public int attack;
        public int defense;
        public int expReward;
    }
}
