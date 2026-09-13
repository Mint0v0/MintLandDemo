namespace MintLandDemo.Core.Event
{
    /// <summary>
    /// 场景切换事件（EventBus 事件，需为 struct）。
    /// </summary>
    public struct SceneChangedEvent
    {
        public string fromScene;
        public string toScene;
    }
}
