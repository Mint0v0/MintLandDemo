using UnityEngine;
using UnityEditor;

namespace MintLandDemo.EditorTools
{
    /// <summary>
    /// 移除环境 Prefab（树、石头、蘑菇、灌木）的碰撞体。
    /// 目的：让 NavMesh 烘焙不再被这些物体挖洞。
    /// 代价：玩家会穿树。对 Demo 可接受。
    /// </summary>
    public static class RemoveEnvironmentColliders
    {
        private static readonly string[] Folders = new[]
        {
            "Assets/Polytope Studio/Lowpoly_Environments/Prefabs/Trees",
            "Assets/Polytope Studio/Lowpoly_Environments/Prefabs/Rocks",
            "Assets/Polytope Studio/Lowpoly_Environments/Prefabs/Mushrooms",
            "Assets/Polytope Studio/Lowpoly_Environments/Prefabs/Shrubs",
        };

        [MenuItem("MintLand/移除环境 Prefab 的碰撞体（修 NavMesh 洞）")]
        public static void RemoveAll()
        {
            int total = 0, removed = 0;

            foreach (var folder in Folders)
            {
                if (!AssetDatabase.IsValidFolder(folder)) continue;

                foreach (var guid in AssetDatabase.FindAssets("t:Prefab", new[] { folder }))
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                    if (prefab == null) continue;

                    total++;
                    var root = PrefabUtility.LoadPrefabContents(path);
                    try
                    {
                        var colliders = root.GetComponents<Collider>();
                        if (colliders.Length == 0) continue;

                        foreach (var c in colliders)
                            Object.DestroyImmediate(c, true);

                        PrefabUtility.SaveAsPrefabAsset(root, path);
                        removed++;
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
                $"扫描 {total} 个 Prefab，移除 {removed} 个的碰撞体。\n\n" +
                "重新 Bake NavMesh 后，地形会完整覆盖。",
                "OK");
        }
    }
}