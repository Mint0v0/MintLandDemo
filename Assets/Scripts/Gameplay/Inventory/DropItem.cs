using UnityEngine;

namespace MintLandDemo.Gameplay.Inventory
{
    /// <summary>
    /// 掉落物（V1 用球形占位）。玩家靠近后自动拾取并销毁自身。
    /// 拾取采用"距离检测"方案，避免 CharacterController + Trigger 的物理兼容性问题。
    /// </summary>
    public class DropItem : MonoBehaviour
    {
        public string itemId;
        public int itemCount = 1;

        [Header("Pickup")]
        [Tooltip("距离玩家多近时自动拾取（米）")]
        [SerializeField] private float autoPickupRadius = 1.2f;

        private Transform _player;

        private void Start()
        {
            // Collider 依然保留，避免未来需要和其他物理体交互时缺组件
            SphereCollider col = GetComponent<SphereCollider>();
            if (col == null)
            {
                col = gameObject.AddComponent<SphereCollider>();
            }
            col.isTrigger = true;
            col.radius = 0.6f;

            var playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null) _player = playerObj.transform;
        }

        private void Update()
        {
            if (_player == null || string.IsNullOrEmpty(itemId)) return;

            // 只比较水平距离，忽略 Y 轴，避免玩家跳跃时距离变大
            Vector3 a = transform.position;
            Vector3 b = _player.position;
            a.y = 0f;
            b.y = 0f;

            if (Vector3.Distance(a, b) <= autoPickupRadius)
            {
                Pickup();
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            // 物理触发作为冗余（某些情况下距离检测还没到时它就能提前拾取）
            if (!other.CompareTag("Player")) return;
            Pickup();
        }

        private void Pickup()
        {
            if (string.IsNullOrEmpty(itemId)) return;

            InventorySystem.AddItem(itemId, itemCount);
            Debug.Log($"[DropItem] 拾取 {itemId} x{itemCount}");
            Destroy(gameObject);
        }
    }
}