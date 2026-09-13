using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using MintLandDemo.Core.Event;
using MintLandDemo.Core.Game;
using MintLandDemo.Gameplay.Equipment;
using MintLandDemo.UI.Inventory;

namespace MintLandDemo.UI.Equipment
{
    /// <summary>
    /// 装备面板（武器面板）。I 键开关；左侧 2 个武器槽，右侧详情（属性/宝石/卸下-装备按钮）。
    /// 阶段 5：高亮与详情跟随 activeWeaponIndex；宝石槽带 "+"（镶嵌）与 "X"（卸下）按钮。
    /// </summary>
    public class EquipmentUI : MonoBehaviour
    {
        [Header("面板")]
        [SerializeField] private GameObject equipmentPanel;

        [Header("武器列表")]
        [SerializeField] private GameObject weaponSlot1;      // 武器1 容器（需挂 Button）
        [SerializeField] private GameObject weaponSlot2;      // 武器2 容器（需挂 Button）
        [SerializeField] private Text weaponNameText1;        // 武器1 名称
        [SerializeField] private Text weaponNameText2;        // 武器2 名称
        [SerializeField] private GameObject weaponHighlight1; // 武器1 高亮（激活时显示）
        [SerializeField] private GameObject weaponHighlight2; // 武器2 高亮（激活时显示）

        [Header("详情面板")]
        [SerializeField] private GameObject detailPanel;
        [SerializeField] private Text weaponNameDetail;
        [SerializeField] private Text attackBonusText;   // "攻击 +10"
        [SerializeField] private Text defenseBonusText;  // "防御 +5"
        [SerializeField] private Text hpBonusText;       // "血量 +20"
        [SerializeField] private Text gemSlot1Text;      // 宝石槽1 名称/"空槽位"
        [SerializeField] private Text gemSlot2Text;      // 宝石槽2 名称/"空槽位"

        [Header("宝石镶嵌")]
        [SerializeField] private Button gemEquipButton1;   // 槽位1 "+" 按钮（空槽时显示）
        [SerializeField] private Button gemEquipButton2;   // 槽位2 "+" 按钮（空槽时显示）

        [Header("宝石卸下")]
        [SerializeField] private Button gemUnequipButton1; // 槽位1 "X" 按钮（有宝石时显示）
        [SerializeField] private Button gemUnequipButton2; // 槽位2 "X" 按钮（有宝石时显示）

        [Header("操作按钮")]
        [SerializeField] private Button actionButton;    // "卸下"/"装备"
        [SerializeField] private Text actionButtonText;  // 按钮文字

        [Header("系统")]
        [SerializeField] private EquipmentSystem equipmentSystem;
        [SerializeField] private InventoryUI inventoryUI;

        private int _pendingGemSlotIndex = 0; // 记录点击 "+" 的宝石槽位，用于镶嵌回传

        private EquipmentRuntime Equip => GameRoot.Instance?.Context?.Data?.Equipment;
        private int ActiveSlotIndex => Equip != null ? Equip.activeWeaponIndex : 0;

        private void Start()
        {
            if (equipmentPanel != null) equipmentPanel.SetActive(false);

            if (equipmentSystem == null) equipmentSystem = FindObjectOfType<EquipmentSystem>();
            if (inventoryUI == null) inventoryUI = FindObjectOfType<InventoryUI>();

            BindSlotClick(weaponSlot1, 0);
            BindSlotClick(weaponSlot2, 1);

            if (actionButton != null) actionButton.onClick.AddListener(OnActionButtonClicked);

            if (gemEquipButton1 != null) gemEquipButton1.onClick.AddListener(() => OnGemEquipClicked(0));
            if (gemEquipButton2 != null) gemEquipButton2.onClick.AddListener(() => OnGemEquipClicked(1));
            if (gemUnequipButton1 != null) gemUnequipButton1.onClick.AddListener(() => OnGemUnequipClicked(0));
            if (gemUnequipButton2 != null) gemUnequipButton2.onClick.AddListener(() => OnGemUnequipClicked(1));
        }

        private void OnEnable()
        {
            EventBus.Subscribe<EquipmentChangedEvent>(OnEquipmentChanged);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<EquipmentChangedEvent>(OnEquipmentChanged);
        }

        private void Update()
        {
            if (Keyboard.current != null && Keyboard.current.iKey.wasPressedThisFrame)
            {
                TogglePanel();
            }
        }

        private void BindSlotClick(GameObject slot, int index)
        {
            if (slot == null) return;
            Button btn = slot.GetComponent<Button>();
            if (btn != null)
            {
                btn.onClick.AddListener(() => SelectWeaponSlot(index));
            }
            else
            {
                Debug.LogWarning($"[EquipmentUI] 武器槽位 {index} 缺少 Button 组件，无法点击。");
            }
        }

        private void TogglePanel()
        {
            if (equipmentPanel == null) return;
            if (equipmentPanel.activeSelf)
            {
                equipmentPanel.SetActive(false);
            }
            else
            {
                equipmentPanel.SetActive(true);
                RefreshUI();
            }
        }

        public void OpenPanel()
        {
            if (equipmentPanel != null) equipmentPanel.SetActive(true);
            RefreshUI();
        }

        /// <summary>点击武器槽位：切换激活武器（空槽 = 空手，用于后续「装备」）。</summary>
        public void SelectWeaponSlot(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= 2) return;
            if (equipmentSystem != null) equipmentSystem.SwitchActiveWeapon(slotIndex);
            // SwitchActiveWeapon 发布 EquipmentChangedEvent → OnEquipmentChanged → RefreshUI
        }

        public void RefreshUI()
        {
            EquipmentRuntime equip = Equip;
            int active = ActiveSlotIndex;

            UpdateSlotDisplay(equip, 0, weaponNameText1, weaponHighlight1, active);
            UpdateSlotDisplay(equip, 1, weaponNameText2, weaponHighlight2, active);

            WeaponSlotData activeSlot = equip != null ? equip.ActiveWeaponSlot : null;
            EquipmentConfig weapon = activeSlot != null ? activeSlot.config : null;

            if (detailPanel != null) detailPanel.SetActive(true);
            if (weaponNameDetail != null) weaponNameDetail.text = weapon != null ? weapon.equipmentName : "无武器";

            // 计算属性：武器加成 + 该槽位所有宝石加成
            int totalAttack = weapon != null ? weapon.attackBonus : 0;
            int totalDefense = weapon != null ? weapon.defenseBonus : 0;
            int totalHp = weapon != null ? weapon.hpBonus : 0;

            if (activeSlot != null && activeSlot.gems != null)
            {
                foreach (GemConfig gem in activeSlot.gems)
                {
                    if (gem != null)
                    {
                        totalAttack += gem.attackBonus;
                        totalDefense += gem.defenseBonus;
                        totalHp += gem.hpBonus;
                    }
                }
            }

            if (attackBonusText != null) attackBonusText.text = $"攻击 +{totalAttack}";
            if (defenseBonusText != null) defenseBonusText.text = $"防御 +{totalDefense}";
            if (hpBonusText != null) hpBonusText.text = $"血量 +{totalHp}";

            UpdateGemDisplay(activeSlot);
            UpdateActionButton(weapon != null);
        }

        private void UpdateSlotDisplay(EquipmentRuntime equip, int slotIndex, Text nameText, GameObject highlight, int activeIndex)
        {
            WeaponSlotData slot = GetSlotAt(equip, slotIndex);
            if (nameText != null) nameText.text = (slot != null && slot.config != null) ? slot.config.equipmentName : "空槽位";
            if (highlight != null) highlight.SetActive(slotIndex == activeIndex);
        }

        private WeaponSlotData GetSlotAt(EquipmentRuntime equip, int slotIndex)
        {
            if (equip == null || equip.weaponSlots == null || slotIndex >= equip.weaponSlots.Count) return null;
            return equip.weaponSlots[slotIndex];
        }

        private void UpdateGemDisplay(WeaponSlotData slot)
        {
            UpdateGemSlot(slot, 0, gemSlot1Text, gemEquipButton1, gemUnequipButton1);
            UpdateGemSlot(slot, 1, gemSlot2Text, gemEquipButton2, gemUnequipButton2);
        }

        private void UpdateGemSlot(WeaponSlotData slot, int gemSlotIndex, Text slotText, Button equipButton, Button unequipButton)
        {
            bool usable = slot != null && slot.config != null && gemSlotIndex < slot.gemSlotCount;
            GemConfig gem = usable && slot.gems != null && slot.gems.Count > gemSlotIndex ? slot.gems[gemSlotIndex] : null;

            if (slotText != null) slotText.text = gem != null ? gem.gemName : "空槽位";
            if (equipButton != null) equipButton.gameObject.SetActive(usable && gem == null);
            if (unequipButton != null) unequipButton.gameObject.SetActive(gem != null);
        }

        /// <summary>点击宝石 "+" 按钮：打开背包（仅宝石），记录待镶嵌的宝石槽位。</summary>
        private void OnGemEquipClicked(int gemSlotIndex)
        {
            if (inventoryUI == null)
            {
                Debug.LogWarning("[EquipmentUI] InventoryUI 未配置，无法镶嵌宝石。");
                return;
            }
            _pendingGemSlotIndex = gemSlotIndex;
            inventoryUI.OpenWithFilter(InventoryMode.GemsOnly, OnGemSelectedFromInventory);
        }

        /// <summary>从背包选择宝石后的回传：镶嵌到当前激活武器的对应宝石槽位。</summary>
        public void OnGemSelectedFromInventory(string gemId)
        {
            if (equipmentSystem != null)
                equipmentSystem.EquipGem(ActiveSlotIndex, _pendingGemSlotIndex, gemId);
            // EquipGem 发布 EquipmentChangedEvent → OnEquipmentChanged → RefreshUI
        }

        /// <summary>点击宝石 "X" 按钮：卸下当前激活武器的对应宝石槽位。</summary>
        private void OnGemUnequipClicked(int gemSlotIndex)
        {
            if (equipmentSystem != null)
                equipmentSystem.UnequipGem(ActiveSlotIndex, gemSlotIndex);
        }

        private void UpdateActionButton(bool hasWeapon)
        {
            if (actionButtonText != null)
            {
                actionButtonText.text = hasWeapon ? "卸下" : "装备";
            }
        }

        private void OnActionButtonClicked()
        {
            EquipmentRuntime equip = Equip;
            if (equip == null) return;

            int slotIndex = ActiveSlotIndex;
            WeaponSlotData slot = GetSlotAt(equip, slotIndex);
            bool hasWeapon = slot != null && slot.config != null;

            if (hasWeapon)
            {
                if (equipmentSystem != null) equipmentSystem.UnequipWeapon(slotIndex);
            }
            else
            {
                if (inventoryUI == null)
                {
                    Debug.LogWarning("[EquipmentUI] InventoryUI 未配置，无法选择武器。");
                    return;
                }
                inventoryUI.OpenWithFilter(InventoryMode.WeaponsOnly, OnWeaponSelectedFromInventory);
            }
        }

        /// <summary>从背包选择武器后的回传：装备到当前激活槽位。</summary>
        public void OnWeaponSelectedFromInventory(string weaponId)
        {
            if (equipmentSystem != null)
                equipmentSystem.EquipWeapon(ActiveSlotIndex, weaponId);
            // EquipWeapon 发布 EquipmentChangedEvent → OnEquipmentChanged → RefreshUI
        }

        private void OnEquipmentChanged(EquipmentChangedEvent e)
        {
            RefreshUI();
        }
    }
}
