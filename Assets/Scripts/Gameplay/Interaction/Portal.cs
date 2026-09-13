using UnityEngine;
using UnityEngine.InputSystem;
using MintLandDemo.Core.Game;
using MintLandDemo.Controller.Player;

namespace MintLandDemo.Gameplay.Interaction
{
    /// <summary>
    /// 传送门（可选，Part 6 收尾）。玩家走进触发区域后按 E 切换到目标场景。
    /// 用 OnTriggerEnter 检测；为让 CharacterController（无 Rigidbody）能触发 OnTrigger 事件，
    /// 自身挂一个 kinematic Rigidbody（Awake 中自动配置）。
    /// </summary>
    [RequireComponent(typeof(BoxCollider))]
    [RequireComponent(typeof(Rigidbody))]
    public class Portal : MonoBehaviour
    {
        [SerializeField] public string targetScene;

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
            {
                _playerInside = true;
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.GetComponentInParent<PlayerController>() != null)
            {
                _playerInside = false;
            }
        }

        private void Update()
        {
            if (!_playerInside) return;

            if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
            {
                if (string.IsNullOrEmpty(targetScene))
                {
                    Debug.LogWarning("[Portal] targetScene 未配置，无法传送。");
                    return;
                }

                GameRoot.Instance?.SceneService?.LoadScene(targetScene);
            }
        }
    }
}
