using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using MintLandDemo.Core.Event;
using MintLandDemo.Core.Game;
using MintLandDemo.Infrastructure.Save;

namespace MintLandDemo.Infrastructure.Scene
{
    /// <summary>
    /// 场景服务。统一场景切换入口：切换前自动存档，切换后发布 SceneChangedEvent。
    /// 挂载在 GameRoot 所在对象（随 DontDestroyOnLoad 持久存在）。
    /// V1 以同步加载为主，异步加载作为接口扩展。
    /// </summary>
    public class SceneService : MonoBehaviour, ISceneService
    {
        private JsonSaveService _saveService;

        private void Awake()
        {
            _saveService = GetComponent<JsonSaveService>();
        }

        public void LoadScene(string sceneName)
        {
            LoadSceneInternal(sceneName, null);
        }

        public void LoadSceneAsync(string sceneName, Action onComplete = null)
        {
            LoadSceneInternal(sceneName, onComplete);
        }

        public string GetCurrentSceneName()
        {
            return SceneManager.GetActiveScene().name;
        }

        private void LoadSceneInternal(string sceneName, Action onComplete)
        {
            if (string.IsNullOrEmpty(sceneName)) return;

            string fromScene = SceneManager.GetActiveScene().name;

            // 1. 记录目标场景（供存档恢复使用）。
            GameRuntimeData data = GameRoot.Instance?.Context?.Data;
            if (data != null)
            {
                data.currentScene = sceneName;
            }

            // 2. 切换前自动存档。
            if (_saveService != null && data != null)
            {
                _saveService.Save(data);
            }

            // 3. 切换场景（同步 / 异步）。
            if (onComplete != null)
            {
                StartCoroutine(LoadAsync(sceneName, fromScene, onComplete));
            }
            else
            {
                SceneManager.LoadScene(sceneName);
                EventBus.Publish(new SceneChangedEvent { fromScene = fromScene, toScene = sceneName });
                Debug.Log($"[SceneService] 切换到 {sceneName}");
            }
        }

        private IEnumerator LoadAsync(string sceneName, string fromScene, Action onComplete)
        {
            AsyncOperation op = SceneManager.LoadSceneAsync(sceneName);
            if (op != null)
            {
                while (!op.isDone)
                {
                    yield return null;
                }
            }

            EventBus.Publish(new SceneChangedEvent { fromScene = fromScene, toScene = sceneName });
            Debug.Log($"[SceneService] 切换到 {sceneName}");
            onComplete?.Invoke();
        }
    }
}
