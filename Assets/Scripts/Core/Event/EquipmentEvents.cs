namespace MintLandDemo.Core.Event
{
    /// <summary>
    /// 装备变更事件（镶嵌宝石时发布）。AttributeSystem 订阅此事件重算最终属性。
    /// </summary>
    public struct EquipmentChangedEvent
    {
        public string equipmentId; // 装备 ID（如 "sword"）
    }

    /// <summary>
    /// 属性变更事件（重算完成后发布），供后续 UI 刷新（本次可先只打 Log）。
    /// </summary>
    public struct AttributeChangedEvent
    {
        public int finalAttack;
        public int finalDefense;
        public int finalMaxHp;
    }
}
