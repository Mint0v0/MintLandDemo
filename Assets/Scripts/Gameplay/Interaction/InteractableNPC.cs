using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using MintLandDemo.Core.Game;
using MintLandDemo.Controller.Player;
using MintLandDemo.UI.Dialogue;

namespace MintLandDemo.Gameplay.Interaction
{
    /// <summary>
    /// 按任务阶段分流的对话分支条件。
    /// </summary>
    public enum QuestCondition
    {
        NotAccepted,   // 任务尚未接取
        Accepted,      // 任务已接取、未完成
        Completed      // 任务已完成
    }

    /// <summary>
    /// 条件对话分支：当条件满足时，用该分支的对话链替换默认对话。
    /// </summary>
    [System.Serializable]
    public class ConditionalDialogue
    {
        public QuestCondition condition;
        public string questId;              // 依据哪个任务判断
        public List<DialogueNode> nodes;    // 该分支的对话链
    }

    /// <summary>
    /// NPC 交互组件：检测玩家进入范围 + 按 E 触发对话。
    /// 不持有 QuestSystem 引用，任务状态变化只经由 DialogueManager 的动作触发 → QuestSystem → EventBus。
    /// </summary>
    public class InteractableNPC : MonoBehaviour
    {
        [Header("NPC Info")]
        public string npcName;
        public float interactionRange = 3f;

        [Header("NPC 注册")]
        public string npcId;   // NPC 唯一 ID（供自动寻路定位，需在场景内唯一）

        /// <summary>NPC 注册表：npcId → NPC。场景加载后各 NPC 在 Awake 注册、OnDestroy 注销。</summary>
        public static Dictionary<string, InteractableNPC> NPCRegistry = new Dictionary<string, InteractableNPC>();

        [Header("默认对话（无条件兜底）")]
        public List<DialogueNode> dialogueNodes;

        [Header("条件对话（按任务状态分流，自上而下优先匹配）")]
        public List<ConditionalDialogue> conditionalDialogues;

        [Header("图模式对话（可选，非空时优先使用）")]
        public DialogueGraph dialogueGraph;

        [Header("交互提示 UI")]
        [SerializeField] private GameObject interactionPrompt;

        private Transform _player;
        private bool _promptShown;

        private void Awake()
        {
            var controller = FindObjectOfType<PlayerController>();
            if (controller != null)
            {
                _player = controller.transform;
            }

            if (!string.IsNullOrEmpty(npcId))
            {
                NPCRegistry[npcId] = this;
            }
        }

        private void OnDestroy()
        {
            if (!string.IsNullOrEmpty(npcId) && NPCRegistry.TryGetValue(npcId, out InteractableNPC npc) && npc == this)
            {
                NPCRegistry.Remove(npcId);
            }
        }

        private void Update()
        {
            if (_player == null) return;

            bool inRange = Vector3.Distance(transform.position, _player.position) <= interactionRange;
            bool dialogueActive = DialogueManager.Instance != null && DialogueManager.Instance.IsDialogueActive;

            bool shouldShowPrompt = inRange && !dialogueActive;
            if (shouldShowPrompt != _promptShown)
            {
                _promptShown = shouldShowPrompt;
                SetPrompt(shouldShowPrompt);
            }

            if (inRange && !dialogueActive && Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
            {
                StartDialogue();
            }
        }

        private void StartDialogue()
        {
            // 图模式优先
            if (dialogueGraph != null && !string.IsNullOrEmpty(dialogueGraph.entryNodeId))
            {
                if (DialogueManager.Instance != null)
                    DialogueManager.Instance.StartDialogue(dialogueGraph);
                return;
            }

            // 旧模式兜底
            List<DialogueNode> nodes = ResolveDialogue();
            if (nodes == null || nodes.Count == 0) return;
            if (DialogueManager.Instance != null)
                DialogueManager.Instance.StartDialogue(nodes);
        }

        private List<DialogueNode> ResolveDialogue()
        {
            GameRuntimeData data = GameRoot.Instance?.Context?.Data;
            if (data == null || conditionalDialogues == null)
            {
                return dialogueNodes;
            }

            foreach (ConditionalDialogue cd in conditionalDialogues)
            {
                if (cd == null || string.IsNullOrEmpty(cd.questId)) continue;

                QuestRuntime quest = data.quests.Find(q => q.questId == cd.questId);
                bool match;
                switch (cd.condition)
                {
                    case QuestCondition.NotAccepted:
                        match = (quest == null);
                        break;
                    case QuestCondition.Accepted:
                        match = (quest != null && quest.isAccepted && !quest.isCompleted);
                        break;
                    case QuestCondition.Completed:
                        match = (quest != null && quest.isCompleted);
                        break;
                    default:
                        match = false;
                        break;
                }

                if (match) return cd.nodes;
            }

            return dialogueNodes;
        }

        private void SetPrompt(bool show)
        {
            if (interactionPrompt != null)
            {
                interactionPrompt.SetActive(show);
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, interactionRange);
        }
    }
}
