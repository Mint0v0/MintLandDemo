using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using MintLandDemo.Core.Event;
using MintLandDemo.Core.Game;
using MintLandDemo.Gameplay.Navigation;
using MintLandDemo.Gameplay.Quest;

namespace MintLandDemo.UI.Quest
{
    /// <summary>
    /// 任务追踪 UI。屏幕正下方显示所有进行中任务（任务名 + 目标描述）为可点击按钮；
    /// 点击条目 → NavigationSystem.NavigateToQuest(questId) 自动寻路到目标 NPC。
    /// 订阅 QuestAcceptedEvent / QuestCompletedEvent / SceneChangedEvent 刷新列表。
    /// </summary>
    public class QuestTrackerUI : MonoBehaviour
    {
        [Header("任务追踪栏")]
        [SerializeField] private GameObject questPanel;          // 整个追踪栏（无进行中任务时隐藏）
        [SerializeField] private Transform questListContainer;   // 按钮容器（建议挂 VerticalLayoutGroup）
        [SerializeField] private Button questButtonPrefab;       // 任务按钮模板（子物体含 Text）

        private readonly List<Button> _spawnedButtons = new List<Button>();

        private void OnEnable()
        {
            EventBus.Subscribe<QuestAcceptedEvent>(OnQuestChanged);
            EventBus.Subscribe<QuestCompletedEvent>(OnQuestChanged);
            EventBus.Subscribe<SceneChangedEvent>(OnSceneChanged);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<QuestAcceptedEvent>(OnQuestChanged);
            EventBus.Unsubscribe<QuestCompletedEvent>(OnQuestChanged);
            EventBus.Unsubscribe<SceneChangedEvent>(OnSceneChanged);
        }

        private void Start()
        {
            EventBus.Subscribe<QuestAcceptedEvent>(OnQuestChanged);
            EventBus.Subscribe<QuestCompletedEvent>(OnQuestChanged);
            Refresh();
        }

        private void OnQuestChanged(QuestAcceptedEvent e) => Refresh();
        private void OnQuestChanged(QuestCompletedEvent e) => Refresh();
        private void OnSceneChanged(SceneChangedEvent e) => Refresh();

        private void Refresh()
        {
            Debug.Log($"[QuestTrackerUI] Refresh 被调用");
            ClearButtons();

            List<QuestRuntime> quests = GameRoot.Instance?.Context?.Data?.quests;
            if (quests == null)
            {
                SetPanelVisible(false);
                return;
            }

            int spawned = 0;
            foreach (QuestRuntime quest in quests)
            {
                if (quest == null || !quest.isAccepted || quest.isCompleted) continue;

                if (questButtonPrefab == null || questListContainer == null)
                {
                    Debug.LogWarning("[QuestTrackerUI] questButtonPrefab 或 questListContainer 未配置，无法显示任务条目。");
                    break;
                }

                Button btn = Instantiate(questButtonPrefab, questListContainer);
                btn.onClick.RemoveAllListeners();

                Text label = btn.GetComponentInChildren<Text>();
                if (label != null)
                {
                    string desc = string.IsNullOrEmpty(quest.description) ? "" : $"\n{quest.description}";
                    label.text = $"{quest.questName}{desc}";
                }

                string capturedId = quest.questId;
                btn.onClick.AddListener(() => OnQuestClicked(capturedId));
                _spawnedButtons.Add(btn);
                spawned++;
            }

            SetPanelVisible(spawned > 0);
        }

        private void OnQuestClicked(string questId)
        {
            if (NavigationSystem.Instance == null)
            {
                Debug.LogWarning("[QuestTrackerUI] NavigationSystem 未找到，无法寻路。");
                return;
            }
            NavigationSystem.Instance.NavigateToQuest(questId);
        }

        private void ClearButtons()
        {
            foreach (Button btn in _spawnedButtons)
            {
                if (btn != null) Destroy(btn.gameObject);
            }
            _spawnedButtons.Clear();
        }

        private void SetPanelVisible(bool visible)
        {
            if (questPanel != null) questPanel.SetActive(visible);
        }
    }
}