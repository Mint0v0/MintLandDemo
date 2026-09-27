using System.Collections.Generic;
using UnityEngine;

namespace MintLandDemo.Gameplay.Interaction
{
    /// <summary>
    /// 对话条件类型：判断一条边是否可选 / 可达。
    /// </summary>
    public enum DialogueConditionType
    {
        None,       // 无条件
        QuestState, // 依据任务状态判断
        HasItem,    // 拥有指定物品
        HasGold     // 拥有足够金币
    }

    /// <summary>
    /// 对话边条件（序列化数据类，不继承 MonoBehaviour）。
    /// </summary>
    [System.Serializable]
    public class DialogueConditionData
    {
        public DialogueConditionType type = DialogueConditionType.None;
        public string questId;                                          // type = QuestState 时使用
        public QuestCondition questState = QuestCondition.NotAccepted;  // type = QuestState 时使用
        public string itemId;                                           // type = HasItem 时使用
        public int itemCount = 1;                                       // type = HasItem 时使用
    }

    /// <summary>
    /// 对话边：连接两个节点的有向关系（序列化数据类，不继承 MonoBehaviour）。
    /// choiceText 非空时该边作为「选项」展示；sortOrder 决定选项排序；conditions 为空 = 无条件。
    /// </summary>
    [System.Serializable]
    public class DialogueEdge
    {
        public string edgeId;
        public string fromNodeId;
        public string toNodeId;
        public string choiceText;                      // 选项文字，空 = 顺序推进
        public DialogueAction action = DialogueAction.None; // 该分支触发的动作
        public string questId;                         // action 涉及任务时使用
        public string itemId;                          // action = GiveItem 时使用
        public int itemCount = 1;                      // action = GiveItem 时使用
        public int sortOrder;                          // 选项排序（升序）
        public List<DialogueConditionData> conditions; // 该边可达的条件（空 = 无条件）
    }

    /// <summary>
    /// 对话图（静态配置 ScriptableObject）：节点 + 边的图结构，供对话系统按图播放。
    /// </summary>
    [CreateAssetMenu(menuName = "MintLand/Dialogue/Dialogue Graph")]
    public class DialogueGraph : ScriptableObject
    {
        public string graphId;
        public string entryNodeId;              // 入口节点 ID
        public List<DialogueNode> nodes;        // 图中所有节点
        public List<DialogueEdge> edges;        // 节点间的有向边
    }
}
