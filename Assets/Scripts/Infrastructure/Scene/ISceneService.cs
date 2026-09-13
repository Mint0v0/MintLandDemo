using System;

namespace MintLandDemo.Infrastructure.Scene
{
    /// <summary>
    /// 场景服务接口。统一场景切换入口。
    /// </summary>
    public interface ISceneService
    {
        void LoadScene(string sceneName);
        void LoadSceneAsync(string sceneName, Action onComplete = null);
        string GetCurrentSceneName();
    }
}
