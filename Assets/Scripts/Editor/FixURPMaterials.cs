using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace MintLandDemo.EditorTools
{
    /// <summary>
    /// 把 Polytope Studio 自定义 Shader 的材质替换为 URP/Lit，并保留主贴图/法线/颜色。
    /// </summary>
    public static class FixURPMaterials
    {
        private const string TargetFolder = "Assets/Polytope Studio";

        [MenuItem("MintLand/修复 Polytope Studio 材质为 URP/Lit")]
        public static void FixPolytopeMaterials()
        {
            string[] guids = AssetDatabase.FindAssets("t:Material", new[] { TargetFolder });
            if (guids.Length == 0)
            {
                EditorUtility.DisplayDialog("没有材质", "未找到任何材质。", "OK");
                return;
            }

            Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");
            if (urpLit == null)
            {
                EditorUtility.DisplayDialog("缺少 URP", "找不到 URP/Lit Shader。", "OK");
                return;
            }

            int totalCount = 0;
            int fixedCount = 0;
            int skippedVariant = 0;
            var fixedList = new List<string>();

            try
            {
                for (int i = 0; i < guids.Length; i++)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                    EditorUtility.DisplayProgressBar("修复材质",
                        $"正在处理: {path}", (float)i / guids.Length);

                    var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
                    if (mat == null) continue;
                    totalCount++;

                    string shaderName = mat.shader != null ? mat.shader.name : "<null>";

                    // 只处理 Polytope Studio 自己写的 Shader
                    bool needFix = shaderName.StartsWith("Polytope Studio/");
                    if (!needFix) continue;

                    // 1) 抓取贴图/颜色
                    Texture baseTex = GetTexture(mat, new[] { "_MainTex", "_BaseMap", "_BaseColorMap", "_AlbedoMap" });
                    Texture normalTex = GetTexture(mat, new[] { "_BumpMap", "_NormalMap" });
                    Color baseColor = GetColor(mat, new[] { "_Color", "_BaseColor", "_Tint" }, Color.white);
                    float cutoff = GetFloat(mat, new[] { "_Cutoff", "_AlphaCutoff" }, 0.5f);
                    bool hasAlphaClip = HasProperty(mat, "_Cutoff") || HasProperty(mat, "_AlphaClip");
                    bool isVegetation = shaderName.Contains("Vegetation") 
                                    || shaderName.Contains("Foliage") 
                                    || shaderName.Contains("Plants") 
                                    || shaderName.Contains("Flowers");

                    // 2) 如果是 Material Variant，先解除继承（关键修复）
                    if (mat.parent != null)
                    {
                        mat.parent = null;
                        EditorUtility.SetDirty(mat);
                    }

                    // 3) 换 Shader
                    try
                    {
                        mat.shader = urpLit;
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogWarning($"[FixURPMaterials] 无法替换 Shader: {path}\n{e.Message}");
                        skippedVariant++;
                        continue;
                    }

                    // 4) 恢复贴图
                    if (baseTex != null)
                    {
                        if (mat.HasProperty("_BaseMap"))
                            mat.SetTexture("_BaseMap", baseTex);
                        if (mat.HasProperty("_MainTex"))
                            mat.SetTexture("_MainTex", baseTex);
                    }

                    if (normalTex != null && mat.HasProperty("_BumpMap"))
                        mat.SetTexture("_BumpMap", normalTex);

                    // 5) 颜色
                    if (mat.HasProperty("_BaseColor"))
                        mat.SetColor("_BaseColor", baseColor);
                    if (mat.HasProperty("_Color"))
                        mat.SetColor("_Color", baseColor);

                    // 6) 植被 / 花：开启 Alpha Clipping
                    if (isVegetation || hasAlphaClip)
                    {
                        mat.SetFloat("_AlphaClip", 1f);
                        if (mat.HasProperty("_Cutoff"))
                            mat.SetFloat("_Cutoff", cutoff);
                        mat.EnableKeyword("_ALPHATEST_ON");
                        mat.SetFloat("_Surface", 0f);
                        mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.AlphaTest;
                    }
                    else
                    {
                        mat.SetFloat("_Surface", 0f);
                        mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Geometry;
                    }

                    // 7) 低多边形风格
                    mat.SetFloat("_Smoothness", 0f);
                    if (mat.HasProperty("_Metallic"))
                        mat.SetFloat("_Metallic", 0f);

                    EditorUtility.SetDirty(mat);
                    fixedCount++;
                    fixedList.Add(System.IO.Path.GetFileName(path));
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            string summary = $"共扫描 {totalCount} 个材质，修复 {fixedCount} 个。";
            if (skippedVariant > 0)
                summary += $"\n跳过 {skippedVariant} 个（无法替换）。";
            if (fixedList.Count > 0)
            {
                summary += "\n\n修复列表:\n";
                foreach (var n in fixedList) summary += "  " + n + "\n";
            }

            EditorUtility.DisplayDialog("修复完成", summary, "OK");
            Debug.Log($"[FixURPMaterials] {summary}");
        }

        // ----------------- 小工具 -----------------

        private static Texture GetTexture(Material m, string[] names)
        {
            foreach (var n in names)
                if (m.HasProperty(n) && m.GetTexture(n) != null) return m.GetTexture(n);
            return null;
        }

        private static Color GetColor(Material m, string[] names, Color fallback)
        {
            foreach (var n in names)
                if (m.HasProperty(n)) return m.GetColor(n);
            return fallback;
        }

        private static float GetFloat(Material m, string[] names, float fallback)
        {
            foreach (var n in names)
                if (m.HasProperty(n)) return m.GetFloat(n);
            return fallback;
        }

        private static bool HasProperty(Material m, string name)
        {
            return m.HasProperty(name);
        }
    }
}