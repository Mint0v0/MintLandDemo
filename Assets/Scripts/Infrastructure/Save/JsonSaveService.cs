using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using MintLandDemo.Core.Game;
using MintLandDemo.Gameplay.Equipment;

namespace MintLandDemo.Infrastructure.Save
{
    /// <summary>
    /// JSON 存档服务。将 GameRuntimeData 转换为 SaveData（纯 JSON 结构）后序列化，
    /// 存到 Application.persistentDataPath/save.json。
    /// 只持久化「字符串 ID + 数值」，不存 MonoBehaviour/ScriptableObject 引用（加载时按名称回查 Config）。
    /// 阶段 5：装备改为按槽位存储（每槽位的武器名 + 宝石名列表），并持久化 activeWeaponIndex。
    /// </summary>
    public class JsonSaveService : MonoBehaviour, ISaveService
    {
        private const string SaveFileName = "save.json";

        [Header("Config 注册表（存档加载时按名称回查模板）")]
        [SerializeField] private List<EquipmentConfig> equipmentConfigs = new List<EquipmentConfig>();
        [SerializeField] private List<GemConfig> gemConfigs = new List<GemConfig>();

        private string SavePath => Path.Combine(Application.persistentDataPath, SaveFileName);

        public void Save(GameRuntimeData data)
        {
            if (data == null) return;

            SaveData saveData = BuildSaveData(data);
            string json = JsonUtility.ToJson(saveData, true);
            try
            {
                File.WriteAllText(SavePath, json);
                Debug.Log($"[SaveService] 存档成功 -> {SavePath}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveService] 存档失败: {e.Message}");
            }
        }

        public bool TryLoad(out GameRuntimeData data)
        {
            data = null;

            if (!File.Exists(SavePath))
            {
                Debug.Log("[SaveService] 无存档，使用默认数据");
                return false;
            }

            try
            {
                string json = File.ReadAllText(SavePath);
                SaveData saveData = JsonUtility.FromJson<SaveData>(json);
                if (saveData == null)
                {
                    Debug.LogWarning("[SaveService] 存档内容为空，使用默认数据");
                    return false;
                }

                data = RestoreRuntime(saveData);
                Debug.Log("[SaveService] 读取存档成功");
                return true;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[SaveService] 读档失败: {e.Message}");
                return false;
            }
        }

        public bool HasSave()
        {
            return File.Exists(SavePath);
        }

        public void DeleteSave()
        {
            if (File.Exists(SavePath))
            {
                File.Delete(SavePath);
                Debug.Log("[SaveService] 存档已删除");
            }
        }

        private SaveData BuildSaveData(GameRuntimeData data)
        {
            List<WeaponSlotSaveData> slots = new List<WeaponSlotSaveData>();
            if (data.Equipment.weaponSlots != null)
            {
                foreach (WeaponSlotData slot in data.Equipment.weaponSlots)
                {
                    WeaponSlotSaveData s = new WeaponSlotSaveData();
                    s.weaponName = (slot != null && slot.config != null) ? slot.config.equipmentName : "";
                    if (slot != null && slot.gems != null)
                    {
                        foreach (GemConfig gem in slot.gems)
                        {
                            s.gemNames.Add(gem != null ? gem.gemName : "");
                        }
                    }
                    slots.Add(s);
                }
            }

            return new SaveData
            {
                level = data.Player.level,
                baseAttack = data.Player.baseAttack,
                baseDefense = data.Player.baseDefense,
                baseMaxHp = data.Player.baseMaxHp,
                currentHp = data.Player.currentHp,
                moveSpeed = data.Player.moveSpeed,
                rotationSpeed = data.Player.rotationSpeed,

                gold = data.Inventory.gold,
                items = data.Inventory.items,

                weaponSlots = slots,
                activeWeaponIndex = data.Equipment.activeWeaponIndex,

                quests = data.quests,
                activeQuestId = data.activeQuest != null ? data.activeQuest.questId : "",

                currentScene = data.currentScene
            };
        }

        private GameRuntimeData RestoreRuntime(SaveData save)
        {
            GameRuntimeData data = new GameRuntimeData();

            data.Player.level = save.level;
            data.Player.baseAttack = save.baseAttack;
            data.Player.baseDefense = save.baseDefense;
            data.Player.baseMaxHp = save.baseMaxHp;
            data.Player.currentHp = save.currentHp;
            data.Player.moveSpeed = save.moveSpeed;
            data.Player.rotationSpeed = save.rotationSpeed;

            data.Inventory.gold = save.gold;
            if (save.items != null)
            {
                data.Inventory.items = save.items;
            }

            // 装备：优先新格式（每槽位独立），否则降级到旧格式（阶段2-4）。
            if (save.weaponSlots != null && save.weaponSlots.Count > 0)
            {
                for (int i = 0; i < save.weaponSlots.Count && i < data.Equipment.weaponSlots.Count; i++)
                {
                    WeaponSlotSaveData s = save.weaponSlots[i];
                    if (s == null) continue;
                    data.Equipment.weaponSlots[i].config = FindEquipmentConfig(s.weaponName);
                    if (s.gemNames != null)
                    {
                        foreach (string gemName in s.gemNames)
                        {
                            data.Equipment.weaponSlots[i].gems.Add(FindGemConfig(gemName));
                        }
                    }
                }
                data.Equipment.activeWeaponIndex = Mathf.Clamp(save.activeWeaponIndex, 0, data.Equipment.weaponSlots.Count - 1);
            }
            else if (save.equippedWeaponIds != null && save.equippedWeaponIds.Count > 0)
            {
                for (int i = 0; i < save.equippedWeaponIds.Count && i < data.Equipment.weaponSlots.Count; i++)
                {
                    data.Equipment.weaponSlots[i].config = FindEquipmentConfig(save.equippedWeaponIds[i]);
                }
                if (save.equippedGemIds != null)
                {
                    foreach (string gemId in save.equippedGemIds)
                    {
                        data.Equipment.weaponSlots[0].gems.Add(FindGemConfig(gemId));
                    }
                }
                data.Equipment.activeWeaponIndex = 0;
            }
            else if (!string.IsNullOrEmpty(save.equippedWeaponId))
            {
                data.Equipment.weaponSlots[0].config = FindEquipmentConfig(save.equippedWeaponId);
                data.Equipment.activeWeaponIndex = 0;
            }

            if (save.quests != null)
            {
                data.quests = save.quests;
            }

            // activeQuest 反序列化后为独立副本，需按 ID 重新关联到 quests 列表中的实例。
            if (!string.IsNullOrEmpty(save.activeQuestId))
            {
                data.activeQuest = data.quests.Find(q => q != null && q.questId == save.activeQuestId);
            }

            data.currentScene = save.currentScene;

            return data;
        }

        private EquipmentConfig FindEquipmentConfig(string equipmentName)
        {
            if (string.IsNullOrEmpty(equipmentName)) return null;
            return equipmentConfigs.Find(c => c != null && c.equipmentName == equipmentName);
        }

        private GemConfig FindGemConfig(string gemName)
        {
            if (string.IsNullOrEmpty(gemName)) return null;
            return gemConfigs.Find(c => c != null && c.gemName == gemName);
        }
    }
}
