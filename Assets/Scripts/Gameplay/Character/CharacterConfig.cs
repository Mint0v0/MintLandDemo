using UnityEngine;

namespace MintLandDemo.Gameplay.Character
{
    /// <summary>
    /// 角色模板（静态配置）。ScriptableObject 只存静态数值，严禁存放当前血量等动态状态。
    /// V1 主要给玩家与史莱姆等角色使用。
    /// </summary>
    [CreateAssetMenu(menuName = "MintLand/Character/Character Config")]
    public class CharacterConfig : ScriptableObject
    {
        public string displayName;
        public int baseAttack;
        public int baseDefense;
        public int baseMaxHp;
    }
}
