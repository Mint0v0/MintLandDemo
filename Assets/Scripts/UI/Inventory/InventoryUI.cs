using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using MintLandDemo.Core.Game;
using MintLandDemo.Gameplay.Equipment;

namespace MintLandDemo.UI.Inventory
{
    public enum InventoryMode { Full, WeaponsOnly, GemsOnly }

    [Serializable]
    public class ItemIconEntry
    {
        public string itemId;
        public Sprite icon;
    }

    public class InventoryUI : MonoBehaviour
    {
        public static InventoryUI Instance { get; private set; }

        [Header("面板")]
        [SerializeField] private GameObject inventoryPanel;

        [Header("Full 模式：格子列表")]
        [Tooltip("格子的父物体（建议挂 GridLayoutGroup）")]
        [SerializeField] private Transform slotContainer;
        [Tooltip("格子 Prefab（含 Icon / NameText / CountText 三个子物体）")]
        [SerializeField] private GameObject itemSlotPrefab;

        [Header("物品图标映射（itemId → 图标）")]
        [SerializeField] private ItemIconEntry[] knownItemIcons;

        [Header("空背包提示（可选）")]
        [SerializeField] private GameObject emptyHint;

        [Header("过滤模式：WeaponsOnly / GemsOnly")]
        [SerializeField] private GameObject filteredListContainer;
        [SerializeField] private Button filteredButtonPrefab;

        [Header("系统")]
        [SerializeField] private EquipmentSystem equipmentSystem;

        private InventoryMode _currentMode = InventoryMode.Full;
        private Action<string> _onItemSelected;
        private readonly List<GameObject> _spawnedSlots = new List<GameObject>();
        private readonly List<Button> _spawnedButtons = new List<Button>();

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void Start()
        {
            if (inventoryPanel != null) inventoryPanel.SetActive(false);
            if (equipmentSystem == null) equipmentSystem = FindObjectOfType<EquipmentSystem>();
        }

        private void Update()
        {
            if (Keyboard.current != null && Keyboard.current.bKey.wasPressedThisFrame)
                Toggle();
        }

        public void Toggle()
        {
            if (inventoryPanel == null) return;
            if (inventoryPanel.activeSelf) Close();
            else OpenWithFilter(InventoryMode.Full, null);
        }

        public void OpenWithFilter(InventoryMode mode) => OpenWithFilter(mode, null);

        public void OpenWithFilter(InventoryMode mode, Action<string> onItemSelected)
        {
            _currentMode = mode;
            _onItemSelected = onItemSelected;
            if (inventoryPanel != null) inventoryPanel.SetActive(true);
            RefreshUI();
        }

        public void Close()
        {
            if (inventoryPanel != null) inventoryPanel.SetActive(false);
            ClearSlots();
            ClearButtons();
        }

        public void RefreshUI()
        {
            if (_currentMode == InventoryMode.Full) RefreshFullList();
            else RefreshFilteredList();
        }

        private void RefreshFullList()
        {
            if (slotContainer != null) slotContainer.gameObject.SetActive(true);
            if (filteredListContainer != null) filteredListContainer.SetActive(false);

            ClearSlots();

            InventoryRuntime inventory = GameRoot.Instance?.Context?.Data?.Inventory;
            bool isEmpty = inventory == null || inventory.items == null || inventory.items.Count == 0;
            if (emptyHint != null) emptyHint.SetActive(isEmpty);
            if (isEmpty || slotContainer == null || itemSlotPrefab == null) return;

            foreach (ItemEntry entry in inventory.items)
            {
                if (entry == null || entry.count <= 0) continue;

                GameObject slot = Instantiate(itemSlotPrefab, slotContainer);
                _spawnedSlots.Add(slot);

                Transform iconT = slot.transform.Find("Icon");
                Transform nameT = slot.transform.Find("NameText");
                Transform countT = slot.transform.Find("CountText");

                Image iconImg = iconT != null ? iconT.GetComponent<Image>() : null;
                Text nameText = nameT != null ? nameT.GetComponent<Text>() : null;
                Text countText = countT != null ? countT.GetComponent<Text>() : null;

                Sprite icon = GetIconForItem(entry.itemId);
                if (iconImg != null)
                {
                    iconImg.sprite = icon;
                    iconImg.enabled = icon != null;
                }

                if (nameText != null) nameText.text = GetDisplayName(entry.itemId);
                if (countText != null) countText.text = "×" + entry.count;
            }
        }

        private Sprite GetIconForItem(string itemId)
        {
            if (string.IsNullOrEmpty(itemId) || knownItemIcons == null) return null;
            foreach (var entry in knownItemIcons)
            {
                if (entry == null) continue;
                if (string.Equals(entry.itemId, itemId, StringComparison.OrdinalIgnoreCase))
                    return entry.icon;
            }
            return null;
        }

        private string GetDisplayName(string itemId)
        {
            if (string.IsNullOrEmpty(itemId)) return "未知";
            switch (itemId.ToLowerInvariant())
            {
                case "sword": return "宝剑";
                case "axe": return "斧头";
                case "potion": return "治疗药水";
                case "slime_mucus": return "史莱姆粘液";
                case "ruby": return "红宝石";
                case "sapphire": return "蓝宝石";
                case "emerald": return "绿宝石";
                default: return itemId;
            }
        }

        private void RefreshFilteredList()
        {
            if (slotContainer != null) slotContainer.gameObject.SetActive(false);
            if (filteredListContainer != null) filteredListContainer.SetActive(true);
            if (emptyHint != null) emptyHint.SetActive(false);

            ClearSlots();
            ClearButtons();

            if (filteredListContainer == null || filteredButtonPrefab == null) return;

            InventoryRuntime inventory = GameRoot.Instance?.Context?.Data?.Inventory;
            if (inventory == null || inventory.items == null) return;

            foreach (ItemEntry entry in inventory.items)
            {
                if (entry == null || entry.count <= 0) continue;
                if (!PassFilter(entry.itemId)) continue;

                Button btn = Instantiate(filteredButtonPrefab, filteredListContainer.transform);
                btn.onClick.RemoveAllListeners();

                Text label = btn.GetComponentInChildren<Text>();
                if (label != null) label.text = $"{GetDisplayName(entry.itemId)} × {entry.count}";

                string capturedId = entry.itemId;
                btn.onClick.AddListener(() => OnItemClicked(capturedId));
                _spawnedButtons.Add(btn);
            }
        }

        private bool PassFilter(string itemId)
        {
            if (equipmentSystem == null) return false;
            if (_currentMode == InventoryMode.WeaponsOnly) return equipmentSystem.IsWeaponItem(itemId);
            if (_currentMode == InventoryMode.GemsOnly) return equipmentSystem.IsGemItem(itemId);
            return false;
        }

        private void OnItemClicked(string itemId)
        {
            _onItemSelected?.Invoke(itemId);
            Close();
        }

        private void ClearSlots()
        {
            foreach (var slot in _spawnedSlots)
                if (slot != null) Destroy(slot);
            _spawnedSlots.Clear();
        }

        private void ClearButtons()
        {
            foreach (Button btn in _spawnedButtons)
                if (btn != null) Destroy(btn.gameObject);
            _spawnedButtons.Clear();
        }
    }
}