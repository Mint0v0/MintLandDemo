using UnityEngine;
using UnityEngine.UI;
using MintLandDemo.Controller.Player;
using MintLandDemo.Core.Event;

namespace MintLandDemo.UI.Common
{
    /// <summary>
    /// 2D 俯视小地图（屏幕右上角）。方案 A：俯视相机渲染到 RenderTexture，RawImage 显示。
    /// 相机跟随玩家（X/Z 对齐、Y 固定高度），玩家图标经相机投影映射到地图 UI；
    /// 场景切换时订阅 SceneChangedEvent 重新定位相机与玩家引用。
    /// </summary>
    public class MiniMapUI : MonoBehaviour
    {
        [Header("地图渲染")]
        [SerializeField] private Camera miniMapCamera;      // 俯视相机（留空则按名称 "MiniMapCamera" 查找）
        [SerializeField] private RenderTexture miniMapRT;   // 渲染纹理
        [SerializeField] private RawImage miniMapImage;     // UI 显示

        [Header("玩家标注")]
        [SerializeField] private RectTransform mapRect;     // 地图 Rect（RawImage 的 RectTransform）
        [SerializeField] private RectTransform playerIcon;  // 小圆点（应为 mapRect 的子物体）

        [Header("地图参数")]
        [SerializeField] private float mapSize = 50f;       // 地图显示的世界范围（米，= 相机正交高度 ×2）
        [SerializeField] private float cameraHeight = 30f;  // 俯视相机离地高度

        [Header("玩家")]
        [SerializeField] private Transform player;          // 可留空，自动查找 PlayerController

        private void OnEnable()
        {
            EventBus.Subscribe<SceneChangedEvent>(OnSceneChanged);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<SceneChangedEvent>(OnSceneChanged);
        }

        private void Start()
        {
            ResolveCamera();
            ResolvePlayer();
            SetupCamera();
            FollowPlayer();

            // 自动接线：RT → 相机输出 + RawImage 显示，减少手动配置错误。
            if (miniMapCamera != null && miniMapRT != null) miniMapCamera.targetTexture = miniMapRT;
            if (miniMapImage != null && miniMapRT != null) miniMapImage.texture = miniMapRT;

            // 图标居中锚点 + 居中 pivot，保证 anchoredPosition 相对地图中心。
            if (playerIcon != null)
            {
                playerIcon.anchorMin = new Vector2(0.5f, 0.5f);
                playerIcon.anchorMax = new Vector2(0.5f, 0.5f);
                playerIcon.pivot = new Vector2(0.5f, 0.5f);
            }

            // 小地图不拦截点击，避免挡住其他 UI。
            if (miniMapImage != null) miniMapImage.raycastTarget = false;

            // 保证小地图绘制在其他面板之上（HUD）。
            if (transform is RectTransform rt) rt.SetAsLastSibling();
        }

        private void Update()
        {
            if (player == null)
            {
                ResolvePlayer();
                return;
            }

            FollowPlayer();
            UpdatePlayerIconPosition();
        }

        /// <summary>场景切换后：重新查找新场景的相机与玩家，并吸附相机。</summary>
        private void OnSceneChanged(SceneChangedEvent evt)
        {
            ResolveCamera();
            ResolvePlayer();
            SetupCamera();
            FollowPlayer();
        }

        private void ResolveCamera()
        {
            if (miniMapCamera != null) return;
            GameObject go = GameObject.Find("MiniMapCamera");
            if (go != null) miniMapCamera = go.GetComponent<Camera>();
        }

        private void ResolvePlayer()
        {
            if (player != null) return;
            PlayerController pc = FindObjectOfType<PlayerController>();
            if (pc != null) player = pc.transform;
        }

        private void SetupCamera()
        {
            if (miniMapCamera == null) return;
            miniMapCamera.orthographic = true;
            miniMapCamera.orthographicSize = mapSize / 2f;
        }

        /// <summary>相机跟随玩家（X/Z 对齐、Y 固定高度）。</summary>
        private void FollowPlayer()
        {
            if (miniMapCamera == null || player == null) return;
            Vector3 p = player.position;
            miniMapCamera.transform.position = new Vector3(p.x, cameraHeight, p.z);
        }

        /// <summary>将玩家世界坐标经相机投影映射到地图 UI（相机跟随 → 图标恒在地图中心）。</summary>
        private void UpdatePlayerIconPosition()
        {
            if (miniMapCamera == null || player == null || playerIcon == null || mapRect == null) return;

            Vector3 viewport = miniMapCamera.WorldToViewportPoint(player.position);

            // viewport (0..1) → 以地图中心为原点的 -1..1。
            float u = (viewport.x - 0.5f) * 2f;
            float v = (viewport.y - 0.5f) * 2f;

            // 换算为图标相对 mapRect 中心的像素偏移，并夹在地图范围内。
            Vector2 half = mapRect.rect.size * 0.5f;
            Vector2 offset = new Vector2(u * half.x, v * half.y);
            offset = Vector2.Max(-half, Vector2.Min(half, offset));

            playerIcon.anchoredPosition = offset;
        }
    }
}
