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
        [Tooltip("单帧最大补偿位移（米/秒），防止瞬移。建议 50 以上")]
        [SerializeField] private float maxDisplacementSpeed = 50f;
        [Tooltip("攻击结束后继续锁定 Hips 世界坐标的时长，应略大于 Attack->Idle 的 Transition Duration")]
        [SerializeField] private float attackExitTrackingDuration = 0.4f;

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

        private Vector3 _attackStartHipsWorld;
        private Vector3 _maxHipsWorld;
        private Vector3 _lockedHipsWorld;
        private bool _hasAttackTracking = false;
        private bool _wasInAttackState = false;
        private float _attackExitTrackingTimer = 0f;

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

            if (_playerController != null)
            {
                if (_isInAttackState)
                    _playerController.AddLock(PlayerLockReason.Attacking);
                else
                    _playerController.RemoveLock(PlayerLockReason.Attacking);
            }

            float speed = 0f;
            if (!_isInAttackState)
            {
                speed = GetCurrentMoveSpeed();
            }
            _animator.SetFloat(_speedHash, speed);
        }

        private void LateUpdate()
        {
            if (!applyAttackDisplacement) return;
            if (_bodyBone == null) return;

            bool isAttacking = _isInAttackState;

            if (isAttacking && !_wasInAttackState)
            {
                _attackStartHipsWorld = _bodyBone.position;
                _maxHipsWorld = _attackStartHipsWorld;
                _hasAttackTracking = true;
                _attackExitTrackingTimer = 0f;
            }

            if (isAttacking && _hasAttackTracking)
            {
                TrackDuringAttack();
            }

            if (!isAttacking && _wasInAttackState && _hasAttackTracking)
            {
                _lockedHipsWorld = _maxHipsWorld;
                _attackExitTrackingTimer = attackExitTrackingDuration;
            }

            if (!isAttacking && _attackExitTrackingTimer > 0f && _hasAttackTracking)
            {
                _attackExitTrackingTimer -= Time.deltaTime;

                CompensateTo(_lockedHipsWorld);

                if (_attackExitTrackingTimer <= 0f)
                    _hasAttackTracking = false;
            }

            _wasInAttackState = isAttacking;
        }

        /// <summary>
        /// 攻击期间：只追踪"Hips 相对起点沿角色 forward 方向的最大位移"。
        /// 前摇阶段的"向后移动"会被忽略，避免玩家被推后。
        /// </summary>
        private void TrackDuringAttack()
        {
            Vector3 currentHips = _bodyBone.position;
            Vector3 offset = currentHips - _attackStartHipsWorld;
            offset.y = 0f;

            Vector3 forward = transform.forward;
            forward.y = 0f;
            forward.Normalize();

            float forwardOffset = Vector3.Dot(offset, forward);
            float maxForwardOffset = Vector3.Dot(_maxHipsWorld - _attackStartHipsWorld, forward);

            if (forwardOffset > maxForwardOffset && forwardOffset > 0f)
            {
                _maxHipsWorld = currentHips;
            }
            else if (forwardOffset < maxForwardOffset - 0.001f && maxForwardOffset > 0f)
            {
                CompensateTo(_maxHipsWorld);
            }
        }

        /// <summary>
        /// 只沿角色 forward 方向补偿，忽略侧向和后向的偏差。
        /// </summary>
        private void CompensateTo(Vector3 targetHipsWorld)
        {
            Vector3 currentHips = _bodyBone.position;

            Vector3 forward = transform.forward;
            forward.y = 0f;
            forward.Normalize();

            float forwardDiff = Vector3.Dot(targetHipsWorld - currentHips, forward);
            if (Mathf.Abs(forwardDiff) < 0.0001f) return;

            Vector3 moveVec = forward * forwardDiff;

            float maxStep = maxDisplacementSpeed * Time.deltaTime;
            if (moveVec.magnitude > maxStep)
                moveVec = moveVec.normalized * maxStep;

            _characterController.Move(moveVec);
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