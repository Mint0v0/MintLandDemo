using UnityEngine;
using MintLandDemo.Core.Event;
using MintLandDemo.Core.Game;
using MintLandDemo.Gameplay.Equipment;
using MintLandDemo.Gameplay.Inventory;

namespace MintLandDemo.Gameplay.Shop
{
    /// <summary>
    /// 商店系统。出售史莱姆粘液换取金币，用金币购买宝石。
    /// 金币落地在 InventoryRuntime.gold，变更通过 GoldChangedEvent 通知 UI。
    /// </summary>
    public class ShopSystem : MonoBehaviour
    {
        private const string SlimeMucusItemId = "slime_mucus";
        private const int SlimeMucusPrice = 10;

        private GameRuntimeData Data => GameRoot.Instance?.Context?.Data;

        /// <summary>
        /// 出售物品（V1 仅支持史莱姆粘液，每个 10 金币）。
        /// </summary>
        public bool SellItem(string itemId, int count)
        {
            if (string.IsNullOrEmpty(itemId) || count <= 0) return false;

            int price = GetSellPrice(itemId);
            if (price <= 0)
            {
                Debug.LogWarning($"[ShopSystem] 物品 {itemId} 不可出售。");
                return false;
            }

            if (!InventorySystem.HasItem(itemId, count))
            {
                Debug.LogWarning($"[ShopSystem] 背包中 {itemId} 数量不足。");
                return false;
            }

            GameRuntimeData data = Data;
            if (data == null || data.Inventory == null) return false;

            InventorySystem.RemoveItem(itemId, count);
            data.Inventory.gold += price * count;

            EventBus.Publish(new GoldChangedEvent { Gold = data.Inventory.gold });
            Debug.Log($"[ShopSystem] 出售 {itemId} x{count}，金币 +{price * count}（当前 {data.Inventory.gold}）。");

            // 关键进度变化 → 自动存档（Part 5）。
            GameRoot.Instance?.SaveService?.Save(data);
            return true;
        }

        /// <summary>
        /// 购买宝石：扣除金币，宝石作为物品存入背包（内部发布 ItemAddedEvent）。
        /// </summary>
        public bool BuyGem(GemConfig gem)
        {
            if (gem == null) return false;

            GameRuntimeData data = Data;
            if (data == null || data.Inventory == null) return false;

            if (data.Inventory.gold < gem.price)
            {
                Debug.LogWarning($"[ShopSystem] 金币不足，无法购买 {gem.gemName}（需要 {gem.price}）。");
                return false;
            }

            data.Inventory.gold -= gem.price;
            EventBus.Publish(new GoldChangedEvent { Gold = data.Inventory.gold });

            InventorySystem.AddItem(gem.gemName, 1); // 内部发布 ItemAddedEvent

            Debug.Log($"[ShopSystem] 购买 {gem.gemName} 成功，剩余金币 {data.Inventory.gold}。");

            // 关键进度变化 → 自动存档（Part 5）。
            GameRoot.Instance?.SaveService?.Save(data);
            return true;
        }

        private int GetSellPrice(string itemId)
        {
            if (itemId == SlimeMucusItemId) return SlimeMucusPrice;
            return 0;
        }
    }
}
