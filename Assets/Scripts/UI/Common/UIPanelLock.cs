using UnityEngine;
using MintLandDemo.Controller.Player;

namespace MintLandDemo.UI.Common
{
    /// <summary>
    /// 挂到任何 UI 面板根节点（带 CanvasGroup 或本身就是那个会 SetActive(false) 的对象）。
    /// OnEnable 时锁定玩家输入和视角，OnDisable 时解锁。
    /// </summary>
    public class UIPanelLock : MonoBehaviour
    {
        private PlayerController _player;

        private void Awake()
        {
            _player = FindObjectOfType<PlayerController>();
            if (_player == null)
                Debug.LogWarning($"[UIPanelLock] {gameObject.name} 未找到 PlayerController，锁不会生效。");
        }

        private void OnEnable()
        {
            if (_player != null)
                _player.AddLock(PlayerLockReason.UI);
        }

        private void OnDisable()
        {
            if (_player != null)
                _player.RemoveLock(PlayerLockReason.UI);
        }
    }
}