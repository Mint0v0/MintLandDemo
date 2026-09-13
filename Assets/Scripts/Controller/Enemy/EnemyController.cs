using UnityEngine;
using UnityEngine.AI;
using MintLandDemo.Core.Event;
using MintLandDemo.Core.Game;
using MintLandDemo.Gameplay.Combat;
using MintLandDemo.Gameplay.Interaction;

namespace MintLandDemo.Controller.Enemy
{
    public enum EnemyState
    {
        Idle,
        Chase,
        Attack,
        Dead,
    }

    [RequireComponent(typeof(NavMeshAgent))]
    public class EnemyController : MonoBehaviour
    {
        [Header("Config")]
        public EnemyConfig config;
        public float currentHp;
        public EnemyState CurrentState => _state;

        [Header("AI - Detection")]
        [SerializeField] private float detectRadius = 8f;
        [SerializeField] private float loseRadius = 12f;

        [Header("AI - Attack")]
        [SerializeField] private float attackRadius = 2f;
        [SerializeField] private float attackCooldown = 1.5f;
        [SerializeField] private float attackDamage = 10f;
        [SerializeField] private float rotateSpeed = 8f;

        [Header("Knockback")]
        [Tooltip("击退持续时间。0.08 太短几乎看不见，建议 0.15 ~ 0.25")]
        [SerializeField] private float knockbackDuration = 0.2f;
        [Tooltip("Boss 免疫击退")]
        [SerializeField] private bool bossImmuneToKnockback = true;

        [Header("References")]
        [SerializeField] private GameObject portalPrefab;

        private NavMeshAgent _agent;
        private Transform _player;
        private EnemyState _state = EnemyState.Idle;
        private float _lastAttackTime = -999f;
        private Vector3 _spawnPosition;
        private Quaternion _spawnRotation;

        private Vector3 _knockbackVelocity;
        private float _knockbackTimer = 0f;
        private bool _wasChasingBeforeKnockback = false;

        private void Awake()
        {
            _agent = GetComponent<NavMeshAgent>();
            _spawnPosition = transform.position;
            _spawnRotation = transform.rotation;
        }

        private void Start()
        {
            if (config != null)
                currentHp = config.maxHp;

            var playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
                _player = playerObj.transform;

            if (_player == null)
            {
                var pc = FindObjectOfType<MintLandDemo.Controller.Player.PlayerController>();
                if (pc != null) _player = pc.transform;
            }

            EnterIdle();
        }

        private void Update()
        {
            if (_state == EnemyState.Dead) return;

            if (_knockbackTimer > 0f)
            {
                TickKnockback();
                return;
            }

            switch (_state)
            {
                case EnemyState.Idle:   TickIdle();   break;
                case EnemyState.Chase:  TickChase();  break;
                case EnemyState.Attack: TickAttack(); break;
            }
        }

        // -------------------- 击退 --------------------

        public void ApplyKnockback(Vector3 direction, float distance)
        {
            if (_state == EnemyState.Dead) return;
            if (bossImmuneToKnockback && config != null && config.enemyName == "Boss") return;

            direction.y = 0f;
            if (direction.sqrMagnitude < 0.001f) return;
            direction.Normalize();

            float speed = distance / Mathf.Max(knockbackDuration, 0.01f);
            _knockbackVelocity = direction * speed;
            _knockbackTimer = knockbackDuration;

            // ===== 关键修复：暂停 agent 寻路，避免和击退位移打架 =====
            _wasChasingBeforeKnockback = (_state == EnemyState.Chase);
            if (_agent != null && _agent.enabled && _agent.isOnNavMesh)
                _agent.isStopped = true;
        }

        private void TickKnockback()
        {
            _knockbackTimer -= Time.deltaTime;

            if (_agent != null && _agent.enabled && _agent.isOnNavMesh)
            {
                // 用 agent.Move 保证 NavMesh 位置同步
                _agent.Move(_knockbackVelocity * Time.deltaTime);
            }
            else
            {
                transform.position += _knockbackVelocity * Time.deltaTime;
            }

            if (_knockbackTimer <= 0f && _agent != null && _agent.enabled && _agent.isOnNavMesh)
            {
                if (_wasChasingBeforeKnockback && _player != null && _state != EnemyState.Dead)
                {
                    // 击退前在追击 → 继续追
                    _agent.isStopped = false;
                    _agent.SetDestination(_player.position);
                }
                else
                {
                    // 击退前在 Idle / Attack → 保持原地
                    _agent.isStopped = true;
                    _agent.ResetPath();
                }
            }
        }

        // -------------------- 状态：Idle --------------------

        private void EnterIdle()
        {
            _state = EnemyState.Idle;
            if (_agent != null && _agent.isOnNavMesh)
            {
                _agent.isStopped = true;
                _agent.ResetPath();
            }
        }

        private void TickIdle()
        {
            if (_player == null) return;

            float dist = Vector3.Distance(transform.position, _player.position);
            if (dist <= detectRadius)
                EnterChase();
        }

        // -------------------- 状态：Chase --------------------

        private void EnterChase()
        {
            _state = EnemyState.Chase;
            if (_agent != null && _agent.isOnNavMesh)
            {
                _agent.isStopped = false;
                _agent.SetDestination(_player.position);
            }
        }

        private void TickChase()
        {
            if (_player == null) { EnterIdle(); return; }

            float dist = Vector3.Distance(transform.position, _player.position);

            if (dist > loseRadius)
            {
                EnterIdle();
                return;
            }

            if (dist <= attackRadius)
            {
                EnterAttack();
                return;
            }

            if (_agent != null && _agent.isOnNavMesh)
                _agent.SetDestination(_player.position);
        }

        // -------------------- 状态：Attack --------------------

        private void EnterAttack()
        {
            _state = EnemyState.Attack;
            if (_agent != null && _agent.isOnNavMesh)
            {
                _agent.isStopped = true;
                _agent.ResetPath();
            }
        }

        private void TickAttack()
        {
            if (_player == null) { EnterIdle(); return; }

            float dist = Vector3.Distance(transform.position, _player.position);

            if (dist > attackRadius * 1.2f)
            {
                EnterChase();
                return;
            }

            Vector3 dir = _player.position - transform.position;
            dir.y = 0f;
            if (dir.sqrMagnitude > 0.001f)
            {
                Quaternion targetRot = Quaternion.LookRotation(dir);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotateSpeed * Time.deltaTime);
            }

            if (Time.time - _lastAttackTime >= attackCooldown)
            {
                PerformAttack();
                _lastAttackTime = Time.time;
            }
        }

        private void PerformAttack()
        {
            var player = GameRoot.Instance?.Context?.Data?.Player;
            if (player == null) return;
            if (player.currentHp <= 0) return;

            player.currentHp -= (int)attackDamage;
            if (player.currentHp < 0) player.currentHp = 0;

            EventBus.Publish(new PlayerDamagedEvent
            {
                Damage = (int)attackDamage,
                CurrentHp = player.currentHp,
                AttackerId = config?.enemyName ?? "Unknown",
            });

            if (player.currentHp <= 0)
                Debug.Log("[Enemy] 玩家死亡（V1 暂未处理，可后续做复活/失败流程）");
        }

        // -------------------- 受击 / 死亡 --------------------

        public void TakeDamage(int damage)
        {
            if (config == null) return;
            if (_state == EnemyState.Dead) return;

            currentHp -= damage;
            Debug.Log($"[Enemy] {config.enemyName} 受击 -{damage}，剩余 {currentHp}");

            if (_state == EnemyState.Idle)
                EnterChase();

            if (currentHp <= 0f)
                Die();
        }

        private void Die()
        {
            _state = EnemyState.Dead;

            if (_agent != null && _agent.isOnNavMesh)
                _agent.isStopped = true;

            if (config != null && config.enemyName == "Boss")
            {
                if (portalPrefab != null)
                {
                    GameObject portal = Instantiate(portalPrefab, transform.position, Quaternion.identity);
                    Portal portalScript = portal.GetComponent<Portal>();
                    if (portalScript != null)
                        portalScript.targetScene = "01_NewbieVillage";
                    Debug.Log($"[Enemy] Boss 死亡，生成传送门");
                }
                else
                {
                    Debug.LogWarning("[Enemy] portalPrefab 未配置，无法生成传送门");
                }
            }

            EventBus.Publish(new EnemyKilledEvent
            {
                enemyId = config.enemyName,
                position = transform.position
            });
            Debug.Log($"[Enemy] {config.enemyName} 死亡 -> 发布 EnemyKilledEvent");

            Destroy(gameObject);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.5f, 0f, 0.5f);
            Gizmos.DrawWireSphere(transform.position, detectRadius);

            Gizmos.color = new Color(1f, 0f, 0f, 0.5f);
            Gizmos.DrawWireSphere(transform.position, attackRadius);

            Gizmos.color = new Color(1f, 1f, 0f, 0.3f);
            Gizmos.DrawWireSphere(transform.position, loseRadius);
        }
    }
}