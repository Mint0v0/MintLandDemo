using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using MintLandDemo.Gameplay.Interaction;
using MintLandDemo.UI.Shop;

namespace MintLandDemo.UI.Dialogue
{
    /// <summary>
    /// 对话选项面板（ChoicePanel）管理器。选项面板独立于 DialogPanel。
    /// 三个固定按钮（购买/出售/结束交易）由场景配置，点击直接驱动商店或结束对话。
    /// 图模式的多分支选项通过 ShowDynamicChoices 动态生成按钮。
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

        [Header("动态选项")]
        [SerializeField] private Transform dynamicChoicesRoot;   // 动态按钮挂载的容器（建议配 VerticalLayoutGroup）
        [SerializeField] private GameObject choiceButtonPrefab;  // 按钮预制体（Prefabs/ChoiceButton.prefab）

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

        /// <summary>显示选项面板（固定三按钮）。</summary>
        public void ShowChoices()
        {
            SetFixedButtonsVisible(true);
            SetDynamicChoicesVisible(false);
            if (choicePanel != null) choicePanel.SetActive(true);
        }

        /// <summary>隐藏选项面板。</summary>
        public void HideChoices()
        {
            if (choicePanel != null) choicePanel.SetActive(false);
        }

        /// <summary>
        /// 显示动态选项：为每条边生成一个按钮（文字 = edge.choiceText，空则显示"(无选项)"）。
        /// 点击某按钮 → 回调 onSelected(edge)，并隐藏选项面板。
        /// </summary>
        public void ShowDynamicChoices(List<DialogueEdge> edges, System.Action<DialogueEdge> onSelected)
        {
            if (choicePanel != null) choicePanel.SetActive(true);

            // 隐藏固定三按钮，避免与动态按钮重叠
            SetFixedButtonsVisible(false);

            // 清空上一次的动态按钮
            ClearDynamicButtons();
            SetDynamicChoicesVisible(true);

            if (edges == null || edges.Count == 0) return;

            // 按 sortOrder 升序排序（复制一份，不改变调用方传入的列表）
            var sorted = new List<DialogueEdge>(edges);
            sorted.Sort((a, b) => a.sortOrder.CompareTo(b.sortOrder));

            foreach (DialogueEdge edge in sorted)
            {
                if (edge == null) continue;

                Button btn = CreateChoiceButton(edge.choiceText);
                if (btn == null) continue;

                DialogueEdge captured = edge;
                btn.onClick.AddListener(() =>
                {
                    // 先隐藏当前面板，再回调：回调内可能同步进入下一个多选项节点并重新弹出面板，
                    // 若先回调再 HideChoices 会把新弹出的面板一并隐藏掉。
                    HideChoices();
                    onSelected?.Invoke(captured);
                });
            }
        }

        private void SetFixedButtonsVisible(bool visible)
        {
            if (buyButton != null) buyButton.gameObject.SetActive(visible);
            if (sellButton != null) sellButton.gameObject.SetActive(visible);
            if (endTradeButton != null) endTradeButton.gameObject.SetActive(visible);
        }

        private void SetDynamicChoicesVisible(bool visible)
        {
            if (dynamicChoicesRoot != null) dynamicChoicesRoot.gameObject.SetActive(visible);
        }

        /// <summary>销毁上一次生成的动态按钮。</summary>
        private void ClearDynamicButtons()
        {
            if (dynamicChoicesRoot == null) return;

            for (int i = dynamicChoicesRoot.childCount - 1; i >= 0; i--)
            {
                Transform child = dynamicChoicesRoot.GetChild(i);
                if (child == null) continue;
                child.gameObject.SetActive(false);
                Destroy(child.gameObject);
            }
        }

        private Button CreateChoiceButton(string label)
        {
            string text = string.IsNullOrEmpty(label) ? "(无选项)" : label;

            if (dynamicChoicesRoot == null)
            {
                Debug.LogWarning("[ChoiceManager] dynamicChoicesRoot 未配置，无法创建动态选项按钮。");
                return null;
            }

            if (choiceButtonPrefab != null)
            {
                GameObject go = Instantiate(choiceButtonPrefab, dynamicChoicesRoot);
                Button btn = go.GetComponent<Button>();
                if (btn == null) btn = go.AddComponent<Button>();

                Text textComp = go.GetComponentInChildren<Text>(true);
                if (textComp != null) textComp.text = text;
                return btn;
            }

            // 兜底：没有预制体时用代码生成 Button + Text
            var root = new GameObject("ChoiceButton", typeof(RectTransform), typeof(Image), typeof(Button));
            root.transform.SetParent(dynamicChoicesRoot, false);

            var rootBtn = root.GetComponent<Button>();
            rootBtn.targetGraphic = root.GetComponent<Image>();

            var textGo = new GameObject("Text", typeof(RectTransform));
            textGo.transform.SetParent(root.transform, false);

            var textComp2 = textGo.AddComponent<Text>();
            textComp2.text = text;
            textComp2.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            textComp2.fontSize = 20;
            textComp2.color = Color.black;
            textComp2.alignment = TextAnchor.MiddleCenter;

            var textRect = textGo.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            return rootBtn;
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
