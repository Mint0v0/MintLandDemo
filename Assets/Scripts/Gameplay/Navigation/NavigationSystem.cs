using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;
using MintLandDemo.Controller.Player;
using MintLandDemo.Core.Event;
using MintLandDemo.Core.Game;
using MintLandDemo.Gameplay.Interaction;
using MintLandDemo.Gameplay.Quest;

namespace MintLandDemo.Gameplay.Navigation
{
    public class NavigationSystem : MonoBehaviour
    {
        public static NavigationSystem Instance { get; private set; }

        [Header("寻路参数")]
        [Tooltip("玩家到目标这个距离就算到达")]
        [SerializeField] private float arrivalDistance = 1.5f;
        [SerializeField] private float agentSpeed = 6f;
        [SerializeField] private float agentAngularSpeed = 500f;
        [SerializeField] private float agentAcceleration = 8f;

        [Header("取消输入")]
        [SerializeField] private float cancelInputThreshold = 0.1f;

        [Header("Debug")]
        [SerializeField] private bool debugNavigation = true;

        private string _pendingQuestId = "";
        private bool _isNavigating;
        private PlayerController _player;
        private NavMeshAgent _agent;
        private PlayerControls _inputActions;
        private bool _hasNavigationLock;
        private Vector3 _targetPosition;

        public bool IsNavigating => _isNavigating;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            _inputActions = new PlayerControls();
        }

        private void OnEnable()
        {
            EventBus.Subscribe<SceneChangedEvent>(OnSceneChanged);
            _inputActions.Enable();
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<SceneChangedEvent>(OnSceneChanged);
            _inputActions.Disable();
        }

        private void Update()
        {
            if (!_isNavigating) return;

            Vector2 moveInput = _inputActions.Gameplay.Move.ReadValue<Vector2>();
            bool playerWantsControl = moveInput.sqrMagnitude > cancelInputThreshold * cancelInputThreshold
                                   || _inputActions.Gameplay.Attack.WasPerformedThisFrame();

            if (playerWantsControl)
            {
                if (debugNavigation) Debug.Log("[NavigationSystem] 玩家手动输入，取消自动寻路。");
                CancelNavigation();
                return;
            }

            if (_player == null) return;

            // 关键修改：用玩家到目标的真实距离判定到达
            float distToTarget = Vector3.Distance(_player.transform.position, _targetPosition);

            if (debugNavigation)
            {
                Debug.Log($"[NavigationSystem] distToTarget={distToTarget:F2} (阈值 {arrivalDistance})");
            }

            if (distToTarget <= arrivalDistance)
            {
                if (debugNavigation) Debug.Log($"[NavigationSystem] 到达 (dist={distToTarget:F2})");
                OnReachedDestination();
            }
        }

        public void NavigateToQuest(string questId)
        {
            QuestConfig config = ResolveQuestConfig(questId);
            if (config == null)
            {
                Debug.LogWarning($"[NavigationSystem] 未找到任务配置: {questId}，无法寻路。");
                return;
            }
            if (string.IsNullOrEmpty(config.targetNPCId))
            {
                Debug.LogWarning($"[NavigationSystem] 任务 {questId} 未配置 targetNPCId，无法寻路。");
                return;
            }

            _pendingQuestId = questId;
            _isNavigating = true;

            string current = GameRoot.Instance?.SceneService?.GetCurrentSceneName() ?? "";
            if (!string.IsNullOrEmpty(config.targetScene) && config.targetScene != current)
            {
                Debug.Log($"[NavigationSystem] 目标 NPC {config.targetNPCId} 在场景 {config.targetScene}，切换场景后继续寻路。");
                GameRoot.Instance?.SceneService?.LoadScene(config.targetScene);
                return;
            }

            StartNavigation(config);
        }

        public void CancelNavigation()
        {
            if (!_isNavigating && !_hasNavigationLock) return;

            _isNavigating = false;
            _pendingQuestId = "";
            StopAgent();
            ReleaseNavigationLock();
        }

        private void OnSceneChanged(SceneChangedEvent evt)
        {
            if (_isNavigating && !string.IsNullOrEmpty(_pendingQuestId))
            {
                QuestConfig config = ResolveQuestConfig(_pendingQuestId);
                if (config == null)
                {
                    Abort();
                    return;
                }
                StartNavigation(config);
            }
        }

        private void StartNavigation(QuestConfig config)
        {
            _player = FindObjectOfType<PlayerController>();
            _agent = _player != null ? _player.GetComponent<NavMeshAgent>() : null;

            InteractableNPC npc = FindNPC(config.targetNPCId);
            if (npc == null)
            {
                Debug.LogWarning($"[NavigationSystem] 当前场景未找到目标 NPC: {config.targetNPCId}。");
                Abort();
                return;
            }
            if (_player == null)
            {
                Debug.LogWarning("[NavigationSystem] 未找到 PlayerController，无法寻路。");
                Abort();
                return;
            }

            _targetPosition = npc.transform.position;

            if (_agent == null)
                _agent = _player.gameObject.AddComponent<NavMeshAgent>();

            ConfigureAgent(_agent);
            _agent.enabled = true;
            _agent.isStopped = false;
            _agent.ResetPath();

            _agent.updatePosition = false;
            _agent.updateRotation = false;

            if (!_agent.SetDestination(_targetPosition))
            {
                Debug.LogWarning($"[NavigationSystem] 无法计算到 NPC {config.targetNPCId} 的路径。");
                Abort();
                return;
            }

            AcquireNavigationLock();

            Debug.Log($"[NavigationSystem] 开始寻路 → NPC {config.targetNPCId}，目标点 {_targetPosition}");
        }

        private void OnReachedDestination()
        {
            _isNavigating = false;
            _pendingQuestId = "";
            StopAgent();
            ReleaseNavigationLock();
        }

        private void AcquireNavigationLock()
        {
            if (_hasNavigationLock || _player == null) return;
            _player.AddLock(PlayerLockReason.Navigating);
            _hasNavigationLock = true;
        }

        private void ReleaseNavigationLock()
        {
            if (!_hasNavigationLock) return;

            if (_player != null)
                _player.RemoveLock(PlayerLockReason.Navigating);

            _hasNavigationLock = false;
        }

        private void StopAgent()
        {
            if (_agent == null) return;

            if (_agent.enabled)
            {
                _agent.isStopped = true;
                _agent.ResetPath();
                _agent.updatePosition = true;
                _agent.updateRotation = true;
                _agent.enabled = false;
            }
        }

        private void Abort()
        {
            _isNavigating = false;
            _pendingQuestId = "";
            StopAgent();
            ReleaseNavigationLock();
        }

        private QuestConfig ResolveQuestConfig(string questId)
        {
            QuestSystem questSystem = FindObjectOfType<QuestSystem>();
            return questSystem != null ? questSystem.GetQuestConfig(questId) : null;
        }

        private InteractableNPC FindNPC(string npcId)
        {
            if (string.IsNullOrEmpty(npcId)) return null;
            InteractableNPC.NPCRegistry.TryGetValue(npcId, out InteractableNPC npc);
            return npc;
        }

        private void ConfigureAgent(NavMeshAgent agent)
        {
            agent.speed = agentSpeed;
            agent.angularSpeed = agentAngularSpeed;
            agent.acceleration = agentAcceleration;
            agent.stoppingDistance = 0f;
        }
    }
}