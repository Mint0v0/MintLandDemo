using UnityEngine;
using UnityEngine.InputSystem;
using MintLandDemo.Controller.Player;

namespace MintLandDemo.Controller.Camera
{
    public class ThirdPersonCamera : MonoBehaviour
    {
        [Header("Target")]
        [SerializeField] private Transform target;
        [Tooltip("视线关注的偏移，约角色头肩高度")]
        [SerializeField] private Vector3 targetOffset = new Vector3(0f, 1.4f, 0f);

        [Header("Settings")]
        [SerializeField] private float distance = 5f;
        [SerializeField] private float sensitivity = 2f;

        [Header("Pitch Limit")]
        [SerializeField] private float maxPitch = 60f;
        [Tooltip("向下看最大角度。建议 -15 ~ -20")]
        [SerializeField] private float minPitch = -15f;

        [Header("Height Limit")]
        [Tooltip("摄像机最低允许高度（相对角色脚底）。0.5 表示摄像机不会低于脚底上方 0.5 米")]
        [SerializeField] private float minCameraHeight = 0.5f;

        [Header("Collision")]
        [SerializeField] private float collisionRadius = 0.25f;
        [SerializeField] private float minDistance = 1.2f;
        [Tooltip("哪些层算障碍物。务必取消勾选 Player 所在的层")]
        [SerializeField] private LayerMask collisionMask = ~0;

        private PlayerControls _inputActions;
        private Vector2 _lookInput;
        private float _currentYaw;
        private float _currentPitch;
        private PlayerController _playerController;

        private void Awake()
        {
            _inputActions = new PlayerControls();

            if (target == null)
            {
                var player = FindObjectOfType<PlayerController>();
                if (player != null)
                {
                    target = player.transform;
                    _playerController = player;
                }
            }
            else
            {
                _playerController = target.GetComponent<PlayerController>();
                if (_playerController == null)
                    _playerController = FindObjectOfType<PlayerController>();
            }

            if (target != null)
            {
                Vector3 relativePos = transform.position - target.position;
                _currentYaw = Mathf.Atan2(relativePos.x, relativePos.z) * Mathf.Rad2Deg;
                _currentPitch = Mathf.Asin(relativePos.y / relativePos.magnitude) * Mathf.Rad2Deg;
                _currentPitch = Mathf.Clamp(_currentPitch, minPitch, maxPitch);
            }
        }

        private void OnEnable()
        {
            _inputActions.Enable();
            _inputActions.Gameplay.Look.performed += OnLookPerformed;
            _inputActions.Gameplay.Look.canceled += OnLookCanceled;
        }

        private void OnDisable()
        {
            _inputActions.Gameplay.Look.performed -= OnLookPerformed;
            _inputActions.Gameplay.Look.canceled -= OnLookCanceled;
            _inputActions.Disable();
        }

        private void OnLookPerformed(InputAction.CallbackContext context)
        {
            _lookInput = context.ReadValue<Vector2>();
        }

        private void OnLookCanceled(InputAction.CallbackContext context)
        {
            _lookInput = Vector2.zero;
        }

        private void LateUpdate()
        {
            if (target == null) return;

            bool cameraLocked = _playerController != null && _playerController.IsUIBlocked;

            if (cameraLocked)
            {
                _lookInput = Vector2.zero;
            }
            else
            {
                _currentYaw += _lookInput.x * sensitivity;
                _currentPitch -= _lookInput.y * sensitivity;
                _currentPitch = Mathf.Clamp(_currentPitch, minPitch, maxPitch);
            }

            Vector3 pivot = target.position + targetOffset;

            Quaternion rotation = Quaternion.Euler(_currentPitch, _currentYaw, 0f);
            Vector3 offsetDir = rotation * Vector3.back;

            float actualDistance = distance;
            if (Physics.SphereCast(
                    pivot,
                    collisionRadius,
                    offsetDir,
                    out RaycastHit hit,
                    distance,
                    collisionMask,
                    QueryTriggerInteraction.Ignore))
            {
                actualDistance = Mathf.Max(hit.distance, minDistance);
            }

            Vector3 finalPosition = pivot + offsetDir * actualDistance;

            // 关键：强制最低高度
            float minY = target.position.y + minCameraHeight;
            if (finalPosition.y < minY)
            {
                finalPosition.y = minY;
            }

            transform.position = finalPosition;
            transform.LookAt(pivot);
        }

        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
        }
    }
}