using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using MintLandDemo.Core.Event;
using MintLandDemo.Core.Game;
using MintLandDemo.Gameplay.Quest;
using MintLandDemo.Gameplay.Navigation;

namespace MintLandDemo.UI.Quest
{
    /// <summary>
    /// 任务详情面板。点击“我的任务”按钮后展开。
    /// 显示所有任务（进行中 + 已完成），分颜色显示。
    /// </summary>
    public class QuestDetailUI : MonoBehaviour
    {
        [Header("面板")]
        [SerializeField] private GameObject detailPanel;          // 整个详情面板
        [SerializeField] private Transform questListContainer;    // 任务列表容器
        [SerializeField] private Button questButtonPrefab;        // 任务按钮模板

        [Header("关闭按钮")]
        [SerializeField] private Button closeButton;              // 面板右上角 X

        [Header("切换按钮")]
        [SerializeField] private Button toggleButton;             // “我的任务”按钮

        private readonly List<Button> _spawnedButtons = new List<Button>();
        private bool _isOpen = false;

        private void Start()
        {
            if (toggleButton != null)
            {
                toggleButton.onClick.AddListener(TogglePanel);
            }

            if (closeButton != null)
            {
                closeButton.onClick.AddListener(ClosePanel);
            }

            if (detailPanel != null)
            {
                detailPanel.SetActive(false);
            }

            // 订阅任务事件，面板打开时刷新
            EventBus.Subscribe<QuestAcceptedEvent>(OnQuestChanged);
            EventBus.Subscribe<QuestCompletedEvent>(OnQuestChanged);
        }

        private void OnDestroy()
        {
            EventBus.Unsubscribe<QuestAcceptedEvent>(OnQuestChanged);
            EventBus.Unsubscribe<QuestCompletedEvent>(OnQuestChanged);
        }

        private void OnQuestChanged(QuestAcceptedEvent e) { if (_isOpen) Refresh(); }
        private void OnQuestChanged(QuestCompletedEvent e) { if (_isOpen) Refresh(); }

        private void TogglePanel()
        {
            _isOpen = !_isOpen;
            if (detailPanel != null)
            {
                detailPanel.SetActive(_isOpen);
                if (_isOpen) Refresh();
            }
        }

        private void ClosePanel()
        {
            _isOpen = false;
            if (detailPanel != null) detailPanel.SetActive(false);
        }

        private void Refresh()
        {
            ClearButtons();

            List<QuestRuntime> quests = GameRoot.Instance?.Context?.Data?.quests;
            if (quests == null || quests.Count == 0)
            {
                ShowEmptyMessage("暂无任务");
                return;
            }

            if (questButtonPrefab == null || questListContainer == null)
            {
                Debug.LogWarning("[QuestDetailUI] 预制体或容器未配置");
                return;
            }

            // 1. 进行中任务（亮黄色，可点击寻路）
            foreach (QuestRuntime quest in quests)
            {
                if (quest == null || !quest.isAccepted || quest.isCompleted) continue;

                Button btn = Instantiate(questButtonPrefab, questListContainer);
                btn.onClick.RemoveAllListeners();

                Text label = btn.GetComponentInChildren<Text>();
                if (label != null)
                {
                    label.text = $"{quest.questName} - 进行中";
                    label.color = Color.yellow;
                }

                string capturedId = quest.questId;
                btn.onClick.AddListener(() => OnQuestClicked(capturedId));
                _spawnedButtons.Add(btn);
            }

            // 2. 已完成任务（黑色，不可点击）
            foreach (QuestRuntime quest in quests)
            {
                if (quest == null || !quest.isAccepted || !quest.isCompleted) continue;

                Button btn = Instantiate(questButtonPrefab, questListContainer);
                btn.onClick.RemoveAllListeners();

                Text label = btn.GetComponentInChildren<Text>();
                if (label != null)
                {
                    label.text = $"{quest.questName} - 已完成";
                    label.color = Color.black;
                }

                btn.interactable = false;
                _spawnedButtons.Add(btn);
            }
        }

        private void ShowEmptyMessage(string message)
        {
            if (questListContainer == null || questButtonPrefab == null) return;

            Button btn = Instantiate(questButtonPrefab, questListContainer);
            Text label = btn.GetComponentInChildren<Text>();
            if (label != null)
            {
                label.text = message;
                label.color = Color.gray;
            }
            btn.interactable = false;
            _spawnedButtons.Add(btn);
        }

        private void OnQuestClicked(string questId)
        {
            // 点击进行中的任务 → 关闭详情面板，让玩家用追踪栏操作
            // 或者直接触发寻路，同时关闭面板
            if (NavigationSystem.Instance != null)
            {
                NavigationSystem.Instance.NavigateToQuest(questId);
            }
            ClosePanel(); // 点击后自动关闭面板
        }

        private void ClearButtons()
        {
            foreach (Button btn in _spawnedButtons)
            {
                if (btn != null) Destroy(btn.gameObject);
            }
            _spawnedButtons.Clear();
        }
    }
}