using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using MintLandDemo.Gameplay.Interaction;
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
        private PlayerController _playerController;

        // 图模式状态（_graph 非空 = 图模式）
        private DialogueGraph _graph;
        private DialogueNode _currentGraphNode;
        private DialogueEdge _pendingEdge;

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

            if (_playerController != null)
            {
                _playerController.AddLock(PlayerLockReason.Talking);
            }

            var nav = MintLandDemo.Gameplay.Navigation.NavigationSystem.Instance;
            if (nav != null && nav.IsNavigating)
                nav.CancelNavigation();

            if (dialoguePanel != null) dialoguePanel.SetActive(true);
            ShowNode(_currentIndex);
        }

        /// <summary>
        /// 图模式入口：按节点 ID 跳转，而不是按 index 线性播放。
        /// 旧 StartDialogue(List&lt;DialogueNode&gt;) 保持不变，走线性路径。
        /// </summary>
        public void StartDialogue(DialogueGraph graph)
        {
            if (graph == null || string.IsNullOrEmpty(graph.entryNodeId)) return;
            if (IsDialogueActive) return;

            _graph = graph;
            _currentGraphNode = null;
            IsDialogueActive = true;

            Debug.Log($"[DialogueManager] 图模式启动对话: graphId={graph.graphId}, entryNodeId={graph.entryNodeId}");

            if (_playerController != null)
            {
                _playerController.AddLock(PlayerLockReason.Talking);
            }

            // 与旧 StartDialogue 一致：取消导航
            var nav = MintLandDemo.Gameplay.Navigation.NavigationSystem.Instance;
            if (nav != null && nav.IsNavigating)
                nav.CancelNavigation();

            if (dialoguePanel != null) dialoguePanel.SetActive(true);

            GoToGraphNode(graph.entryNodeId);
        }

        /// <summary>
        /// 图模式：跳转到指定节点并播放。节点自身 action 立即执行，出边决定下一步。
        /// </summary>
        private void GoToGraphNode(string nodeId)
        {
            if (_graph == null) { EndDialogue(); return; }

            DialogueNode node = _graph.nodes.Find(n => n != null && n.nodeId == nodeId);
            if (node == null) { EndDialogue(); return; }

            _currentGraphNode = node;
            Debug.Log($"[DialogueManager] 图模式节点: {node.nodeId} (speaker={node.speakerName})");

            // 1. 执行节点自身的 action
            DialogueInstructionExecutor.Execute(node.action, node.questId, node.itemId, node.itemCount);

            // 2. 计算文本（复用现有 BuildDisplayText 逻辑）
            string displayText = BuildDisplayText(node);

            // 3. 找出边，并过滤：条件通过 + 目标节点非空
            List<DialogueEdge> outEdges = _graph.edges.FindAll(e => e != null && e.fromNodeId == nodeId);
            outEdges = outEdges.FindAll(e =>
                !string.IsNullOrEmpty(e.toNodeId) &&
                DialogueConditionEvaluator.EvaluateAll(e.conditions));

            // 4. 分支处理
            if (outEdges.Count == 0)
            {
                // 无边 → 显示当前文本（如有），点下一步结束对话
                if (speakerText != null) speakerText.text = node.speakerName;
                if (dialogueText != null) dialogueText.text = displayText;
                if (nextButton != null) nextButton.gameObject.SetActive(true);
                return;
            }

            if (outEdges.Count == 1 && string.IsNullOrEmpty(outEdges[0].choiceText))
            {
                // 单条无边文字 → 显示文本 + 下一步，点击后跳到下个节点
                if (speakerText != null) speakerText.text = node.speakerName;
                if (dialogueText != null) dialogueText.text = displayText;
                if (nextButton != null) nextButton.gameObject.SetActive(true);
                _pendingEdge = outEdges[0];
                return;
            }

            // 多条 或 有 choiceText → 显示动态选项按钮
            if (speakerText != null) speakerText.text = node.speakerName;
            if (dialogueText != null) dialogueText.text = displayText;
            if (nextButton != null) nextButton.gameObject.SetActive(false);

            if (ChoiceManager.Instance != null)
            {
                ChoiceManager.Instance.ShowDynamicChoices(outEdges, OnEdgeSelected);
            }
        }

        /// <summary>图模式：玩家点击某个动态选项后，执行该边 action 并跳转。</summary>
        private void OnEdgeSelected(DialogueEdge edge)
        {
            if (edge == null) { EndDialogue(); return; }

            // 执行边自身的 action
            DialogueInstructionExecutor.Execute(edge.action, edge.questId, edge.itemId, edge.itemCount);

            // 跳转
            if (string.IsNullOrEmpty(edge.toNodeId))
            {
                // 边没连线 → 结束对话
                EndDialogue();
                return;
            }
            GoToGraphNode(edge.toNodeId);
        }

        private void ShowNextNode()
        {
            if (!IsDialogueActive) return;

            // 图模式：沿 pending 边跳转；无边则结束
            if (_graph != null)
            {
                if (_pendingEdge != null)
                {
                    string next = _pendingEdge.toNodeId;
                    _pendingEdge = null;
                    GoToGraphNode(next);
                }
                else
                {
                    EndDialogue();
                }
                return;
            }

            // 旧线性模式
            ShowNode(_currentIndex + 1);
        }

        /// <summary>
        /// 显示第 index 个节点。
        /// - 有台词 → 显示台词，等玩家点下一步
        /// - 无台词但有动作（获得/扣除物品）→ 自动生成提示文本"获得: 宝剑 × 1"
        /// - 无台词、无选项、无可显示的动作 → 直接执行动作并跳到下一节点
        /// </summary>
        private void ShowNode(int index)
        {
            // 跳过所有"无内容可显示"的节点
            while (index < _nodes.Count)
            {
                DialogueNode node = _nodes[index];
                _currentIndex = index;

                string displayText = BuildDisplayText(node);
                bool hasText = !string.IsNullOrEmpty(displayText);
                bool hasChoices = node.choices != null && node.choices.Count > 0;

                // 无文本、无选项 → 直接执行动作，继续下一个节点
                if (!hasText && !hasChoices)
                {
                    ExecuteAction(node);
                    index++;
                    continue;
                }

                // 有内容 → 显示
                if (speakerText != null) speakerText.text = node.speakerName;
                if (dialogueText != null) dialogueText.text = displayText;
                if (nextButton != null) nextButton.gameObject.SetActive(!hasChoices);

                if (hasChoices)
                {
                    if (ChoiceManager.Instance != null)
                        ChoiceManager.Instance.ShowChoices();
                    else
                        Debug.LogError("[DialogueManager] ChoiceManager 引用为空！请检查场景中是否有 ChoiceManager 组件。");
                }
                else
                {
                    ExecuteAction(node);
                }
                return;
            }

            // 所有节点都跳过了 → 结束对话
            EndDialogue();
        }

        /// <summary>
        /// 决定当前节点显示什么文本：
        /// 1. 有 dialogueText → 直接用
        /// 2. 没 dialogueText 但是 GiveItem → "获得: 宝剑 × 1"
        /// 3. 没 dialogueText 但是 RemoveItem → "交付: 治疗药水 × 1"
        /// 4. 其他 → null（会触发跳过）
        /// </summary>
        private string BuildDisplayText(DialogueNode node)
        {
            if (!string.IsNullOrEmpty(node.dialogueText))
                return node.dialogueText;

            string itemName = GetDisplayName(node.itemId);

            switch (node.action)
            {
                case DialogueAction.GiveItem:
                    if (string.IsNullOrEmpty(node.itemId)) return null;
                    return $"获得: {itemName} × {node.itemCount}";

                case DialogueAction.RemoveItem:
                    if (string.IsNullOrEmpty(node.itemId)) return null;
                    return $"交付: {itemName} × {node.itemCount}";

                default:
                    return null;
            }
        }

        private string GetDisplayName(string itemId)
        {
            if (string.IsNullOrEmpty(itemId)) return "物品";
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

        private void ExecuteAction(DialogueNode node)
        {
            Execute(node.action, node.questId, node.itemId, node.itemCount);
        }

        private void Execute(DialogueAction action, string questId, string itemId, int itemCount)
        {
            DialogueInstructionExecutor.Execute(action, questId, itemId, itemCount);
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
            _graph = null;
            _currentGraphNode = null;
            _pendingEdge = null;
            if (nextButton != null) nextButton.gameObject.SetActive(true);
            if (dialoguePanel != null) dialoguePanel.SetActive(false);
            if (ChoiceManager.Instance != null) ChoiceManager.Instance.HideChoices();

            if (_playerController == null)
                _playerController = FindObjectOfType<PlayerController>();

            if (_playerController != null)
            {
                _playerController.RemoveLock(PlayerLockReason.Talking);
            }
        }
    }
}