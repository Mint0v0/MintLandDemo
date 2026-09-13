using UnityEngine;
using MintLandDemo.Core.Event;
using MintLandDemo.Gameplay.Character;
using MintLandDemo.Infrastructure.Save;
using MintLandDemo.Infrastructure.Scene;

namespace MintLandDemo.Core.Game
{
    /// <summary>
    /// 项目生命周期入口 / Composition Root。
    /// 只负责初始化核心服务与跨场景持久化，不承载具体业务逻辑（不是万能 Manager）。
    /// 单例模式仅限 GameRoot 本身使用，严禁扩散到其他任何 System。
    /// Part 5：装配 Save/Scene 服务，启动时读档并跳转到对应场景。
    /// </summary>
    public class GameRoot : MonoBehaviour
    {
        public static GameRoot Instance { get; private set; }

        private const string DefaultScene = "01_NewbieVillage";

        private GameContext _context;
        public GameContext Context => _context;

        private ISaveService _saveService;
        private ISceneService _sceneService;
        public ISaveService SaveService => _saveService;
        public ISceneService SceneService => _sceneService;

        private void Awake()
        {
            // 防止重复场景加载时出现第二个 GameRoot（DontDestroyOnLoad 单例的兜底）。
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            _context = new GameContext();

            // 服务装配：Save/Scene 服务与 GameRoot 挂载在同一对象，作为 Composition Root 统一持有。
            _saveService = GetComponent<JsonSaveService>();
            _sceneService = GetComponent<SceneService>();

            Debug.Log("[GameRoot] Initialized successfully.");
        }

        private void Start()
        {
            // 装备变更 → 属性重算（Part 4C）。
            EventBus.Subscribe<EquipmentChangedEvent>(AttributeSystem.OnEquipmentChanged);

            // 主线收尾：完成森林 Boss 任务后输出里程碑日志（Part 6）。
            EventBus.Subscribe<DemoCompletedEvent>(OnDemoCompleted);

            // 1. 尝试读档：存在则恢复，否则使用默认初始数据。
            GameRuntimeData data;
            if (_saveService != null && _saveService.TryLoad(out GameRuntimeData loaded))
            {
                _context.Data = loaded;
                data = loaded;
            }
            else
            {
                data = _context.Data;
            }

            // 2. 读档后必须重算最终属性（finalXxx 不持久化，需从装备/宝石重新计算）。
            AttributeSystem.CalculateFinalAttributes(data.Player, data.Equipment);

            // 3. 根据存档中的场景名（或默认新手村）跳转场景。
            string sceneName = string.IsNullOrEmpty(data.currentScene) ? DefaultScene : data.currentScene;
            if (_sceneService != null)
            {
                _sceneService.LoadScene(sceneName);
            }
            else
            {
                Debug.LogWarning("[GameRoot] 未找到 SceneService，跳过场景加载。");
            }
        }

        private static void OnDemoCompleted(DemoCompletedEvent evt)
        {
            Debug.Log("[Demo] 主线流程全部完成！🎉");
        }
    }
}
