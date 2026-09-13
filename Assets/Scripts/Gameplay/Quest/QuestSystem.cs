using System.Collections.Generic;
using System.Collections;
using MintLandDemo.Gameplay.Inventory;
using MintLandDemo.Gameplay.Navigation;
using UnityEngine;
using MintLandDemo.Core.Event;
using MintLandDemo.Core.Game;

namespace MintLandDemo.Gameplay.Quest
{
    /// <summary>
    /// 任务系统（System 服务）。挂载在场景中的 GameObject 上（建议与 GameRoot 同级）。
    /// 任务状态落地在 GameContext.Data（quests / activeQuest），并通过 EventBus 发布事件。
    /// </summary>
    public class QuestSystem : MonoBehaviour
    {
        [SerializeField] private List<QuestConfig> questConfigs;

        private GameRuntimeData Data => GameRoot.Instance?.Context?.Data;

        private const string KillSlimeQuestId = "quest_kill_slime";
        private const string ForestBossQuestId = "quest_forest_boss";

        // 敌人 Config 名（EnemyConfig.enemyName）→ 击杀任务 ID 的映射键。
        private const string SlimeEnemyId = "Slime";
        private const string BossEnemyId = "Boss";

        private void Awake()
        {
            EventBus.Subscribe<EnemyKilledEvent>(OnEnemyKilled);
            EventBus.Subscribe<ItemAddedEvent>(OnItemAdded);
        }

        private void OnDestroy()
        {
            // 场景切换会销毁并重建本系统，必须取消订阅避免重复计数（Part 5）。
            EventBus.Unsubscribe<EnemyKilledEvent>(OnEnemyKilled);
            EventBus.Unsubscribe<ItemAddedEvent>(OnItemAdded);
        }

        /// <summary>
        /// 击杀事件回调：推进「消灭史莱姆」任务进度（数据落地 + 事件驱动，不直接改 NPC）。
        /// 达到 targetProgress 时由 UpdateQuestProgress 内部触发 CompleteQuest。
        /// </summary>
        private void OnEnemyKilled(EnemyKilledEvent evt)
        {
            GameRuntimeData data = Data;
            if (data == null) return;

            string questId = MapEnemyToQuest(evt.enemyId);
            if (string.IsNullOrEmpty(questId)) return;

            TryAdvanceQuest(data, questId);
        }

        /// <summary>
        /// 击杀敌人 → 击杀任务 ID 的映射（Part 6 收尾：Slime → 消灭史莱姆，Boss → 讨伐森林 Boss）。
        /// 只认明确匹配的敌人名，其余击杀不推进任何任务，避免误计数。
        /// </summary>
        private static string MapEnemyToQuest(string enemyId)
        {
            if (string.IsNullOrEmpty(enemyId)) return null;

            switch (enemyId)
            {
                case SlimeEnemyId: return KillSlimeQuestId;
                case BossEnemyId: return ForestBossQuestId;
                default: return null;
            }
        }

        private void TryAdvanceQuest(GameRuntimeData data, string questId)
        {
            QuestRuntime quest = data.quests.Find(q => q.isAccepted && !q.isCompleted && q.questId == questId);
            if (quest == null) return;

            UpdateQuestProgress(questId, 1);
            Debug.Log($"[QuestSystem] 击杀进度更新: {quest.currentProgress}/{quest.targetProgress}");
        }

        public void AcceptQuest(string questId)
    {
        GameRuntimeData data = Data;

        if (data == null) return;

        if (data.quests.Exists(q => q.questId == questId))
        {
            return;
        }

        QuestConfig config = FindConfig(questId);

        if (config == null)
        {
            return;
        }

        QuestRuntime runtime = new QuestRuntime
        {
            questId = config.questId,
            questName = config.questName,
            description = config.description,
            currentProgress = 0,
            targetProgress = config.targetCount,
            isAccepted = true,
            isCompleted = false
        };

        data.quests.Add(runtime);
        data.activeQuest = runtime;

        EventBus.Publish(new QuestAcceptedEvent { QuestId = runtime.questId, QuestName = runtime.questName });
        Debug.Log($"[QuestSystem] AcceptQuest -> 发布 QuestAcceptedEvent: {runtime.questId} ({runtime.questName})");
    }

        public void UpdateQuestProgress(string questId, int increment)
        {
            GameRuntimeData data = Data;
            if (data == null) return;

            QuestRuntime quest = data.quests.Find(q => q.questId == questId);
            if (quest == null || !quest.isAccepted || quest.isCompleted) return;

            quest.currentProgress += increment;
            if (quest.currentProgress >= quest.targetProgress)
            {
                CompleteQuest(questId);
            }
        }

        public void CompleteQuest(string questId)
        {
            GameRuntimeData data = Data;
            if (data == null) return;

            QuestRuntime quest = data.quests.Find(q => q.questId == questId);
            if (quest == null || quest.isCompleted) return;

            quest.isCompleted = true;
            quest.currentProgress = quest.targetProgress;

            EventBus.Publish(new QuestCompletedEvent { QuestId = quest.questId, QuestName = quest.questName });
            Debug.Log($"[QuestSystem] CompleteQuest -> 发布 QuestCompletedEvent: {quest.questId} ({quest.questName})");

            // 完成任务后自动接续下一个任务
            QuestConfig config = FindConfig(questId);
            if (config != null && !string.IsNullOrEmpty(config.nextQuestId))
            {
                AcceptQuest(config.nextQuestId);
                Debug.Log($"[QuestSystem] 自动接取后续任务: {config.nextQuestId}");
            }

            // 主线收尾：完成「讨伐森林 Boss」→ 发布 Demo 完成事件（Part 6）。
            if (quest.questId == ForestBossQuestId)
            {
                EventBus.Publish(new DemoCompletedEvent());
                Debug.Log("[QuestSystem] 发布 DemoCompletedEvent（主线流程收尾）");
            }

            // 关键进度变化 → 自动存档（Part 5）。
            GameRoot.Instance?.SaveService?.Save(data);
        }

        /// <summary>按 ID 返回任务配置（供自动寻路读取 targetNPCId / targetScene）。</summary>
        public QuestConfig GetQuestConfig(string questId)
        {
            return FindConfig(questId);
        }

        private QuestConfig FindConfig(string questId)
        {
            if (questConfigs == null) return null;
            return questConfigs.Find(c => c != null && c.questId == questId);
        }
        
        private void OnItemAdded(ItemAddedEvent evt)
        {
            if (evt.ItemId != "potion") return;
            
            var data = Data;
            if (data == null) return;
            
            var quest = data.quests.Find(q => q.questId == "quest_heal" && q.isAccepted && !q.isCompleted);
            if (quest == null) return;
            
            // 直接完成 quest_heal（自动触发 nextQuestId 接续）
            CompleteQuest("quest_heal");
        }

        private System.Collections.IEnumerator DelayedNavigateToHeal()
        {
            yield return null; // 等待一帧，确保 NPC1 对话完全结束
            if (NavigationSystem.Instance != null)
            {
                NavigationSystem.Instance.NavigateToQuest("quest_heal");
            }
        }
    }
}
