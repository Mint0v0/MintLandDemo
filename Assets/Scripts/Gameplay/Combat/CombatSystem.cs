using UnityEngine;
using MintLandDemo.Core.Game;
using MintLandDemo.Controller.Enemy;
using MintLandDemo.Controller.Player;

namespace MintLandDemo.Gameplay.Combat
{
    public class CombatSystem : MonoBehaviour
    {
        [Header("攻击参数")]
        public float attackRange = 2.5f;
        public float attackAngle = 60f;
        public LayerMask enemyLayer;
        public float attackCooldown = 0.5f;

        [Header("Knockback")]
        [Tooltip("命中后怪物被击退的距离")]
        [SerializeField] private float knockbackDistance = 0.6f;

        [Header("Debug")]
        [SerializeField] private bool debugAttack = true;

        private float _lastAttackTime = float.NegativeInfinity;
        private Transform _playerTransform;
        private LockOnSystem _lockOn;

        private Transform PlayerTransform
        {
            get
            {
                if (_playerTransform == null)
                {
                    PlayerController controller = FindObjectOfType<PlayerController>();
                    if (controller != null) _playerTransform = controller.transform;
                }
                return _playerTransform;
            }
        }

        private void Start()
        {
            _lockOn = FindObjectOfType<LockOnSystem>();
        }

        public void PerformAttack(PlayerRuntime player)
        {
            if (debugAttack) Debug.Log($"[Combat] PerformAttack 被调用, player null? {player == null}");

            if (player == null) return;
            if (Time.time - _lastAttackTime < attackCooldown)
            {
                if (debugAttack) Debug.Log($"[Combat] 跳过: 冷却中");
                return;
            }

            _lastAttackTime = Time.time;

            Transform origin = PlayerTransform;
            if (origin == null)
            {
                if (debugAttack) Debug.LogError("[Combat] 跳过: PlayerTransform 为 null");
                return;
            }

            EnemyController enemy = FindNearestEnemy(origin);
            if (enemy == null)
            {
                if (debugAttack) Debug.Log("[Combat] 跳过: 未找到敌人");
                return;
            }

            int damage = DamageCalculator.CalculateDamage(player.finalAttack, enemy.config.defense);
            enemy.TakeDamage(damage);

            // 击退方向：使用玩家正面朝向，避免玩家位置和怪物位置接近时方向不稳定
            Vector3 knockbackDir = origin.forward;
            knockbackDir.y = 0f;
            enemy.ApplyKnockback(knockbackDir, knockbackDistance);

            Debug.Log($"[Combat] 攻击造成 {damage} 点伤害，击退 {knockbackDistance} 米。");
        }

        private EnemyController FindNearestEnemy(Transform origin)
        {
            // 优先使用锁定目标
            if (_lockOn != null && _lockOn.HasTarget)
            {
                var locked = _lockOn.CurrentTarget;
                if (locked != null && locked.config != null && locked.CurrentState != EnemyState.Dead)
                {
                    Vector3 toLocked = locked.transform.position - origin.position;
                    toLocked.y = 0f;
                    float lockedDist = toLocked.magnitude;
                    float lockedAngle = Vector3.Angle(origin.forward, toLocked);

                    if (lockedDist <= attackRange && lockedAngle <= attackAngle * 0.5f)
                    {
                        if (debugAttack)
                            Debug.Log($"[Combat] 使用锁定目标 {locked.name}, dist={lockedDist:F2}, angle={lockedAngle:F1}");
                        return locked;
                    }
                    else if (debugAttack)
                    {
                        Debug.Log($"[Combat] 锁定目标超出范围，回落到普通检测 (dist={lockedDist:F2}, angle={lockedAngle:F1})");
                    }
                }
            }

            Collider[] hits = Physics.OverlapSphere(origin.position, attackRange, enemyLayer);
            if (debugAttack) Debug.Log($"[Combat] OverlapSphere 命中 {hits.Length} 个 Collider");

            EnemyController nearest = null;
            float nearestDist = float.MaxValue;

            foreach (Collider col in hits)
            {
                EnemyController enemy = col.GetComponentInParent<EnemyController>();
                if (enemy == null)
                {
                    if (debugAttack) Debug.Log($"[Combat]   - {col.name}: 无 EnemyController");
                    continue;
                }
                if (enemy.config == null)
                {
                    if (debugAttack) Debug.Log($"[Combat]   - {col.name}: 无 config");
                    continue;
                }
                if (enemy.CurrentState == EnemyState.Dead) continue;

                Vector3 toEnemy = enemy.transform.position - origin.position;
                toEnemy.y = 0f;
                float dist = toEnemy.magnitude;
                if (dist < 0.01f) continue;

                float angle = Vector3.Angle(origin.forward, toEnemy);
                if (debugAttack)
                    Debug.Log($"[Combat]   - {col.name}: dist={dist:F2}, angle={angle:F1} (阈值 {attackAngle * 0.5f})");

                if (angle > attackAngle * 0.5f) continue;

                if (dist < nearestDist)
                {
                    nearestDist = dist;
                    nearest = enemy;
                }
            }

            return nearest;
        }
    }
}