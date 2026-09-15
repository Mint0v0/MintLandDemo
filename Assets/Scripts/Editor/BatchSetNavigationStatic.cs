#pragma warning disable 0618
using UnityEngine;
using UnityEditor;

namespace MintLandDemo.EditorTools
{
    /// <summary>
    /// 批量给选中的物体设置 Navigation Static 标志。
    /// 解决 Unity 2022.3 里 Inspector 的 Navigation Static 项变灰无法点击的问题。
    /// </summary>
    public static class BatchSetNavigationStatic
    {
        [MenuItem("MintLand/批量设置选中物体为 Navigation Static")]
        public static void SetNavStatic()
        {
            var selected = Selection.gameObjects;
            if (selected.Length == 0)
            {
                EditorUtility.DisplayDialog("未选中物体", "请先在 Hierarchy 里选中要设置的物体。", "OK");
                return;
            }

            int count = 0;
            foreach (var go in selected)
            {
                var flags = GameObjectUtility.GetStaticEditorFlags(go);
                flags |= StaticEditorFlags.NavigationStatic;
                GameObjectUtility.SetStaticEditorFlags(go, flags);
                count++;
            }

            Debug.Log($"[BatchSetNavigationStatic] 已设置 {count} 个物体为 Navigation Static");
            EditorUtility.DisplayDialog("完成", $"已设置 {count} 个物体。\n\n下一步：重新 Bake NavMesh。", "OK");
        }

        [MenuItem("MintLand/取消选中物体的 Navigation Static")]
        public static void ClearNavStatic()
        {
            var selected = Selection.gameObjects;
            if (selected.Length == 0) return;

            int count = 0;
            foreach (var go in selected)
            {
                var flags = GameObjectUtility.GetStaticEditorFlags(go);
                flags &= ~StaticEditorFlags.NavigationStatic;
                GameObjectUtility.SetStaticEditorFlags(go, flags);
                count++;
            }

            Debug.Log($"[BatchSetNavigationStatic] 已取消 {count} 个物体的 Navigation Static");
        }
    }
}