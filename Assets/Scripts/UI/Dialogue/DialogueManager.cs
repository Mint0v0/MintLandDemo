using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using MintLandDemo.Core.Event;
using MintLandDemo.Core.Game;
using MintLandDemo.Gameplay.Interaction;
using MintLandDemo.Gameplay.Quest;
using MintLandDemo.Gameplay.Inventory;
using MintLandDemo.UI.Shop;
using MintLandDemo.Controller.Player;

namespace MintLandDemo.UI.Dialogue
{
    public class DialogueManager : MonoBehaviour
    {
        public static DialogueManager Instance { get; private set; }

        [Header("UI 引用")]
        [SerializeField] private GameObject dialoguePanel;
        [SerializeField] private Text speakerText;
        [SerializeField] private Text dialogueText;
        [SerializeField] private Button nextButton;

        private List<DialogueNode> _nodes;
        private int _currentIndex;
        private QuestSystem _questSystem;
        private PlayerController _playerController;

        public bool IsDialogueActive { get; private set; }

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
            _questSystem = FindObjectOfType<QuestSystem>();
            if (_questSystem == null)
            {
                Debug.LogWarning("[DialogueManager] 未找到 QuestSystem，接/交任务动作将无法执行。");
            }

            // 缓存 PlayerController，避免每次对话都 FindObjectOfType
            _playerController = FindObjectOfType<PlayerController>();
            if (_playerController == null)
            {
                Debug.LogWarning("[DialogueManager] 未找到 PlayerController，对话期间将无法锁定玩家输入。");
            }

            if (nextButton != null)
            {
                nextButton.onClick.AddListener(ShowNextNode);
            }

            if (dialoguePanel != null)
            {
                dialoguePanel.SetActive(false);
            }
        }

        public void StartDialogue(List<DialogueNode> nodes)
        {
            if (nodes == null || nodes.Count == 0) return;
            if (IsDialogueActive) return;

            _nodes = nodes;
            _currentIndex = 0;
            IsDialogueActive = true;

            // 对话开始：加锁 + 禁用 Gameplay 输入
            if (_playerController != null)
            {
                _playerController.AddLock(PlayerLockReason.Talking);
            }

            // 对话开始时，如果正在自动寻路，先取消导航
            var nav = MintLandDemo.Gameplay.Navigation.NavigationSystem.Instance;
            if (nav != null && nav.IsNavigating)
                nav.CancelNavigation();

            if (dialoguePanel != null) dialoguePanel.SetActive(true);
            ShowNode(_currentIndex);
        }

        private void ShowNextNode()
        {
            if (!IsDialogueActive) return;

            _currentIndex++;
            if (_currentIndex >= _nodes.Count)
            {
                EndDialogue();
            }
            else
            {
                ShowNode(_currentIndex);
            }
        }

        private void ShowNode(int index)
        {
            DialogueNode node = _nodes[index];
            if (speakerText != null) speakerText.text = node.speakerName;
            if (dialogueText != null) dialogueText.text = node.dialogueText;

            bool hasChoices = node.choices != null && node.choices.Count > 0;

            if (nextButton != null) nextButton.gameObject.SetActive(!hasChoices);

            if (hasChoices)
            {
                if (ChoiceManager.Instance != null)
                {
                    ChoiceManager.Instance.ShowChoices();
                }
                else
                {
                    Debug.LogError("[DialogueManager] ChoiceManager 引用为空！请检查场景中是否有 ChoiceManager 组件。");
                }
            }
            else
            {
                ExecuteAction(node);
            }
        }

        private void ExecuteAction(DialogueNode node)
        {
            Execute(node.action, node.questId, node.itemId, node.itemCount);
        }

        private void Execute(DialogueAction action, string questId, string itemId, int itemCount)
        {
            if (action != DialogueAction.None)
            {
                Debug.Log($"[DialogueManager] 执行动作: {action}, QuestId: {questId}");
            }

            switch (action)
            {
                case DialogueAction.AcceptQuest:
                    if (_questSystem == null)
                    {
                        Debug.LogError("[DialogueManager] QuestSystem 引用为空！请检查场景中是否有 QuestSystem 组件。");
                        return;
                    }
                    _questSystem.AcceptQuest(questId);
                    break;

                case DialogueAction.CompleteQuest:
                    if (_questSystem != null) _questSystem.CompleteQuest(questId);
                    break;

                case DialogueAction.GiveItem:
                    GiveItem(itemId, itemCount);
                    break;

                case DialogueAction.RemoveItem:
                    RemoveItem(itemId, itemCount);
                    break;

                case DialogueAction.OpenShopBuy:
                    OpenShop(ShopMode.Buy);
                    break;

                case DialogueAction.OpenShopSell:
                    OpenShop(ShopMode.Sell);
                    break;

                case DialogueAction.EndTrade:
                    if (ChoiceManager.Instance != null)
                    {
                        ChoiceManager.Instance.OnEndTrade();
                    }
                    break;

                default:
                    break;
            }
        }

        private void OpenShop(ShopMode mode)
        {
            if (ShopUI.Instance == null)
            {
                Debug.LogError("[DialogueManager] ShopUI 引用为空！请检查场景中是否有 ShopUI 组件。");
                return;
            }
            ShopUI.Instance.OpenWithMode(mode);
        }

        private void GiveItem(string itemId, int count)
        {
            if (string.IsNullOrEmpty(itemId)) return;

            GameRuntimeData data = GameRoot.Instance?.Context?.Data;
            if (data == null || data.Inventory == null) return;

            ItemEntry entry = data.Inventory.items.Find(i => i != null && i.itemId == itemId);
            if (entry == null)
            {
                entry = new ItemEntry { itemId = itemId, count = 0 };
                data.Inventory.items.Add(entry);
            }
            entry.count += count;

            EventBus.Publish(new ItemAddedEvent { ItemId = itemId, Count = count });
            Debug.Log($"[DialogueManager] GiveItem -> 发布 ItemAddedEvent: {itemId} x{count}");
        }

        private void RemoveItem(string itemId, int count)
        {
            if (string.IsNullOrEmpty(itemId)) return;

            GameRuntimeData data = GameRoot.Instance?.Context?.Data;
            if (data == null || data.Inventory == null) return;

            ItemEntry entry = data.Inventory.items.Find(i => i != null && i.itemId == itemId);
            if (entry == null)
            {
                Debug.LogWarning($"[DialogueManager] 背包中没有 {itemId}，无法扣除。");
                return;
            }

            entry.count -= count;
            if (entry.count <= 0)
            {
                data.Inventory.items.Remove(entry);
            }

            Debug.Log($"[DialogueManager] RemoveItem -> 扣除 {itemId} x{count}，剩余 {entry.count}");
        }

        public void ShowDialogue()
        {
            if (dialoguePanel != null) dialoguePanel.SetActive(true);
        }

        public void HideDialogue()
        {
            if (dialoguePanel != null) dialoguePanel.SetActive(false);
        }

        public void HideDialogueAndChoices()
        {
            HideDialogue();
            if (ChoiceManager.Instance != null) ChoiceManager.Instance.HideChoices();
        }

        public void EndDialogue()
        {
            IsDialogueActive = false;
            _nodes = null;
            if (nextButton != null) nextButton.gameObject.SetActive(true);
            if (dialoguePanel != null) dialoguePanel.SetActive(false);
            if (ChoiceManager.Instance != null) ChoiceManager.Instance.HideChoices();

            // 兜底：缓存引用可能因跨场景失效，重新查找
            if (_playerController == null)
                _playerController = FindObjectOfType<PlayerController>();

            if (_playerController != null)
            {
                _playerController.RemoveLock(PlayerLockReason.Talking);
            }
        }
    }
}