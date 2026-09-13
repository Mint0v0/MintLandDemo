namespace MintLandDemo.Core.Game
{
    /// <summary>
    /// 跨场景运行时数据容器。
    /// 纯 C# 类：不继承 MonoBehaviour。
    /// 由 GameRoot 在 Awake 中创建并持有，随 DontDestroyOnLoad 的 GameRoot 持久存在。
    /// </summary>
    public class GameContext
    {
        public GameRuntimeData Data { get; set; }

        public GameContext()
        {
            Data = new GameRuntimeData();
        }
    }
}
