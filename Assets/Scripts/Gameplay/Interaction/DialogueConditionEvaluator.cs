using System.Collections.Generic;
using MintLandDemo.Core.Game;

namespace MintLandDemo.Gameplay.Interaction
{
    /// <summary>
    /// 对话条件求值器：判断一条边/选项的可见条件是否满足。
    /// 读取 GameRoot.Instance.Context.Data 的运行时状态（任务 / 背包 / 金币）。
    /// 失败安全：数据链任一环节为 null 时保守返回 false。
    /// </summary>
    public static class DialogueConditionEvaluator
    {
        /// <summary>全部满足才返回 true；空列表视为满足。</summary>
        public static bool EvaluateAll(List<DialogueConditionData> conditions)
        {
            if (conditions == null || conditions.Count == 0)
                return true;

            foreach (DialogueConditionData c in conditions)
            {
                if (c == null || !Evaluate(c))
                    return false;
            }
            return true;
        }

        private static bool Evaluate(DialogueConditionData c)
        {
            switch (c.type)
            {
                case DialogueConditionType.None:
                    return true;

                case DialogueConditionType.QuestState:
                {
                    GameRuntimeData data = GameRoot.Instance?.Context?.Data;
                    if (data == null || data.quests == null) return false;

                    QuestRuntime quest = data.quests.Find(q => q != null && q.questId == c.questId);
                    switch (c.questState)
                    {
                        case QuestCondition.NotAccepted:
                            return quest == null;
                        case QuestCondition.Accepted:
                            return quest != null && quest.isAccepted && !quest.isCompleted;
                        case QuestCondition.Completed:
                            return quest != null && quest.isCompleted;
                        default:
                            return false;
                    }
                }

                case DialogueConditionType.HasItem:
                {
                    GameRuntimeData data = GameRoot.Instance?.Context?.Data;
                    if (data == null || data.Inventory == null || data.Inventory.items == null) return false;

                    ItemEntry entry = data.Inventory.items.Find(i => i != null && i.itemId == c.itemId);
                    return entry != null && entry.count >= c.itemCount;
                }

                case DialogueConditionType.HasGold:
                {
                    GameRuntimeData data = GameRoot.Instance?.Context?.Data;
                    if (data == null || data.Inventory == null) return false;

                    return data.Inventory.gold >= c.itemCount;
                }

                default:
                    return false;
            }
        }
    }
}
