namespace MintLandDemo.Core.Event
{
    /// <summary>
    /// 主线 Demo 全流程完成事件（EventBus 事件，需为 struct）。
    /// 由 QuestSystem 在完成「讨伐森林 Boss」任务后发布，GameRoot 订阅并输出里程碑日志。
    /// </summary>
    public struct DemoCompletedEvent
    {
    }
}
