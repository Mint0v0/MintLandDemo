namespace MintLandDemo.Gameplay.Inventory
{
    /// <summary>
    /// 物品获得事件（EventBus 事件，需为 struct）。
    /// </summary>
    public struct ItemAddedEvent
    {
        public string ItemId;
        public int Count;
    }

    /// <summary>
    /// 金币变更事件（购买/出售时发布）。
    /// </summary>
    public struct GoldChangedEvent
    {
        public int Gold;
    }
}
