using UnityEngine;
using MintLandDemo.Core.Event;
using MintLandDemo.Core.Game;

namespace MintLandDemo.Gameplay.Inventory
{
    /// <summary>
    /// 背包系统（纯静态，无状态）。物品数量落地在 GameContext.Data.Inventory，
    /// 变更通过 EventBus 发布 ItemAddedEvent 通知 UI。
    /// </summary>
    public static class InventorySystem
    {
        /// <summary>
        /// 增加物品数量（累加到 InventoryRuntime.items 字典）。
        /// </summary>
        public static void AddItem(string itemId, int count)
        {
            if (string.IsNullOrEmpty(itemId) || count <= 0) return;

            GameRuntimeData data = GameRoot.Instance?.Context?.Data;
            if (data == null || data.Inventory == null) return;

            ItemEntry entry = data.Inventory.items.Find(i => i != null && i.itemId == itemId);
            if (entry == null)
            {
                entry = new ItemEntry { itemId = itemId, count = 0 };
                data.Inventory.items.Add(entry);
            }
            entry.count += count;

            EventBus.Publish(new ItemAddedEvent { ItemId = itemId, Count = count });
            Debug.Log($"[InventorySystem] AddItem -> 发布 ItemAddedEvent: {itemId} x{count}");
        }

        /// <summary>
        /// 检查背包中是否有指定数量的物品。
        /// </summary>
        public static bool HasItem(string itemId, int count)
        {
            if (string.IsNullOrEmpty(itemId)) return count <= 0;

            GameRuntimeData data = GameRoot.Instance?.Context?.Data;
            if (data == null || data.Inventory == null) return false;

            ItemEntry entry = data.Inventory.items.Find(i => i != null && i.itemId == itemId);
            return entry != null && entry.count >= count;
        }

        /// <summary>
        /// 从背包移除指定数量的物品（数量不足则失败，不发布事件）。
        /// </summary>
        public static bool RemoveItem(string itemId, int count)
        {
            if (string.IsNullOrEmpty(itemId) || count <= 0) return false;

            GameRuntimeData data = GameRoot.Instance?.Context?.Data;
            if (data == null || data.Inventory == null) return false;

            ItemEntry entry = data.Inventory.items.Find(i => i != null && i.itemId == itemId);
            if (entry == null || entry.count < count) return false;

            entry.count -= count;
            if (entry.count <= 0)
            {
                data.Inventory.items.Remove(entry);
            }

            return true;
        }
    }
}
