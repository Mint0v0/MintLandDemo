#if UNITY_EDITOR || DEVELOPMENT_BUILD
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using MintLandDemo.Controller.Player;
using MintLandDemo.Core.Game;
using MintLandDemo.Gameplay.Inventory;
using MintLandDemo.Gameplay.Equipment;
using MintLandDemo.Infrastructure.Scene;

namespace MintLandDemo.DebugTools
{
    /// <summary>
    /// 开发者调试台（仅 Editor / Development Build 编译，Release 不包含）。
    /// F1 加金币、F2 加史莱姆粘液、F3 切换场景（新手村 ↔ 森林）、F5 注入调试宝石。
    /// OnGUI 显示：当前场景名、任务进度、金币、玩家坐标。
    /// 需挂载在 GameRoot 所在对象（DontDestroyOnLoad），跨场景存活以支持 F3 往返。
    /// </summary>
    public class DeveloperConsole : MonoBehaviour
    {
        [SerializeField] private int addGoldAmount = 500;
        [SerializeField] private int addMucusAmount = 10;
        [SerializeField] private int addGemAmount = 1;
        [SerializeField] private GemConfig[] debugGems; // 调试用宝石（F5 注入背包）

        private const string VillageScene = "01_NewbieVillage";
        private const string ForestScene = "02_Forest";

        private void Update()
        {
            if (Keyboard.current == null) return;

            GameRuntimeData data = GameRoot.Instance?.Context?.Data;
            if (data == null || data.Inventory == null) return;

            if (Keyboard.current.f1Key.wasPressedThisFrame)
            {
                data.Inventory.gold += addGoldAmount;
                Debug.Log($"[DeveloperConsole] 金币 +{addGoldAmount}，当前 {data.Inventory.gold}");
            }

            if (Keyboard.current.f2Key.wasPressedThisFrame)
            {
                InventorySystem.AddItem("slime_mucus", addMucusAmount);
            }

            if (Keyboard.current.f3Key.wasPressedThisFrame)
            {
                ToggleScene();
            }

            if (Keyboard.current.f5Key.wasPressedThisFrame)
            {
                InjectDebugGems();
            }
        }

        private void InjectDebugGems()
        {
            if (debugGems == null || debugGems.Length == 0)
            {
                Debug.LogWarning("[DeveloperConsole] 未配置调试宝石（debugGems），F5 无法注入。");
                return;
            }

            foreach (GemConfig gem in debugGems)
            {
                if (gem == null) continue;
                InventorySystem.AddItem(gem.gemName, addGemAmount);
            }
            Debug.Log($"[DeveloperConsole] 已注入 {debugGems.Length} 种宝石（每种 x{addGemAmount}）。");
        }

        private void ToggleScene()
        {
            ISceneService sceneService = GameRoot.Instance?.SceneService;
            if (sceneService == null)
            {
                Debug.LogWarning("[DeveloperConsole] 未找到 SceneService，无法切换场景。");
                return;
            }

            string current = sceneService.GetCurrentSceneName();
            string target = current == ForestScene ? VillageScene : ForestScene;
            sceneService.LoadScene(target);
        }

        private void OnGUI()
        {
            GameRuntimeData data = GameRoot.Instance?.Context?.Data;
            if (data == null) return;

            string sceneName = SceneManager.GetActiveScene().name;

            string questText = "无任务";
            QuestRuntime quest = data.activeQuest;
            if (quest != null)
            {
                questText = $"{quest.questName} {quest.currentProgress}/{quest.targetProgress}";
            }

            string posText = "无玩家";
            PlayerController player = FindObjectOfType<PlayerController>();
            if (player != null)
            {
                Vector3 p = player.transform.position;
                posText = $"({p.x:F1}, {p.y:F1}, {p.z:F1})";
            }

            string content = $"场景: {sceneName}\n任务: {questText}\n金币: {data.Inventory.gold}\n坐标: {posText}";

            GUI.Box(new Rect(10, 10, 260, 120), "MintLand Debug");
            GUI.Label(new Rect(20, 32, 240, 96), content);
        }
    }
}
#endif
