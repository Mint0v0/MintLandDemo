using System;
using System.Collections.Generic;
using UnityEngine;
using MintLandDemo.Core.Event;
using MintLandDemo.Core.Game;
using MintLandDemo.Gameplay.Inventory;

namespace MintLandDemo.Gameplay.Equipment
{
    /// <summary>
    /// 装备系统。持有 EquipmentRuntime（来自 GameContext），负责武器装备/卸下/切换与宝石镶嵌/卸下。
    /// 阶段 5：宝石改为按武器槽位独立管理；activeWeaponIndex 表示当前激活武器（影响最终属性）。
    /// 装备变化通过发布 EquipmentChangedEvent 驱动 AttributeSystem 重算，不直接改数值。
    /// </summary>
    public class EquipmentSystem : MonoBehaviour
    {
        [Header("初始武器（首次启动放入槽位 0）")]
        [SerializeField] private EquipmentConfig defaultWeapon;

        [Header("武器 Config 注册表（按 equipmentName 回查模板）")]
        [SerializeField] private List<EquipmentConfig> weaponConfigs = new List<EquipmentConfig>();

        [Header("宝石 Config 注册表（按 gemName 回查模板）")]
        [SerializeField] private List<GemConfig> gemConfigs = new List<GemConfig>();

        private EquipmentRuntime Equip => GameRoot.Instance?.Context?.Data?.Equipment;

        private void Start()
        {
            // 仅首次启动（无存档）时自动装备初始武器到槽位 0，避免读档后覆盖玩家已卸下的状态。
            bool firstRun = !(GameRoot.Instance?.SaveService?.HasSave() ?? false);
            if (firstRun && Equip != null && Equip.weaponSlots != null && Equip.weaponSlots.Count > 0
                && Equip.weaponSlots[0].config == null && defaultWeapon != null)
            {
                Equip.weaponSlots[0].config = defaultWeapon;
                PublishAndSave(defaultWeapon.equipmentName);
                Debug.Log($"[EquipmentSystem] 初始装备武器: {defaultWeapon.equipmentName}（槽位 0）");
            }
        }

        /// <summary>装备武器到指定槽位（槽位占用时先卸下旧武器及其宝石，不重复发布事件）。</summary>
        public bool EquipWeapon(int slotIndex, string weaponItemId)
        {
            EquipmentRuntime equip = Equip;
            if (equip == null || equip.weaponSlots == null)
            {
                Debug.LogWarning("[EquipmentSystem] 装备数据为空。");
                return false;
            }
            if (slotIndex < 0 || slotIndex >= equip.MaxWeaponSlots)
            {
                Debug.LogWarning($"[EquipmentSystem] 武器槽位 {slotIndex} 无效（共 {equip.MaxWeaponSlots} 槽）。");
                return false;
            }

            EquipmentConfig weapon = FindWeaponConfig(weaponItemId);
            if (weapon == null)
            {
                Debug.LogWarning($"[EquipmentSystem] 未找到武器配置: {weaponItemId}。");
                return false;
            }
            if (!InventorySystem.HasItem(weaponItemId, 1))
            {
                Debug.LogWarning($"[EquipmentSystem] 背包中没有武器 {weaponItemId}。");
                return false;
            }

            WeaponSlotData slot = equip.weaponSlots[slotIndex];
            if (slot.config != null)
            {
                UnequipWeaponInternal(slot);
            }

            slot.config = weapon;
            InventorySystem.RemoveItem(weaponItemId, 1);

            PublishAndSave(weapon.equipmentName);
            Debug.Log($"[EquipmentSystem] 装备武器 {weapon.equipmentName} 到槽位 {slotIndex}");
            return true;
        }

        /// <summary>卸下指定槽位的武器；该槽位宝石一并归还背包并发布事件。</summary>
        public bool UnequipWeapon(int slotIndex)
        {
            EquipmentRuntime equip = Equip;
            if (equip == null || equip.weaponSlots == null)
            {
                Debug.LogWarning("[EquipmentSystem] 装备数据为空。");
                return false;
            }
            if (equip.IsWeaponSlotEmpty(slotIndex))
            {
                Debug.LogWarning($"[EquipmentSystem] 槽位 {slotIndex} 没有武器。");
                return false;
            }

            WeaponSlotData slot = equip.weaponSlots[slotIndex];
            string weaponName = slot.config.equipmentName;
            UnequipWeaponInternal(slot);

            PublishAndSave(weaponName);
            Debug.Log($"[EquipmentSystem] 卸下武器 {weaponName}（槽位 {slotIndex}），宝石一并归还背包。");
            return true;
        }

        /// <summary>切换激活武器到指定槽位（空槽 = 空手，属性只算基础值）。</summary>
        public bool SwitchActiveWeapon(int slotIndex)
        {
            EquipmentRuntime equip = Equip;
            if (equip == null || equip.weaponSlots == null)
            {
                Debug.LogWarning("[EquipmentSystem] 装备数据为空。");
                return false;
            }
            if (slotIndex < 0 || slotIndex >= equip.MaxWeaponSlots)
            {
                Debug.LogWarning($"[EquipmentSystem] 武器槽位 {slotIndex} 无效。");
                return false;
            }

            if (equip.activeWeaponIndex == slotIndex)
            {
                return true; // 已是激活槽位，无需切换。
            }

            equip.activeWeaponIndex = slotIndex;
            WeaponSlotData slot = equip.weaponSlots[slotIndex];
            string weaponName = slot.config != null ? slot.config.equipmentName : "";

            PublishAndSave(weaponName);
            Debug.Log($"[EquipmentSystem] 切换激活武器到槽位 {slotIndex}（{(slot.config != null ? slot.config.equipmentName : "空手")}）。");
            return true;
        }

        /// <summary>将宝石镶嵌到指定武器槽位的宝石槽，从背包扣除宝石并发布事件。</summary>
        public bool EquipGem(int weaponSlotIndex, int gemSlotIndex, string gemId)
        {
            EquipmentRuntime equip = Equip;
            if (equip == null || equip.weaponSlots == null)
            {
                Debug.LogWarning("[EquipmentSystem] 装备数据为空。");
                return false;
            }
            if (weaponSlotIndex < 0 || weaponSlotIndex >= equip.MaxWeaponSlots)
            {
                Debug.LogWarning($"[EquipmentSystem] 武器槽位 {weaponSlotIndex} 无效（共 {equip.MaxWeaponSlots} 槽）。");
                return false;
            }

            WeaponSlotData slot = equip.weaponSlots[weaponSlotIndex];
            if (slot.config == null)
            {
                Debug.LogWarning($"[EquipmentSystem] 槽位 {weaponSlotIndex} 未装备武器，无法镶嵌宝石。");
                return false;
            }
            if (gemSlotIndex < 0 || gemSlotIndex >= slot.gemSlotCount)
            {
                Debug.LogWarning($"[EquipmentSystem] 宝石槽位 {gemSlotIndex} 无效（共 {slot.gemSlotCount} 槽）。");
                return false;
            }

            GemConfig gem = FindGemConfig(gemId);
            if (gem == null)
            {
                Debug.LogWarning($"[EquipmentSystem] 未找到宝石配置: {gemId}。");
                return false;
            }
            if (slot.gems.Count > gemSlotIndex && slot.gems[gemSlotIndex] != null)
            {
                Debug.LogWarning($"[EquipmentSystem] 宝石槽位 {gemSlotIndex} 已被占用。");
                return false;
            }
            if (!InventorySystem.RemoveItem(gem.gemName, 1))
            {
                Debug.LogWarning($"[EquipmentSystem] 背包中无宝石 {gem.gemName}。");
                return false;
            }

            while (slot.gems.Count <= gemSlotIndex) slot.gems.Add(null);
            slot.gems[gemSlotIndex] = gem;

            PublishAndSave(slot.config.equipmentName);
            Debug.Log($"[EquipmentSystem] 镶嵌 {gem.gemName} 到武器 {slot.config.equipmentName} 宝石槽位 {gemSlotIndex}");
            return true;
        }

        /// <summary>卸下指定武器槽位某宝石槽的宝石，归还背包并发布事件。</summary>
        public bool UnequipGem(int weaponSlotIndex, int gemSlotIndex)
        {
            EquipmentRuntime equip = Equip;
            if (equip == null || equip.weaponSlots == null)
            {
                Debug.LogWarning("[EquipmentSystem] 装备数据为空。");
                return false;
            }
            if (weaponSlotIndex < 0 || weaponSlotIndex >= equip.MaxWeaponSlots)
            {
                Debug.LogWarning($"[EquipmentSystem] 武器槽位 {weaponSlotIndex} 无效。");
                return false;
            }

            WeaponSlotData slot = equip.weaponSlots[weaponSlotIndex];
            if (slot.config == null)
            {
                Debug.LogWarning($"[EquipmentSystem] 槽位 {weaponSlotIndex} 未装备武器，无法卸下宝石。");
                return false;
            }
            if (gemSlotIndex < 0 || gemSlotIndex >= slot.gemSlotCount)
            {
                Debug.LogWarning($"[EquipmentSystem] 宝石槽位 {gemSlotIndex} 无效（共 {slot.gemSlotCount} 槽）。");
                return false;
            }
            if (slot.gems == null || slot.gems.Count <= gemSlotIndex || slot.gems[gemSlotIndex] == null)
            {
                Debug.LogWarning($"[EquipmentSystem] 宝石槽位 {gemSlotIndex} 为空，无可卸下的宝石。");
                return false;
            }

            GemConfig gem = slot.gems[gemSlotIndex];
            slot.gems[gemSlotIndex] = null;
            InventorySystem.AddItem(gem.gemName, 1);

            PublishAndSave(slot.config.equipmentName);
            Debug.Log($"[EquipmentSystem] 卸下宝石 {gem.gemName}（武器 {slot.config.equipmentName} 宝石槽位 {gemSlotIndex}），归还背包。");
            return true;
        }

        /// <summary>返回指定武器槽位（越界返回 null）。</summary>
        public WeaponSlotData GetWeaponSlot(int slotIndex)
        {
            EquipmentRuntime equip = Equip;
            if (equip == null || equip.weaponSlots == null) return null;
            if (slotIndex < 0 || slotIndex >= equip.weaponSlots.Count) return null;
            return equip.weaponSlots[slotIndex];
        }

        /// <summary>返回当前激活武器槽位索引。</summary>
        public int GetActiveWeaponIndex()
        {
            EquipmentRuntime equip = Equip;
            return equip != null ? equip.activeWeaponIndex : 0;
        }

        /// <summary>返回当前所有已装备武器（非空）的列表。</summary>
        public List<EquipmentConfig> GetEquippedWeapons()
        {
            List<EquipmentConfig> result = new List<EquipmentConfig>();
            EquipmentRuntime equip = Equip;
            if (equip == null || equip.weaponSlots == null) return result;

            foreach (WeaponSlotData slot in equip.weaponSlots)
            {
                if (slot != null && slot.config != null) result.Add(slot.config);
            }
            return result;
        }

        /// <summary>判断背包物品 ID 是否为武器（按 weaponConfigs 回查）。供背包 UI 过滤武器用。</summary>
        public bool IsWeaponItem(string itemId)
        {
            if (string.IsNullOrEmpty(itemId) || weaponConfigs == null) return false;
            foreach (var config in weaponConfigs)
            {
                if (config != null && string.Equals(itemId, config.equipmentName, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }

        /// <summary>判断背包物品 ID 是否为宝石（按 gemConfigs 回查）。供背包 UI 过滤宝石用。</summary>
        public bool IsGemItem(string itemId)
        {
            if (string.IsNullOrEmpty(itemId) || gemConfigs == null) return false;
            foreach (var config in gemConfigs)
            {
                if (config != null && string.Equals(itemId, config.gemName, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }

        /// <summary>卸下槽位武器与全部宝石（归还背包，不发布事件；供 EquipWeapon/UnequipWeapon 内部复用）。</summary>
        private void UnequipWeaponInternal(WeaponSlotData slot)
        {
            if (slot.config != null)
            {
                InventorySystem.AddItem(slot.config.equipmentName, 1);
            }
            if (slot.gems != null)
            {
                foreach (GemConfig gem in slot.gems)
                {
                    if (gem != null) InventorySystem.AddItem(gem.gemName, 1);
                }
                slot.gems.Clear();
            }
            slot.config = null;
        }

        /// <summary>按 equipmentName 回查武器配置（忽略大小写）。</summary>
        private EquipmentConfig FindWeaponConfig(string weaponItemId)
        {
            if (string.IsNullOrEmpty(weaponItemId)) return null;
            return weaponConfigs.Find(c => c != null && string.Equals(c.equipmentName, weaponItemId, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>按 gemName 回查宝石配置（忽略大小写）。</summary>
        private GemConfig FindGemConfig(string gemName)
        {
            if (string.IsNullOrEmpty(gemName)) return null;
            return gemConfigs.Find(c => c != null && string.Equals(c.gemName, gemName, StringComparison.OrdinalIgnoreCase));
        }

        private void PublishAndSave(string equipmentId)
        {
            EventBus.Publish(new EquipmentChangedEvent { equipmentId = equipmentId });
            GameRuntimeData data = GameRoot.Instance?.Context?.Data;
            GameRoot.Instance?.SaveService?.Save(data);
        }
    }
}