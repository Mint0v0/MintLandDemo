using UnityEngine;
using UnityEditor;

namespace MintLandDemo.EditorTools
{
    /// <summary>
    /// 移除装饰性 Prefab（草、花、蘑菇、小灌木）上的碰撞体。
    /// 只保留 Trees 和 Rocks 的 Collider。
    /// </summary>
    public static class RemoveUnwantedColliders
    {
        // 这些文件夹下的 Prefab 不应该有碰撞体
        private static readonly string[] ExcludeFolders = new[]
        {
            "Assets/Polytope Studio/Lowpoly_Environments/Prefabs/Mushrooms",
            "Assets/Polytope Studio/Lowpoly_Environments/Prefabs/Shrubs",
            "Assets/Polytope Studio/Lowpoly_Environments/Prefabs/Flowers",
            "Assets/Polytope Studio/Lowpoly_Environments/Prefabs/Plants",
        };

        [MenuItem("MintLand/移除装饰 Prefab 的碰撞体")]
        public static void RemoveColliders()
        {
            int total = 0, removed = 0;

            foreach (var folder in ExcludeFolders)
            {
                if (!AssetDatabase.IsValidFolder(folder)) continue;

                string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { folder });
                foreach (var guid in guids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                    if (prefab == null) continue;

                    total++;

                    var prefabRoot = PrefabUtility.LoadPrefabContents(path);
                    try
                    {
                        var colliders = prefabRoot.GetComponents<Collider>();
                        if (colliders.Length == 0) continue;

                        foreach (var c in colliders)
                            Object.DestroyImmediate(c, true);

                        PrefabUtility.SaveAsPrefabAsset(prefabRoot, path);
                        removed++;
                    }
                    finally
                    {
                        PrefabUtility.UnloadPrefabContents(prefabRoot);
                    }
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog("完成",
                $"扫描 {total} 个装饰 Prefab，移除 {removed} 个的碰撞体。",
                "OK");
            Debug.Log($"[RemoveUnwantedColliders] 扫描 {total}，移除 {removed}");
        }
    }
}