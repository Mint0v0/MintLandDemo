using UnityEngine;
using MintLandDemo.Core.Event;
using MintLandDemo.Core.Game;
using MintLandDemo.Gameplay.Inventory;
using MintLandDemo.Gameplay.Quest;
using MintLandDemo.UI.Dialogue;
using MintLandDemo.UI.Shop;

namespace MintLandDemo.Gameplay.Interaction
{
    /// <summary>
    /// 对话指令执行器：执行一个对话动作（接受/完成任务、给予/扣除物品、开店、结束交易）。
    /// 从 DialogueManager 抽取，供线性对话与图对话复用。参数版兼容 DialogueNode 与 DialogueEdge。
    /// </summary>
    public static class DialogueInstructionExecutor
    {
        /// <summary>执行一个对话动作。</summary>
        public static void Execute(DialogueAction action, string questId, string itemId, int itemCount)
        {
            if (action != DialogueAction.None)
            {
                Debug.Log($"[DialogueInstructionExecutor] 执行动作: {action}, QuestId: {questId}");
            }

            switch (action)
            {
                case DialogueAction.AcceptQuest:
                {
                    QuestSystem questSystem = Object.FindObjectOfType<QuestSystem>();
                    if (questSystem == null)
                    {
                        Debug.LogError("[DialogueInstructionExecutor] QuestSystem 引用为空！请检查场景中是否有 QuestSystem 组件。");
                        return;
                    }
                    questSystem.AcceptQuest(questId);
                    break;
                }

                case DialogueAction.CompleteQuest:
                {
                    QuestSystem questSystem = Object.FindObjectOfType<QuestSystem>();
                    if (questSystem != null) questSystem.CompleteQuest(questId);
                    break;
                }

                case DialogueAction.GiveItem:
                    GiveItem(itemId, itemCount);
                    break;

                case DialogueAction.RemoveItem:
                    RemoveItem(itemId, itemCount);
                    break;

                case DialogueAction.OpenShopBuy:
                    OpenShop(ShopMode.Buy);
                    break;

                case DialogueAction.OpenShopSell:
                    OpenShop(ShopMode.Sell);
                    break;

                case DialogueAction.EndTrade:
                    if (ChoiceManager.Instance != null)
                    {
                        ChoiceManager.Instance.OnEndTrade();
                    }
                    break;

                default:
                    break;
            }
        }

        private static void OpenShop(ShopMode mode)
        {
            if (ShopUI.Instance == null)
            {
                Debug.LogError("[DialogueInstructionExecutor] ShopUI 引用为空！请检查场景中是否有 ShopUI 组件。");
                return;
            }
            ShopUI.Instance.OpenWithMode(mode);
        }

        private static void GiveItem(string itemId, int count)
        {
            if (string.IsNullOrEmpty(itemId)) return;

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
            Debug.Log($"[DialogueInstructionExecutor] GiveItem -> 发布 ItemAddedEvent: {itemId} x{count}");
        }

        private static void RemoveItem(string itemId, int count)
        {
            if (string.IsNullOrEmpty(itemId)) return;

            GameRuntimeData data = GameRoot.Instance?.Context?.Data;
            if (data == null || data.Inventory == null) return;

            ItemEntry entry = data.Inventory.items.Find(i => i != null && i.itemId == itemId);
            if (entry == null)
            {
                Debug.LogWarning($"[DialogueInstructionExecutor] 背包中没有 {itemId}，无法扣除。");
                return;
            }

            entry.count -= count;
            if (entry.count <= 0)
            {
                data.Inventory.items.Remove(entry);
            }

            Debug.Log($"[DialogueInstructionExecutor] RemoveItem -> 扣除 {itemId} x{count}，剩余 {entry.count}");
        }
    }
}
