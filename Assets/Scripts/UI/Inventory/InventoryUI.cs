using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using MintLandDemo.Core.Game;
using MintLandDemo.Gameplay.Equipment;

namespace MintLandDemo.UI.Inventory
{
    /// <summary>
    /// 背包显示模式：Full = 全量展示（B 键）；WeaponsOnly = 仅武器；GemsOnly = 仅宝石。
    /// 后两者可点选回传（装备面板调用）。
    /// </summary>
    public enum InventoryMode
    {
        Full,
        WeaponsOnly,
        GemsOnly
    }

    /// <summary>
    /// 背包 UI。B 键全量显示；也支持被装备面板以 WeaponsOnly/GemsOnly 模式打开，点选物品后回传 itemId 并关闭。
    /// </summary>
    public class InventoryUI : MonoBehaviour
    {
        public static InventoryUI Instance { get; private set; }

        [Header("面板")]
        [SerializeField] private GameObject inventoryPanel;

        [Header("物品列表（单 Text 多行，Full 模式用）")]
        [SerializeField] private Text itemListText;

        [Header("过滤选择列表（WeaponsOnly / GemsOnly 模式用）")]
        [SerializeField] private GameObject filteredListContainer;
        [SerializeField] private Button filteredButtonPrefab;

        [Header("系统")]
        [SerializeField] private EquipmentSystem equipmentSystem;

        private InventoryMode _currentMode = InventoryMode.Full;
        private Action<string> _onItemSelected;
        private readonly List<Button> _spawnedButtons = new List<Button>();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
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
            {
                Toggle();
            }
        }

        /// <summary>B 键切换背包（全量模式）。</summary>
        public void Toggle()
        {
            if (inventoryPanel == null) return;

            if (inventoryPanel.activeSelf)
            {
                Close();
            }
            else
            {
                OpenWithFilter(InventoryMode.Full, null);
            }
        }

        /// <summary>以指定模式打开背包（无回传）。</summary>
        public void OpenWithFilter(InventoryMode mode)
        {
            OpenWithFilter(mode, null);
        }

        /// <summary>
        /// 以指定模式打开背包；WeaponsOnly/GemsOnly 模式下点选物品后回传 itemId 并关闭面板。
        /// </summary>
        public void OpenWithFilter(InventoryMode mode, Action<string> onItemSelected)
        {
            _currentMode = mode;
            _onItemSelected = onItemSelected;
            if (inventoryPanel != null) inventoryPanel.SetActive(true);
            RefreshUI();
        }

        /// <summary>关闭背包面板并清理动态按钮。</summary>
        public void Close()
        {
            if (inventoryPanel != null) inventoryPanel.SetActive(false);
            ClearButtons();
        }

        /// <summary>按当前模式刷新：Full 用多行文本，WeaponsOnly/GemsOnly 用可点选按钮。</summary>
        public void RefreshUI()
        {
            if (_currentMode == InventoryMode.Full)
            {
                RefreshFullList();
            }
            else
            {
                RefreshFilteredList();
            }
        }

        private void RefreshFullList()
        {
            if (itemListText != null) itemListText.gameObject.SetActive(true);
            if (filteredListContainer != null) filteredListContainer.SetActive(false);

            if (itemListText == null) return;

            InventoryRuntime inventory = GameRoot.Instance?.Context?.Data?.Inventory;
            if (inventory == null || inventory.items == null || inventory.items.Count == 0)
            {
                itemListText.text = "（背包为空）";
                return;
            }

            StringBuilder sb = new StringBuilder();
            foreach (ItemEntry entry in inventory.items)
            {
                if (entry == null) continue;
                sb.AppendLine($"{entry.itemId} × {entry.count}");
            }
            itemListText.text = sb.ToString();
        }

        private void RefreshFilteredList()
        {
            if (itemListText != null) itemListText.gameObject.SetActive(false);
            if (filteredListContainer != null) filteredListContainer.SetActive(true);

            ClearButtons();

            if (filteredListContainer == null || filteredButtonPrefab == null)
            {
                Debug.LogWarning("[InventoryUI] filteredListContainer 或 filteredButtonPrefab 未配置，无法选择物品。");
                return;
            }

            InventoryRuntime inventory = GameRoot.Instance?.Context?.Data?.Inventory;
            if (inventory == null || inventory.items == null) return;

            foreach (ItemEntry entry in inventory.items)
            {
                if (entry == null || entry.count <= 0) continue;
                if (!PassFilter(entry.itemId)) continue;

                Button btn = Instantiate(filteredButtonPrefab, filteredListContainer.transform);
                btn.onClick.RemoveAllListeners();

                Text label = btn.GetComponentInChildren<Text>();
                if (label != null) label.text = $"{entry.itemId} × {entry.count}";

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

        private void ClearButtons()
        {
            foreach (Button btn in _spawnedButtons)
            {
                if (btn != null) Destroy(btn.gameObject);
            }
            _spawnedButtons.Clear();
        }
    }
}
