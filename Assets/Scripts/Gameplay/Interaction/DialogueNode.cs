using System.Collections.Generic;
using UnityEngine;

namespace MintLandDemo.Gameplay.Interaction
{
    /// <summary>
    /// 对话节点触发的动作类型。
    /// </summary>
    public enum DialogueAction
    {
        None,
        AcceptQuest,
        CompleteQuest,
        GiveItem,
        RemoveItem,
        OpenShopBuy,   // 打开商店购买面板
        OpenShopSell,  // 打开商店出售面板
        EndTrade       // 结束交易，关闭对话
    }

    /// <summary>
    /// 对话选项（选择节点用）。点击后触发 action（如 OpenShopBuy / OpenShopSell）。
    /// </summary>
    [System.Serializable]
    public class DialogueChoice
    {
        public string choiceText;              // 选项按钮文字，如 "我要购买商品"
        public DialogueAction action = DialogueAction.None;
        public string questId;                 // action 涉及任务时使用
        public string itemId;                  // action = GiveItem 时使用
        public int itemCount = 1;
        public string nextNodeId;                      // 选择后跳转的节点 ID（空 = 沿用默认顺序）
        public List<DialogueConditionData> conditions; // 该选项可见的条件（空 = 无条件显示）
    }

    /// <summary>
    /// 对话节点（序列化数据类，不继承 MonoBehaviour）。
    /// action 决定该节点展示时触发的行为；choices 非空时是「选择节点」：显示选项按钮，点击触发对应动作。
    /// </summary>
    [System.Serializable]
    public class DialogueNode
    {
        public string speakerName;
        [TextArea] public string dialogueText;
        public DialogueAction action = DialogueAction.None;
        public string questId;     // action 涉及任务时使用（AcceptQuest / CompleteQuest）
        public string itemId;      // action = GiveItem 时使用
        public int itemCount = 1;  // action = GiveItem 时使用
        [HideInInspector] public List<DialogueChoice> choices;    // 已废弃：跳转统一由 DialogueEdge 表达（保留字段以兼容旧资产序列化）

        // ---- 图结构字段（仅由 DialogueGraph 使用；线性播放时忽略）----
        public string nodeId;           // 节点唯一 ID（图中引用）
        public string nextNodeId;       // 顺序推进时的下一节点 ID（空 = 由 choices/edges 决定）
        public Vector2 editorPosition;  // 图编辑器中的节点位置（运行时忽略）
    }
}
