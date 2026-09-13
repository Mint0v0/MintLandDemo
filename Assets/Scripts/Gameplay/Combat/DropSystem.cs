using UnityEngine;
using MintLandDemo.Core.Event;
using MintLandDemo.Gameplay.Inventory;

namespace MintLandDemo.Gameplay.Combat
{
    /// <summary>
    /// 掉落系统。订阅 EnemyKilledEvent，在死亡位置生成掉落物（V1 不用对象池）。
    /// </summary>
    public class DropSystem : MonoBehaviour
    {
        [SerializeField] private DropItem dropItemPrefab;

        private void Awake()
        {
            EventBus.Subscribe<EnemyKilledEvent>(OnEnemyKilled);
        }

        private void OnDestroy()
        {
            // 场景切换会销毁并重建本系统，必须取消订阅避免重复掉落（Part 5）。
            EventBus.Unsubscribe<EnemyKilledEvent>(OnEnemyKilled);
        }

        private void OnEnemyKilled(EnemyKilledEvent evt)
        {
            if (!IsSlime(evt.enemyId)) return;

            if (dropItemPrefab == null)
            {
                Debug.LogWarning("[DropSystem] 未配置 dropItemPrefab，无法生成掉落物。");
                return;
            }

            Instantiate(dropItemPrefab, evt.position, Quaternion.identity);
            Debug.Log($"[DropSystem] 在 {evt.position} 生成掉落物 {dropItemPrefab.itemId}");
        }

        private bool IsSlime(string enemyId)
        {
            return !string.IsNullOrEmpty(enemyId) && enemyId.ToLowerInvariant().Contains("slime");
        }
    }
}
