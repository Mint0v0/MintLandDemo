using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace MintLandDemo.EditorTools
{
    public static class DiagnoseMaterials
    {
        [MenuItem("MintLand/诊断 Polytope Studio 材质 Shader")]
        public static void Diagnose()
        {
            string[] guids = AssetDatabase.FindAssets("t:Material",
                new[] { "Assets/Polytope Studio" });

            var shaderGroups = new Dictionary<string, List<string>>();

            foreach (var guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (mat == null) continue;

                string shaderName = mat.shader != null ? mat.shader.name : "<null>";
                if (!shaderGroups.ContainsKey(shaderName))
                    shaderGroups[shaderName] = new List<string>();
                shaderGroups[shaderName].Add(System.IO.Path.GetFileName(path));
            }

            Debug.Log($"===== 共 {guids.Length} 个材质 =====");
            foreach (var kvp in shaderGroups)
            {
                Debug.Log($"【Shader: {kvp.Key}】 数量: {kvp.Value.Count}");
                foreach (var m in kvp.Value)
                    Debug.Log($"    - {m}");
            }
        }
    }
}