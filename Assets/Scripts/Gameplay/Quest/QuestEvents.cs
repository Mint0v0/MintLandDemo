namespace MintLandDemo.Gameplay.Quest
{
    /// <summary>
    /// 任务已接取事件（EventBus 事件，需为 struct）。
    /// </summary>
    public struct QuestAcceptedEvent
    {
        public string QuestId;
        public string QuestName;
    }

    /// <summary>
    /// 任务已完成事件（EventBus 事件，需为 struct）。
    /// </summary>
    public struct QuestCompletedEvent
    {
        public string QuestId;
        public string QuestName;
    }
}
