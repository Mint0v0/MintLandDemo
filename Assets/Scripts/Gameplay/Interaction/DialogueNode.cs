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
        public List<DialogueChoice> choices;    // 非空 = 选择节点
    }
}
