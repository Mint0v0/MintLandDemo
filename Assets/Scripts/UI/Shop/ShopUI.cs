using UnityEngine;
using UnityEngine.UI;
using MintLandDemo.Core.Event;
using MintLandDemo.Core.Game;
using MintLandDemo.Gameplay.Equipment;
using MintLandDemo.Gameplay.Inventory;
using MintLandDemo.Gameplay.Shop;
using MintLandDemo.UI.Dialogue;

namespace MintLandDemo.UI.Shop
{
    /// <summary>
    /// 商店模式：购买宝石 / 出售粘液。
    /// </summary>
    public enum ShopMode
    {
        Buy,
        Sell
    }

    /// <summary>
    /// 商店 UI（极简）。由 NPC 对话触发（OpenWithMode），不再用 B 键直接打开。
    /// 打开时先隐藏对话与选项；关闭（右上角 X）时恢复对话与选项。
    /// </summary>
    public class ShopUI : MonoBehaviour
    {
        public static ShopUI Instance { get; private set; }

        [Header("面板")]
        [SerializeField] private GameObject shopPanel;

        [Header("模式分组")]
        [SerializeField] private GameObject buyGroup;   // 购买宝石按钮组
        [SerializeField] private GameObject sellGroup;  // 出售粘液按钮组

        [Header("金币显示")]
        [SerializeField] private Text goldText;

        [Header("购买按钮")]
        [SerializeField] private Button buyRubyButton;
        [SerializeField] private Button buySapphireButton;
        [SerializeField] private Button buyEmeraldButton;

        [Header("出售按钮")]
        [SerializeField] private Button sellMucusButton;

        [Header("关闭按钮")]
        [SerializeField] private Button closeButton;

        [Header("宝石配置")]
        [SerializeField] private GemConfig rubyGem;
        [SerializeField] private GemConfig sapphireGem;
        [SerializeField] private GemConfig emeraldGem;

        [Header("系统")]
        [SerializeField] private ShopSystem shopSystem;

        private ShopMode _currentMode = ShopMode.Buy;

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
            if (shopPanel != null) shopPanel.SetActive(false);

            if (shopSystem == null)
                shopSystem = FindObjectOfType<ShopSystem>();

            if (buyRubyButton != null) buyRubyButton.onClick.AddListener(() => BuyGem(rubyGem));
            if (buySapphireButton != null) buySapphireButton.onClick.AddListener(() => BuyGem(sapphireGem));
            if (buyEmeraldButton != null) buyEmeraldButton.onClick.AddListener(() => BuyGem(emeraldGem));
            if (sellMucusButton != null) sellMucusButton.onClick.AddListener(SellMucus);
            if (closeButton != null) closeButton.onClick.AddListener(CloseShop);

            RefreshGold();
        }

        private void OnEnable()
        {
            EventBus.Subscribe<GoldChangedEvent>(OnGoldChanged);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<GoldChangedEvent>(OnGoldChanged);
        }

        /// <summary>
        /// 由对话触发：先隐藏对话与选项面板，再按模式打开商店。
        /// </summary>
        public void OpenWithMode(ShopMode mode)
        {
            _currentMode = mode;

            if (DialogueManager.Instance != null)
            {
                DialogueManager.Instance.HideDialogueAndChoices();
            }

            ApplyMode();
            if (shopPanel != null) shopPanel.SetActive(true);
            RefreshGold();
        }

        /// <summary>
        /// 关闭商店（右上角 X）：隐藏商店面板，恢复显示对话与选项（回到交易选项状态）。
        /// </summary>
        public void CloseShop()
        {
            if (shopPanel != null) shopPanel.SetActive(false);

            if (DialogueManager.Instance != null)
            {
                DialogueManager.Instance.ShowDialogue();
            }
            if (ChoiceManager.Instance != null)
            {
                ChoiceManager.Instance.ShowChoices();
            }
        }

        private void ApplyMode()
        {
            if (buyGroup != null) buyGroup.SetActive(_currentMode == ShopMode.Buy);
            if (sellGroup != null) sellGroup.SetActive(_currentMode == ShopMode.Sell);
        }

        private void BuyGem(GemConfig gem)
        {
            if (shopSystem != null) shopSystem.BuyGem(gem);
        }

        private void SellMucus()
        {
            if (shopSystem != null) shopSystem.SellItem("slime_mucus", 1);
        }

        private void OnGoldChanged(GoldChangedEvent e)
        {
            RefreshGold();
        }

        private void RefreshGold()
        {
            if (goldText == null) return;
            int gold = GameRoot.Instance?.Context?.Data?.Inventory?.gold ?? 0;
            goldText.text = $"金币: {gold}";
        }
    }
}
