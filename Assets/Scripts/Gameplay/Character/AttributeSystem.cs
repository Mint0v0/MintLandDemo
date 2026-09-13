using UnityEngine;
using MintLandDemo.Core.Event;
using MintLandDemo.Core.Game;
using MintLandDemo.Gameplay.Equipment;

namespace MintLandDemo.Gameplay.Character
{
    /// <summary>
    /// 属性计算系统（纯静态类）。
    /// 把「基础属性 + 当前激活武器的装备加成 + 其宝石加成」合成为最终属性，写入 PlayerRuntime 的 finalXxx 缓存字段。
    /// 阶段 5：只取 activeWeaponIndex 对应的武器与宝石加成，不再遍历所有武器。
    /// </summary>
    public static class AttributeSystem
    {
        public static PlayerRuntime CalculateFinalAttributes(PlayerRuntime player, EquipmentRuntime equipment)
        {
            int attack = player.baseAttack;
            int defense = player.baseDefense;
            int maxHp = player.baseMaxHp;

            if (equipment != null)
            {
                WeaponSlotData activeSlot = equipment.ActiveWeaponSlot;

                // 装备加成：仅当前激活武器
                if (activeSlot != null && activeSlot.config != null)
                {
                    EquipmentConfig weapon = activeSlot.config;
                    attack += weapon.attackBonus;
                    defense += weapon.defenseBonus;
                    maxHp += weapon.hpBonus;
                }

                // 宝石加成：仅当前激活武器的宝石
                if (activeSlot != null && activeSlot.gems != null)
                {
                    foreach (GemConfig gem in activeSlot.gems)
                    {
                        if (gem == null) continue;
                        attack += gem.attackBonus;
                        defense += gem.defenseBonus;
                        maxHp += gem.hpBonus;
                    }
                }
            }

            player.finalAttack = attack;
            player.finalDefense = defense;
            player.finalMaxHp = maxHp;

            return player;
        }

        /// <summary>
        /// 装备变更回调（由 GameRoot 在 Start 中订阅）。重新计算最终属性并发布 AttributeChangedEvent。
        /// 注意：本方法承担「事件胶水」职责；CalculateFinalAttributes 仍保持纯数学、不查 Unity 对象。
        /// </summary>
        public static void OnEquipmentChanged(EquipmentChangedEvent evt)
        {
            GameRuntimeData data = GameRoot.Instance?.Context?.Data;
            if (data == null) return;

            CalculateFinalAttributes(data.Player, data.Equipment);

            EventBus.Publish(new AttributeChangedEvent
            {
                finalAttack = data.Player.finalAttack,
                finalDefense = data.Player.finalDefense,
                finalMaxHp = data.Player.finalMaxHp
            });

            Debug.Log($"[AttributeSystem] 属性重算: 最终攻击 = {data.Player.finalAttack}, 最终防御 = {data.Player.finalDefense}, 最终生命 = {data.Player.finalMaxHp}");
        }
    }
}
