using UnityEngine;
using UnityEngine.AI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using MintLandDemo.Core.Game;
using MintLandDemo.Gameplay.Combat;
using MintLandDemo.Gameplay.Equipment;

namespace MintLandDemo.Controller.Player
{
    [System.Flags]
    public enum PlayerLockReason
    {
        None = 0,
        Attacking = 1 << 0,
        Talking = 1 << 1,
        Dead = 1 << 2,
        UI = 1 << 3,
        Navigating = 1 << 4,
    }

    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Transform cameraPivot;

        [Header("Attack Facing")]
        [Tooltip("攻击中持续转向锁定目标的速度（度/秒）。1440 ≈ 瞬间对准")]
        [SerializeField] private float attackFacingSpeed = 1440f;

        [Header("Debug (运行时只读)")]
        [SerializeField, TextArea] private string _debugLockInfo;

        private CharacterController _characterController;
        private NavMeshAgent _navAgent;
        private PlayerControls _inputActions;
        private Vector2 _moveInput;
        private float _verticalVelocity;
        private bool _isGrounded;
        private bool _isPointerOverUI;
        private LockOnSystem _lockOn;

        private PlayerLockReason _lockReasons = PlayerLockReason.None;

        private float MoveSpeed => GameRoot.Instance?.Context?.Data?.Player?.moveSpeed ?? 5f;
        private float RotationSpeed => GameRoot.Instance?.Context?.Data?.Player?.rotationSpeed ?? 10f;

        private const float Gravity = -9.81f;

        public bool IsLocked => _lockReasons != PlayerLockReason.None;
        public bool IsNavigating => (_lockReasons & PlayerLockReason.Navigating) != 0;
        public bool IsAttacking => (_lockReasons & PlayerLockReason.Attacking) != 0;

        public bool IsUIBlocked =>
            (_lockReasons & PlayerLockReason.Talking) != 0 ||
            (_lockReasons & PlayerLockReason.UI) != 0;

        public bool IsBlockedExceptAttacking =>
            (_lockReasons & ~PlayerLockReason.Attacking) != PlayerLockReason.None;

        public void AddLock(PlayerLockReason reason)
        {
            if ((_lockReasons & reason) == reason) return;
            _lockReasons |= reason;
            Debug.Log($"[Lock] +{reason} → 当前 = {_lockReasons}");
        }

        public void RemoveLock(PlayerLockReason reason)
        {
            if ((_lockReasons & reason) == 0) return;
            _lockReasons &= ~reason;
            Debug.Log($"[Lock] -{reason} → 当前 = {_lockReasons}");
        }

        public void ClearLocks()
        {
            Debug.Log($"[Lock] Clear (was {_lockReasons})");
            _lockReasons = PlayerLockReason.None;
        }

        public void SetGameplayInputEnabled(bool enabled)
        {
            // no-op
        }

        private void Awake()
        {
            _characterController = GetComponent<CharacterController>();
            _navAgent = GetComponent<NavMeshAgent>();
            _inputActions = new PlayerControls();
            if (_navAgent != null)
                _navAgent.enabled = false;

            if (cameraPivot == null && UnityEngine.Camera.main != null)
                cameraPivot = UnityEngine.Camera.main.transform;
        }

        private void Start()
        {
            _lockOn = FindObjectOfType<LockOnSystem>();
        }

        private void OnEnable()
        {
            _inputActions.Enable();
            _inputActions.Gameplay.Move.performed += OnMovePerformed;
            _inputActions.Gameplay.Move.canceled += OnMoveCanceled;
        }

        private void OnDisable()
        {
            _inputActions.Gameplay.Move.performed -= OnMovePerformed;
            _inputActions.Gameplay.Move.canceled -= OnMoveCanceled;
            _inputActions.Disable();
        }

        private void OnMovePerformed(InputAction.CallbackContext context)
        {
            _moveInput = context.ReadValue<Vector2>();
        }

        private void OnMoveCanceled(InputAction.CallbackContext context)
        {
            _moveInput = Vector2.zero;
        }

        private void Update()
        {
            if (EventSystem.current != null)
                _isPointerOverUI = EventSystem.current.IsPointerOverGameObject();

            _debugLockInfo = $"Locks: {_lockReasons}\n" +
                             $"moveInput: {_moveInput}\n" +
                             $"isPointerOverUI: {_isPointerOverUI}";

            TryAttack();

            HandleMovement();
            ApplyGravity();
            RotatePlayer();
            FaceTargetWhileAttacking();
            HandleWeaponSwitch();
        }

        private void TryAttack()
        {
            if (!_inputActions.Gameplay.Attack.WasPerformedThisFrame()) return;
            if (IsBlockedExceptAttacking) return;
            if (_isPointerOverUI) return;

            // 攻击瞬间立即转向锁定目标
            if (_lockOn != null && _lockOn.HasTarget)
            {
                Transform target = _lockOn.CurrentTarget.transform;
                Vector3 dir = target.position - transform.position;
                dir.y = 0f;
                if (dir.sqrMagnitude > 0.001f)
                    transform.rotation = Quaternion.LookRotation(dir);
            }

            CombatSystem combat = FindObjectOfType<CombatSystem>();
            if (combat == null) return;

            PlayerRuntime player = GameRoot.Instance?.Context?.Data?.Player;
            combat.PerformAttack(player);
        }

        /// <summary>
        /// 攻击期间每帧强制玩家朝向锁定目标，保证 Attack1 → Attack2 连击时朝向不丢。
        /// </summary>
        private void FaceTargetWhileAttacking()
        {
            if ((_lockReasons & PlayerLockReason.Attacking) == 0) return;
            if (_lockOn == null || !_lockOn.HasTarget) return;

            Transform target = _lockOn.CurrentTarget.transform;
            Vector3 dir = target.position - transform.position;
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.001f) return;

            Quaternion targetRot = Quaternion.LookRotation(dir);
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation, targetRot, attackFacingSpeed * Time.deltaTime);
        }

        private void HandleWeaponSwitch()
        {
            if (Keyboard.current == null) return;
            if (IsLocked) return;

            if (Keyboard.current.digit1Key.wasPressedThisFrame)
                FindObjectOfType<EquipmentSystem>()?.SwitchActiveWeapon(0);
            else if (Keyboard.current.digit2Key.wasPressedThisFrame)
                FindObjectOfType<EquipmentSystem>()?.SwitchActiveWeapon(1);
        }

        /// <summary>
        /// 移动处理。关键：无论玩家是否按 WASD，都调用 CharacterController.Move 应用重力，
        /// 这样传送到空中后角色会立即受重力下落。
        /// </summary>
        private void HandleMovement()
        {
            if (IsNavigating)
            {
                DriveByNavMeshAgent();
                return;
            }

            // 攻击期间位置由动画驱动（OnAnimatorMove），代码不再干预
            if (IsLocked) return;

            Vector3 forward = UnityEngine.Camera.main.transform.forward;
            Vector3 right = UnityEngine.Camera.main.transform.right;
            forward.y = 0f;
            right.y = 0f;
            forward.Normalize();
            right.Normalize();

            Vector3 moveDirection = (forward * _moveInput.y + right * _moveInput.x).normalized;

            // 水平速度：无输入时 moveDirection 为零向量 → 水平位移为 0
            Vector3 horizontalVelocity = moveDirection * MoveSpeed;

            // 垂直速度：始终应用重力
            Vector3 velocity = horizontalVelocity;
            velocity.y = _verticalVelocity;

            _characterController.Move(velocity * Time.deltaTime);
            _isGrounded = _characterController.isGrounded;
        }

        private void DriveByNavMeshAgent()
        {
            if (_navAgent == null || !_navAgent.enabled || !_navAgent.isOnNavMesh) return;

            Vector3 desiredVel = _navAgent.desiredVelocity;
            Vector3 horizontalVel = new Vector3(desiredVel.x, 0f, desiredVel.z);

            Vector3 moveVel = new Vector3(horizontalVel.x, _verticalVelocity, horizontalVel.z);
            _characterController.Move(moveVel * Time.deltaTime);
            _isGrounded = _characterController.isGrounded;

            _navAgent.nextPosition = transform.position;

            if (horizontalVel.sqrMagnitude > 0.01f)
            {
                Quaternion targetRot = Quaternion.LookRotation(horizontalVel.normalized);
                transform.rotation = Quaternion.Slerp(
                    transform.rotation, targetRot, RotationSpeed * Time.deltaTime);
            }
        }

        private void ApplyGravity()
        {
            if (_isGrounded && _verticalVelocity < 0f)
            {
                _verticalVelocity = -2f;
            }
            else
            {
                _verticalVelocity += Gravity * Time.deltaTime;
            }
        }

        private void RotatePlayer()
        {
            if (IsLocked) return;

            if (_moveInput.sqrMagnitude > 0.01f)
            {
                Vector3 forward = UnityEngine.Camera.main.transform.forward;
                Vector3 right = UnityEngine.Camera.main.transform.right;
                forward.y = 0f;
                right.y = 0f;
                forward.Normalize();
                right.Normalize();

                Vector3 moveDir = (forward * _moveInput.y + right * _moveInput.x).normalized;
                if (moveDir != Vector3.zero)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(moveDir);
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, RotationSpeed * Time.deltaTime);
                }
            }
        }

        public void TeleportTo(Vector3 position)
        {
            if (_characterController != null) _characterController.enabled = false;
            transform.position = position;
            if (_characterController != null) _characterController.enabled = true;
        }
    }
}