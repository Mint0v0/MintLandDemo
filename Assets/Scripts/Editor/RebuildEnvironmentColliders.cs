using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace MintLandDemo.EditorTools
{
    /// <summary>
    /// 重建环境碰撞体：
    /// 1) 清除所有环境 Prefab 的旧 Collider（之前加的太大）。
    /// 2) 只给 树 / 石头 / 围栏 三类加碰撞体。
    /// 3) 树的 Collider 只覆盖树干（总高的 50%，半径 25%），避免 NavMesh 被挖大洞。
    /// </summary>
    public static class RebuildEnvironmentColliders
    {
        // 要清除 Collider 的所有环境 Prefab 文件夹
        private static readonly string[] ClearFolders = new[]
        {
            "Assets/Polytope Studio/Lowpoly_Environments/Prefabs/Trees",
            "Assets/Polytope Studio/Lowpoly_Environments/Prefabs/Rocks",
            "Assets/Polytope Studio/Lowpoly_Environments/Prefabs/Mushrooms",
            "Assets/Polytope Studio/Lowpoly_Environments/Prefabs/Shrubs",
            "Assets/Polytope Studio/Lowpoly_Environments/Prefabs/Flowers",
            "Assets/Polytope Studio/Lowpoly_Environments/Prefabs/Plants",
            "Assets/Polytope Studio/Lowpoly_Village/Prefabs/Modular/Fence",
        };

        // 树（细 Capsule，只包树干）
        private static readonly string[] TreeFolders = new[]
        {
            "Assets/Polytope Studio/Lowpoly_Environments/Prefabs/Trees",
        };

        // 石头、围栏（Box，稍缩小）
        private static readonly string[] BoxFolders = new[]
        {
            "Assets/Polytope Studio/Lowpoly_Environments/Prefabs/Rocks",
            "Assets/Polytope Studio/Lowpoly_Village/Prefabs/Modular/Fence",
        };

        [MenuItem("MintLand/重建环境碰撞体（小尺寸版）")]
        public static void Rebuild()
        {
            int cleared = 0, rebuilt = 0;

            // --- 第 1 步：清除所有旧 Collider ---
            foreach (var folder in ClearFolders)
            {
                if (!AssetDatabase.IsValidFolder(folder)) continue;

                foreach (var guid in AssetDatabase.FindAssets("t:Prefab", new[] { folder }))
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    var root = PrefabUtility.LoadPrefabContents(path);
                    try
                    {
                        var colliders = root.GetComponents<Collider>();
                        if (colliders.Length > 0)
                        {
                            foreach (var c in colliders)
                                Object.DestroyImmediate(c, true);
                            PrefabUtility.SaveAsPrefabAsset(root, path);
                            cleared++;
                        }
                    }
                    finally
                    {
                        PrefabUtility.UnloadPrefabContents(root);
                    }
                }
            }

            // --- 第 2 步：给树加小 Capsule ---
            foreach (var folder in TreeFolders)
            {
                if (!AssetDatabase.IsValidFolder(folder)) continue;

                foreach (var guid in AssetDatabase.FindAssets("t:Prefab", new[] { folder }))
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    var root = PrefabUtility.LoadPrefabContents(path);
                    try
                    {
                        Bounds? bounds = CalculateBounds(root);
                        if (bounds == null) continue;

                        var b = bounds.Value;
                        var capsule = root.AddComponent<CapsuleCollider>();
                        capsule.direction = 1; // Y 轴
                        capsule.radius = Mathf.Min(b.size.x, b.size.z) * 0.12f;
                        capsule.height = b.size.y * 0.5f;
                        capsule.center = new Vector3(
                            b.center.x,
                            b.min.y + capsule.height * 0.35f,
                            b.center.z);

                        PrefabUtility.SaveAsPrefabAsset(root, path);
                        rebuilt++;
                    }
                    finally
                    {
                        PrefabUtility.UnloadPrefabContents(root);
                    }
                }
            }

            // --- 第 3 步：给石头和围栏加 Box ---
            foreach (var folder in BoxFolders)
            {
                if (!AssetDatabase.IsValidFolder(folder)) continue;

                foreach (var guid in AssetDatabase.FindAssets("t:Prefab", new[] { folder }))
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    var root = PrefabUtility.LoadPrefabContents(path);
                    try
                    {
                        Bounds? bounds = CalculateBounds(root);
                        if (bounds == null) continue;

                        var b = bounds.Value;
                        var box = root.AddComponent<BoxCollider>();
                        box.center = b.center;
                        box.size = new Vector3(
                            b.size.x * 0.9f,
                            b.size.y * 0.95f,
                            b.size.z * 0.9f);

                        PrefabUtility.SaveAsPrefabAsset(root, path);
                        rebuilt++;
                    }
                    finally
                    {
                        PrefabUtility.UnloadPrefabContents(root);
                    }
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog("完成",
                $"清除 {cleared} 个 Prefab 的旧 Collider\n" +
                $"重建 {rebuilt} 个（树=细 Capsule，石头/围栏=Box）\n\n" +
                "下一步：重新 Bake NavMesh。",
                "OK");
            Debug.Log($"[RebuildEnvColliders] 清除 {cleared}，重建 {rebuilt}");
        }

        private static Bounds? CalculateBounds(GameObject root)
        {
            var renderers = root.GetComponentsInChildren<MeshRenderer>();
            if (renderers.Length == 0) return null;

            Bounds b = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
                b.Encapsulate(renderers[i].bounds);

            Matrix4x4 worldToLocal = root.transform.worldToLocalMatrix;
            Vector3 localCenter = worldToLocal.MultiplyPoint3x4(b.center);

            return new Bounds(localCenter, b.size);
        }
    }
}