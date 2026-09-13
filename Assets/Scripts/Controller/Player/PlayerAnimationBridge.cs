using UnityEngine;
using UnityEngine.AI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace MintLandDemo.Controller.Player
{
    [RequireComponent(typeof(Animator))]
    [RequireComponent(typeof(CharacterController))]
    public class PlayerAnimationBridge : MonoBehaviour
    {
        [Header("Combo Settings")]
        [SerializeField] private float comboWindow = 0.6f;
        [SerializeField] private int maxComboCount = 2;

        [Header("Attack State")]
        [SerializeField] private string attackTag = "Attack";
        [SerializeField] private string attack1StateName = "Attack1";
        [SerializeField] private string attack2StateName = "Attack2";

        [Header("Attack Displacement")]
        [Tooltip("攻击时是否让玩家位置跟随动画的 Hips 位移")]
        [SerializeField] private bool applyAttackDisplacement = true;
        [Tooltip("补偿速度上限（米/秒），避免瞬移")]
        [SerializeField] private float maxDisplacementSpeed = 5f;

        private Animator _animator;
        private CharacterController _characterController;
        private PlayerController _playerController;
        private NavMeshAgent _navAgent;
        private PlayerControls _inputActions;
        private Transform _bodyBone;

        private readonly int _speedHash = Animator.StringToHash("Speed");
        private readonly int _attackHash = Animator.StringToHash("Attack");
        private readonly int _comboIndexHash = Animator.StringToHash("ComboIndex");

        private int _comboIndex = 0;
        private float _lastAttackTime = -999f;
        private bool _isInAttackState = false;

        // 攻击位移追踪
        private Vector3 _attackStartHipsWorld;
        private Vector3 _maxHipsWorld;
        private bool _hasAttackTracking = false;
        private bool _wasInAttackState = false;

        private void Awake()
        {
            _animator = GetComponent<Animator>();
            _characterController = GetComponent<CharacterController>();
            _playerController = GetComponent<PlayerController>();
            _navAgent = GetComponent<NavMeshAgent>();
            _inputActions = new PlayerControls();

            _bodyBone = _animator.GetBoneTransform(HumanBodyBones.Hips);
            if (_bodyBone == null)
                Debug.LogError("[PlayerAnimationBridge] 未找到 Hips 骨骼！");
        }

        private void OnEnable()
        {
            _inputActions.Enable();
        }

        private void OnDisable()
        {
            _inputActions.Disable();

            if (_playerController != null)
                _playerController.RemoveLock(PlayerLockReason.Attacking);
        }

        private void Update()
        {
            TryHandleAttackInput();

            var state = _animator.GetCurrentAnimatorStateInfo(0);
            _isInAttackState = state.IsTag(attackTag)
                            || state.IsName(attack1StateName)
                            || state.IsName(attack2StateName);

            // 攻击锁
            if (_playerController != null)
            {
                if (_isInAttackState)
                    _playerController.AddLock(PlayerLockReason.Attacking);
                else
                    _playerController.RemoveLock(PlayerLockReason.Attacking);
            }

            // Speed：攻击期间强制为 0
            float speed = 0f;
            if (!_isInAttackState)
            {
                speed = GetCurrentMoveSpeed();
            }
            _animator.SetFloat(_speedHash, speed);
        }

        /// <summary>
        /// 攻击期间：把 Hips 的世界位移应用到 CharacterController。
        /// 原理：
        ///   1. 攻击开始时记录 Hips 世界坐标作为起点。
        ///   2. 追踪整个攻击过程中 Hips 世界坐标的"最大偏移点"。
        ///   3. 一旦 Hips 开始回归（偏移量减小），每帧把 Player 往最大偏移点推。
        /// 效果：Hips 视觉上前冲 → 玩家跟着前移；Hips 回归 → 玩家保持在最远点。
        /// </summary>
        private void LateUpdate()
        {
            if (!applyAttackDisplacement) return;
            if (_bodyBone == null) return;

            bool isAttacking = _isInAttackState;

            // 攻击开始那一帧：记录起始 Hips 世界坐标
            if (isAttacking && !_wasInAttackState)
            {
                _attackStartHipsWorld = _bodyBone.position;
                _maxHipsWorld = _attackStartHipsWorld;
                _hasAttackTracking = true;
            }

            if (isAttacking && _hasAttackTracking)
            {
                Vector3 currentHips = _bodyBone.position;
                Vector3 offsetFromStart = currentHips - _attackStartHipsWorld;
                offsetFromStart.y = 0f;

                float currentOffset = offsetFromStart.magnitude;
                float maxOffset = (_maxHipsWorld - _attackStartHipsWorld).magnitude;

                if (currentOffset > maxOffset)
                {
                    // 还在前冲：更新最大偏移点，Player 不动
                    _maxHipsWorld = currentHips;
                }
                else if (currentOffset < maxOffset - 0.005f)
                {
                    // 回归阶段：Player 补偿，让 Hips 保持在前冲最远点
                    Vector3 diff = _maxHipsWorld - currentHips;
                    diff.y = 0f;

                    if (diff.sqrMagnitude > 0.0001f)
                    {
                        float maxStep = maxDisplacementSpeed * Time.deltaTime;
                        if (diff.magnitude > maxStep)
                            diff = diff.normalized * maxStep;

                        _characterController.Move(diff);
                    }
                }
            }

            if (!isAttacking && _wasInAttackState)
            {
                _hasAttackTracking = false;
            }

            _wasInAttackState = isAttacking;
        }

        private float GetCurrentMoveSpeed()
        {
            float inputSpeed = 0f;
            Vector2 moveInput = _inputActions.Gameplay.Move.ReadValue<Vector2>();
            if (moveInput.sqrMagnitude > 0.01f)
            {
                Vector3 ccVel = new Vector3(
                    _characterController.velocity.x, 0f, _characterController.velocity.z);
                inputSpeed = ccVel.magnitude;
            }

            float agentSpeed = 0f;
            if (_navAgent != null && _navAgent.enabled && _navAgent.isOnNavMesh)
            {
                Vector3 agentVel = new Vector3(
                    _navAgent.velocity.x, 0f, _navAgent.velocity.z);
                agentSpeed = agentVel.magnitude;
            }

            return Mathf.Max(inputSpeed, agentSpeed);
        }

        private void TryHandleAttackInput()
        {
            if (!_inputActions.Gameplay.Attack.WasPerformedThisFrame()) return;

            if (_playerController != null && _playerController.IsBlockedExceptAttacking) return;

            if (EventSystem.current != null &&
                EventSystem.current.IsPointerOverGameObject())
                return;

            if (Time.time - _lastAttackTime <= comboWindow)
                _comboIndex = (_comboIndex + 1) % maxComboCount;
            else
                _comboIndex = 0;

            _lastAttackTime = Time.time;
            _animator.SetInteger(_comboIndexHash, _comboIndex);
            _animator.SetTrigger(_attackHash);
        }
    }
}