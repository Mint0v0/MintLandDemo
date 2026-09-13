using UnityEngine;
using UnityEngine.UI;
using MintLandDemo.UI.Shop;

namespace MintLandDemo.UI.Dialogue
{
    /// <summary>
    /// 对话选项面板（ChoicePanel）管理器。选项面板独立于 DialogPanel。
    /// 三个固定按钮（购买/出售/结束交易）由场景配置，点击直接驱动商店或结束对话。
    /// </summary>
    public class ChoiceManager : MonoBehaviour
    {
        public static ChoiceManager Instance { get; private set; }

        [Header("面板")]
        [SerializeField] private GameObject choicePanel;

        [Header("按钮")]
        [SerializeField] private Button buyButton;
        [SerializeField] private Button sellButton;
        [SerializeField] private Button endTradeButton;

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
            if (choicePanel != null) choicePanel.SetActive(false);

            if (buyButton != null) buyButton.onClick.AddListener(OnBuySelected);
            if (sellButton != null) sellButton.onClick.AddListener(OnSellSelected);
            if (endTradeButton != null) endTradeButton.onClick.AddListener(OnEndTrade);
        }

        /// <summary>显示选项面板。</summary>
        public void ShowChoices()
        {
            if (choicePanel != null) choicePanel.SetActive(true);
        }

        /// <summary>隐藏选项面板。</summary>
        public void HideChoices()
        {
            if (choicePanel != null) choicePanel.SetActive(false);
        }

        /// <summary>点击「购买」：打开购买模式商店（OpenWithMode 内部会先隐藏对话+选项）。</summary>
        public void OnBuySelected()
        {
            if (ShopUI.Instance == null)
            {
                Debug.LogError("[ChoiceManager] ShopUI 引用为空！请检查场景中是否有 ShopUI 组件。");
                return;
            }
            ShopUI.Instance.OpenWithMode(ShopMode.Buy);
        }

        /// <summary>点击「出售」：打开出售模式商店。</summary>
        public void OnSellSelected()
        {
            if (ShopUI.Instance == null)
            {
                Debug.LogError("[ChoiceManager] ShopUI 引用为空！请检查场景中是否有 ShopUI 组件。");
                return;
            }
            ShopUI.Instance.OpenWithMode(ShopMode.Sell);
        }

        /// <summary>点击「结束交易」：关闭选项面板并结束对话，玩家回到自由移动。</summary>
        public void OnEndTrade()
        {
            HideChoices();
            if (DialogueManager.Instance != null)
            {
                DialogueManager.Instance.EndDialogue();
            }
        }
    }
}
