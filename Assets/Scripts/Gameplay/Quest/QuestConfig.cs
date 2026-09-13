using UnityEngine;

namespace MintLandDemo.Gameplay.Quest
{
    /// <summary>
    /// 任务模板（静态配置）。V1 为简单主线链式任务。
    /// targetCount 为目标进度数量；nextQuestId 为后续任务 ID（V1 子阶段 A 由对话动作驱动推进，暂不自动链式接取）。
    /// </summary>
    [CreateAssetMenu(menuName = "MintLand/Quest/Quest Config")]
    public class QuestConfig : ScriptableObject
    {
        public string questId;
        public string questName;
        public string description;
        public int targetCount;
        public string nextQuestId;
        public string targetNPCId;      // 目标 NPC 的 ID（与 InteractableNPC.npcId 对应，供自动寻路）
        public string targetScene;      // 目标所在场景名（如 "01_NewbieVillage"；留空 = 当前场景）
    }
}
