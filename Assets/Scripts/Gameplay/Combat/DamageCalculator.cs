using UnityEngine;

namespace MintLandDemo.Gameplay.Combat
{
    /// <summary>
    /// 伤害计算（纯静态类）。仅使用 UnityEngine.Mathf 基础数学库。
    /// 规则：伤害 = 攻击 - 防御，最低保底 1 点。
    /// </summary>
    public static class DamageCalculator
    {
        public static int CalculateDamage(int attackerFinalAttack, int targetFinalDefense)
        {
            int damage = attackerFinalAttack - targetFinalDefense;
            return Mathf.Max(1, damage);
        }
    }
}
