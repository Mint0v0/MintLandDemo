using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using MintLandDemo.Core.Event;
using MintLandDemo.Controller.Enemy;
using MintLandDemo.Controller.Player;

namespace MintLandDemo.Gameplay.Combat
{
    /// <summary>
    /// 锁定系统：Tab 切换锁定，用加权评分选择最优目标。
    /// 评分 = 视野角度 × angleWeight + 距离 × distanceWeight。
    /// 只广播事件，不直接操作 UI / 摄像机。
    /// </summary>
    public class LockOnSystem : MonoBehaviour
    {
        [Header("Lock-On Settings")]
        [Tooltip("搜索半径")]
        [SerializeField] private float lockRadius = 15f;
        [Tooltip("只考虑玩家正前方这个全角范围内的敌人，360 表示全方位")]
        [SerializeField] private float maxLockAngle = 360f;
        [Tooltip("角度在评分里的权重，越大越偏向视野中心")]
        [SerializeField] private float angleWeight = 1f;
        [Tooltip("距离在评分里的权重，越大越偏向近处")]
        [SerializeField] private float distanceWeight = 0.2f;

        [Header("References")]
        [SerializeField] private Transform playerTransform;

        [Header("Debug")]
        [Tooltip("是否打印锁定/解锁/无目标日志")]
        [SerializeField] private bool debugLockOn = true;

        private readonly List<EnemyController> _enemies = new List<EnemyController>();
        private EnemyController _currentTarget;

        public EnemyController CurrentTarget => _currentTarget;
        public bool HasTarget => _currentTarget != null;

        private void Awake()
        {
            if (playerTransform == null)
            {
                var pc = FindObjectOfType<PlayerController>();
                if (pc != null) playerTransform = pc.transform;
            }
        }

        private void Start()
        {
            RefreshEnemyList();
        }

        private void Update()
        {
            // 清理已销毁的敌人
            _enemies.RemoveAll(e => e == null);

            // 目标死亡 / 被销毁 → 自动解除
            if (_currentTarget != null)
            {
                if (_currentTarget.CurrentState == EnemyState.Dead)
                {
                    ClearTarget();
                }
            }

            // Tab 切换
            if (Keyboard.current != null && Keyboard.current.tabKey.wasPressedThisFrame)
            {
                if (HasTarget) ClearTarget();
                else TryAcquireTarget();
            }

            // 超出范围自动解锁
            if (HasTarget && playerTransform != null && _currentTarget != null)
            {
                float dist = Vector3.Distance(playerTransform.position, _currentTarget.transform.position);
                if (dist > lockRadius * 1.2f)
                    ClearTarget();
            }
        }

        /// <summary>重新扫描场景中的敌人。可在刷怪后调用。</summary>
        public void RefreshEnemyList()
        {
            _enemies.Clear();
            _enemies.AddRange(FindObjectsOfType<EnemyController>());
        }

        /// <summary>根据加权评分挑选最优目标。</summary>
        public void TryAcquireTarget()
        {
            if (playerTransform == null) return;

            // 关键修复：每次按 Tab 都重新扫描场景，避免依赖 Start 时的一次性缓存。
            // 每次按 Tab 才执行一次，性能开销可忽略。
            RefreshEnemyList();

            Vector3 playerPos = playerTransform.position;
            Vector3 playerForward = playerTransform.forward;

            EnemyController best = null;
            float bestScore = float.MaxValue;

            foreach (var enemy in _enemies)
            {
                if (enemy == null) continue;
                if (enemy.CurrentState == EnemyState.Dead) continue;

                Vector3 toEnemy = enemy.transform.position - playerPos;
                toEnemy.y = 0f;
                float dist = toEnemy.magnitude;
                if (dist > lockRadius) continue;

                float angle = Vector3.Angle(playerForward, toEnemy);
                if (angle > maxLockAngle * 0.5f) continue;

                // 加权评分：数值越小越优
                float score = angle * angleWeight + dist * distanceWeight;

                if (score < bestScore)
                {
                    bestScore = score;
                    best = enemy;
                }
            }

            if (best != null)
            {
                _currentTarget = best;
                EventBus.Publish(new LockOnChangedEvent { Target = best.transform });
                if (debugLockOn)
                    Debug.Log($"[LockOn] 锁定 {best.name}，评分 {bestScore:F1}");
            }
            else
            {
                // 只在调试模式打，避免失败时每按一次 Tab 就刷一条
                if (debugLockOn)
                    Debug.Log("[LockOn] 范围内没有可锁定的敌人");
            }
        }

        public void ClearTarget()
        {
            if (_currentTarget == null) return;

            _currentTarget = null;
            EventBus.Publish(new LockOnChangedEvent { Target = null });
            if (debugLockOn)
                Debug.Log("[LockOn] 解除锁定");
        }
    }
}