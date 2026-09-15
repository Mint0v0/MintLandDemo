using UnityEngine;
using UnityEngine.InputSystem;
using MintLandDemo.Core.Game;
using MintLandDemo.Controller.Player;

namespace MintLandDemo.Gameplay.Interaction
{
    /// <summary>
    /// 传送门。两种模式：
    ///   1. 场景内传送：传送到 targetPointName 指定的场景对象位置。
    ///   2. 跨场景传送：加载 targetScene，可选落到指定 SpawnPoint。
    ///
    /// 注意：用字符串名字而不是 Transform 引用，因为本组件可能挂在 Prefab 上，
    /// Prefab 无法引用场景对象。
    /// </summary>
    [RequireComponent(typeof(BoxCollider))]
    [RequireComponent(typeof(Rigidbody))]
    public class Portal : MonoBehaviour
    {
        [Header("模式")]
        [Tooltip("勾选 = 场景内传送到 Target Point Name；不勾 = 加载 Target Scene")]
        [SerializeField] private bool sceneInternal = true;

        [Header("场景内传送")]
        [Tooltip("目标场景里空物体的名字（运行时 GameObject.Find 查找）")]
        [SerializeField] private string targetPointName = "";

        [Header("跨场景传送")]
        [SerializeField] public string targetScene;
        [SerializeField] public string targetSpawnPointName = "";

        private bool _playerInside;

        private void Awake()
        {
            BoxCollider col = GetComponent<BoxCollider>();
            col.isTrigger = true;

            Rigidbody rb = GetComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.GetComponentInParent<PlayerController>() != null)
                _playerInside = true;
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.GetComponentInParent<PlayerController>() != null)
                _playerInside = false;
        }

        private void Update()
        {
            if (!_playerInside) return;
            if (Keyboard.current == null || !Keyboard.current.eKey.wasPressedThisFrame) return;

            if (sceneInternal)
            {
                TeleportInScene();
                return;
            }

            // 跨场景
            if (string.IsNullOrEmpty(targetScene))
            {
                Debug.LogWarning("[Portal] targetScene 未配置，无法传送。");
                return;
            }

            GameRoot.Instance?.SceneService?.LoadScene(targetScene);
        }

        private void TeleportInScene()
        {
            if (string.IsNullOrEmpty(targetPointName))
            {
                Debug.LogWarning("[Portal] 场景内传送但 targetPointName 未配置！");
                return;
            }

            var target = GameObject.Find(targetPointName);
            if (target == null)
            {
                Debug.LogWarning($"[Portal] 找不到目标点: {targetPointName}");
                return;
            }

            var player = FindObjectOfType<PlayerController>();
            if (player != null)
            {
                player.TeleportTo(target.transform.position);
                Debug.Log($"[Portal] 传送到 {targetPointName} @ {target.transform.position}");
            }
        }
    }
}