using MintLandDemo.Core.Game;

namespace MintLandDemo.Infrastructure.Save
{
    /// <summary>
    /// 存档服务接口。负责 GameRuntimeData 的持久化与读取。
    /// </summary>
    public interface ISaveService
    {
        void Save(GameRuntimeData data);
        bool TryLoad(out GameRuntimeData data);
        bool HasSave();
        void DeleteSave();
    }
}
