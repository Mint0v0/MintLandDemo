using UnityEngine;
using UnityEditor;

namespace MintLandDemo.EditorTools
{
    public static class FixFencesInScene
    {
        [MenuItem("MintLand/修复场景里的围栏（改 Shader + 加 BoxCollider）")]
        public static void FixFences()
        {
            // 在场景里找所有名字包含 Fence 的对象
            var all = Object.FindObjectsOfType<GameObject>();
            int fixedCount = 0;

            Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");

            foreach (var go in all)
            {
                if (!go.name.Contains("Fence") && !go.name.Contains("FENCE"))
                    continue;

                // 只看有 MeshRenderer 的（叶子节点，实际渲染的物体）
                var meshRenderer = go.GetComponent<MeshRenderer>();
                if (meshRenderer == null) continue;

                // --- 修 Shader ---
                if (meshRenderer.sharedMaterial != null)
                {
                    var mat = meshRenderer.sharedMaterial;
                    if (mat.shader != null && mat.shader.name.StartsWith("Polytope Studio/"))
                    {
                        mat.shader = urpLit;
                        EditorUtility.SetDirty(mat);
                    }
                }

                // --- 删掉丢失的 Mesh Collider，加 BoxCollider ---
                var meshCollider = go.GetComponent<MeshCollider>();
                if (meshCollider != null)
                {
                    Object.DestroyImmediate(meshCollider, true);
                }

                // 如果已有 BoxCollider，跳过
                if (go.GetComponent<BoxCollider>() == null)
                {
                    var box = go.AddComponent<BoxCollider>();
                    // 根据 Mesh 的包围盒自动算尺寸
                    var mf = go.GetComponent<MeshFilter>();
                    if (mf != null && mf.sharedMesh != null)
                    {
                        box.center = mf.sharedMesh.bounds.center;
                        box.size = mf.sharedMesh.bounds.size;
                    }
                    else
                    {
                        // 兜底：给一个典型围栏段的尺寸
                        box.center = new Vector3(0f, 0.75f, 0f);
                        box.size = new Vector3(2.0f, 1.5f, 0.3f);
                    }
                }

                fixedCount++;
            }

            EditorUtility.SetDirty(GameRootOrAnyScene());

            EditorUtility.DisplayDialog("完成",
                $"场景里修复了 {fixedCount} 个围栏对象。\n\n" +
                "下一步：\n" +
                "1. 保存场景（Ctrl+S）\n" +
                "2. 重新 Bake NavMesh",
                "OK");
        }

        private static GameObject GameRootOrAnyScene()
        {
            // 只是为了 EditorUtility.SetDirty 能用，随便返回一个场景里的根物体
            var roots = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
            return roots.Length > 0 ? roots[0] : null;
        }
    }
}