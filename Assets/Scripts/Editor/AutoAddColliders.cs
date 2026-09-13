using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;

namespace MintLandDemo.EditorTools
{
    /// <summary>
    /// 批量为指定文件夹下的 Prefab 添加碰撞体。
    /// 根据 Mesh 的包围盒自动计算 Collider 大小。
    /// </summary>
    public static class AutoAddColliders
    {
        // 要处理的文件夹（你可以按需要改）
        private static readonly string[] TargetFolders = new[]
        {
            "Assets/Polytope Studio/Lowpoly_Environments/Prefabs/Trees",
            "Assets/Polytope Studio/Lowpoly_Environments/Prefabs/Rocks",
            "Assets/Polytope Studio/Lowpoly_Environments/Prefabs/Mushrooms",
            "Assets/Polytope Studio/Lowpoly_Environments/Prefabs/Shrubs",
            // 不加 Flowers 和 Plants（草和花不需要碰撞体）
        };

        [MenuItem("MintLand/批量给环境 Prefab 加碰撞体")]
        public static void AddColliders()
        {
            int total = 0, added = 0, skipped = 0;

            foreach (var folder in TargetFolders)
            {
                if (!AssetDatabase.IsValidFolder(folder))
                {
                    Debug.LogWarning($"[AutoAddColliders] 跳过不存在的文件夹: {folder}");
                    continue;
                }

                string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { folder });
                foreach (var guid in guids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                    if (prefab == null) continue;

                    total++;

                    // 已经加了 Collider 就跳过
                    if (prefab.GetComponent<Collider>() != null)
                    {
                        skipped++;
                        continue;
                    }

                    try
                    {
                        AddColliderToPrefab(prefab, path);
                        added++;
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogWarning($"[AutoAddColliders] 处理失败: {path}\n{e.Message}");
                    }
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog(
                "批量加碰撞体完成",
                $"共 {total} 个 Prefab\n成功添加: {added}\n跳过（已有 Collider）: {skipped}",
                "OK");
            Debug.Log($"[AutoAddColliders] 共 {total}，添加 {added}，跳过 {skipped}");
        }

        private static void AddColliderToPrefab(GameObject prefab, string path)
        {
            // 打开 Prefab 进行编辑
            var prefabRoot = PrefabUtility.LoadPrefabContents(path);

            try
            {
                // 计算所有 Mesh 的包围盒
                Bounds? bounds = CalculateBounds(prefabRoot);
                if (bounds == null)
                {
                    Debug.LogWarning($"[AutoAddColliders] {path} 里没有找到 Mesh，跳过");
                    return;
                }

                var b = bounds.Value;

                // 根据形状选 Collider 类型：
                // - 细长物体（树、灌木）用 Capsule
                // - 宽扁物体（石头、蘑菇）用 Box
                bool isTall = b.size.y > b.size.x * 1.5f && b.size.y > b.size.z * 1.5f;

                if (isTall)
                {
                    var capsule = prefabRoot.AddComponent<CapsuleCollider>();
                    capsule.center = b.center;
                    capsule.height = b.size.y;
                    capsule.radius = Mathf.Min(b.size.x, b.size.z) * 0.5f;
                    capsule.direction = 1; // Y 轴
                }
                else
                {
                    var box = prefabRoot.AddComponent<BoxCollider>();
                    box.center = b.center;
                    box.size = b.size;
                }

                PrefabUtility.SaveAsPrefabAsset(prefabRoot, path);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefabRoot);
            }
        }

        private static Bounds? CalculateBounds(GameObject root)
        {
            var renderers = root.GetComponentsInChildren<MeshRenderer>();
            if (renderers.Length == 0) return null;

            Bounds b = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
                b.Encapsulate(renderers[i].bounds);

            // 转到本地坐标
            Matrix4x4 worldToLocal = root.transform.worldToLocalMatrix;
            Vector3 localCenter = worldToLocal.MultiplyPoint3x4(b.center);

            Bounds localBounds = new Bounds(localCenter, b.size);
            return localBounds;
        }
    }
}