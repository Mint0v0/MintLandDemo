using UnityEngine;
using MintLandDemo.Core.Event;

namespace MintLandDemo.UI.Combat
{
    /// <summary>
    /// 锁定红框 UI：订阅 LockOnChangedEvent，把 4 条边组成的方框跟随目标的世界坐标。
    /// 挂在 Canvas 下的一个空物体上，该物体下包含 4 个 Image 子节点。
    /// </summary>
    public class LockOnIndicator : MonoBehaviour
    {
        [SerializeField] private RectTransform indicatorRoot;
        [SerializeField] private Vector3 worldOffset = new Vector3(0f, 1.5f, 0f);

        private Transform _target;
        private Camera _camera;

        private void Awake()
        {
            if (indicatorRoot == null) indicatorRoot = GetComponent<RectTransform>();
            _camera = UnityEngine.Camera.main;
            if (indicatorRoot != null) indicatorRoot.gameObject.SetActive(false);

            EventBus.Subscribe<LockOnChangedEvent>(OnLockOnChanged);
        }

        private void OnDestroy()
        {
            EventBus.Unsubscribe<LockOnChangedEvent>(OnLockOnChanged);
        }

        private void OnLockOnChanged(LockOnChangedEvent evt)
        {
            _target = evt.Target;
            if (indicatorRoot != null && _target == null)
                indicatorRoot.gameObject.SetActive(false);
        }

        private void LateUpdate()
        {
            if (_target == null)
            {
                if (indicatorRoot != null) indicatorRoot.gameObject.SetActive(false);
                return;
            }

            if (_camera == null)
            {
                _camera = UnityEngine.Camera.main;
                if (_camera == null) return;
            }

            Vector3 worldPos = _target.position + worldOffset;
            Vector3 screenPos = _camera.WorldToScreenPoint(worldPos);

            // 目标在相机后面时隐藏
            if (screenPos.z < 0f)
            {
                indicatorRoot.gameObject.SetActive(false);
                return;
            }

            indicatorRoot.gameObject.SetActive(true);
            indicatorRoot.position = screenPos;
        }
    }
}