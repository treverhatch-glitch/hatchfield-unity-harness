// Switch this project from URP to HDRP at maximum quality (Hatchfield: a Windows game, decided 2026-10-31).
//
// Run once, either way:
//   - In Unity: menu Hatchfield > Switch to HDRP (max quality)
//   - From a terminal: Unity.exe -batchmode -projectPath <this folder> -executeMethod HdrpSetup.Run -quit -logFile -
//
// It creates Assets/Settings/HDRP/HDRP-Max.asset with every high-end feature on, makes it the active pipeline for
// the project and every Quality level, and converts the URP starter materials to HDRP/Lit (or HDRP/Unlit).
// Every step logs "[hdrp] DONE ..." or "[hdrp] SKIPPED ...". A SKIPPED step means this HDRP version names
// that setting differently: open Window > Rendering > HDRP Wizard and press Fix All, then check the HDRP asset
// in the Inspector. The URP files stay in Assets/Settings until a person decides to remove them.
// Ray tracing needs DirectX 12 and an RTX-class card: the setting is on; check it once the PC's GPU is known.
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

public static class HdrpSetup
{
    const string Folder = "Assets/Settings/HDRP";
    const string AssetPath = Folder + "/HDRP-Max.asset";

    [MenuItem("Hatchfield/Switch to HDRP (max quality)")]
    public static void RunFromMenu() => Run();

    public static void Run()
    {
        Directory.CreateDirectory(Folder);
        var asset = AssetDatabase.LoadAssetAtPath<HDRenderPipelineAsset>(AssetPath);
        if (asset == null)
        {
            asset = ScriptableObject.CreateInstance<HDRenderPipelineAsset>();
            AssetDatabase.CreateAsset(asset, AssetPath);
            Debug.Log($"[hdrp] DONE created {AssetPath}");
        }
        else Debug.Log($"[hdrp] DONE found {AssetPath}");

        MaxQuality(asset);

        GraphicsSettings.defaultRenderPipeline = asset;
        Debug.Log("[hdrp] DONE project render pipeline = HDRP-Max");
        int current = QualitySettings.GetQualityLevel();
        for (int i = 0; i < QualitySettings.names.Length; i++)
        {
            QualitySettings.SetQualityLevel(i, false);
            QualitySettings.renderPipeline = asset;
        }
        QualitySettings.SetQualityLevel(current, false);
        Debug.Log($"[hdrp] DONE every Quality level ({string.Join(", ", QualitySettings.names)}) uses HDRP-Max");

        ConvertMaterials();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[hdrp] FINISHED. Next: Window > Rendering > HDRP Wizard, check every row is green (Fix All if not), then press Play.");
    }

    // Each setting is set by its serialized name, so a renamed one is reported, not a compile error.
    static void MaxQuality(HDRenderPipelineAsset asset)
    {
        var so = new SerializedObject(asset);
        var on = new[]
        {
            "supportSSR", "supportSSRTransparent", "supportSSAO", "supportSSGI", "supportSubsurfaceScattering",
            "supportVolumetrics", "increaseResolutionOfVolumetrics", "supportVolumetricClouds", "supportLightLayers",
            "supportMotionVectors", "supportDitheringCrossFade", "supportTerrainHole", "supportDecals",
            "supportDecalLayers", "supportShadowMask", "supportScreenSpaceLensFlare", "supportWater",
            "supportTransparentBackface", "supportTransparentDepthPrepass", "supportTransparentDepthPostpass",
            "supportRayTracing",
        };
        foreach (var name in on) SetBool(so, $"m_RenderPipelineSettings.{name}", true);
        SetInt(so, "m_RenderPipelineSettings.supportedLitShaderMode", 3); // forward and deferred
        SetInt(so, "m_RenderPipelineSettings.colorBufferFormat", 48); // R16G16B16A16 (high dynamic range)
        SetInt(so, "m_RenderPipelineSettings.hdShadowInitParams.maxDirectionalShadowMapResolution", 4096);
        SetInt(so, "m_RenderPipelineSettings.hdShadowInitParams.maxPunctualShadowMapResolution", 4096);
        SetInt(so, "m_RenderPipelineSettings.hdShadowInitParams.maxAreaShadowMapResolution", 4096);
        SetInt(so, "m_RenderPipelineSettings.hdShadowInitParams.punctualLightShadowAtlas.shadowAtlasResolution", 8192);
        SetInt(so, "m_RenderPipelineSettings.hdShadowInitParams.areaLightShadowAtlas.shadowAtlasResolution", 8192);
        SetInt(so, "m_RenderPipelineSettings.hdShadowInitParams.maxShadowRequests", 256);
        SetInt(so, "m_RenderPipelineSettings.lightLoopSettings.maxDirectionalLightsOnScreen", 16);
        SetInt(so, "m_RenderPipelineSettings.lightLoopSettings.maxPunctualLightsOnScreen", 512);
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(asset);
    }

    static void SetBool(SerializedObject so, string path, bool value)
    {
        var p = so.FindProperty(path);
        if (p == null || p.propertyType != SerializedPropertyType.Boolean) { Debug.Log($"[hdrp] SKIPPED {path} (not in this HDRP version)"); return; }
        p.boolValue = value;
        Debug.Log($"[hdrp] DONE {path} = {value}");
    }

    static void SetInt(SerializedObject so, string path, int value)
    {
        var p = so.FindProperty(path);
        if (p == null) { Debug.Log($"[hdrp] SKIPPED {path} (not in this HDRP version)"); return; }
        if (p.propertyType == SerializedPropertyType.Enum) p.enumValueFlag = value;
        else if (p.propertyType == SerializedPropertyType.Integer) p.intValue = value;
        else { Debug.Log($"[hdrp] SKIPPED {path} (type {p.propertyType})"); return; }
        Debug.Log($"[hdrp] DONE {path} = {value}");
    }

    // URP Lit/Simple Lit/Unlit -> HDRP Lit/Unlit, keeping colour, textures, normal map, metal and smoothness.
    static void ConvertMaterials()
    {
        var hdLit = Shader.Find("HDRP/Lit");
        var hdUnlit = Shader.Find("HDRP/Unlit");
        if (hdLit == null || hdUnlit == null) { Debug.Log("[hdrp] SKIPPED materials: HDRP shaders not found (is the package imported?)"); return; }
        int converted = 0;
        foreach (var guid in AssetDatabase.FindAssets("t:Material", new[] { "Assets" }))
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null || m.shader == null || !m.shader.name.StartsWith("Universal Render Pipeline/")) continue;
            bool unlit = m.shader.name.EndsWith("Unlit");
            var keep = new Dictionary<string, object>();
            void Tex(string from) { if (m.HasProperty(from)) keep[from] = m.GetTexture(from); }
            void Col(string from) { if (m.HasProperty(from)) keep[from] = m.GetColor(from); }
            void Num(string from) { if (m.HasProperty(from)) keep[from] = m.GetFloat(from); }
            Tex("_BaseMap"); Col("_BaseColor"); Tex("_BumpMap"); Num("_Metallic"); Num("_Smoothness"); Tex("_EmissionMap"); Col("_EmissionColor");
            m.shader = unlit ? hdUnlit : hdLit;
            if (keep.TryGetValue("_BaseMap", out var t)) m.SetTexture(unlit ? "_UnlitColorMap" : "_BaseColorMap", (Texture)t);
            if (keep.TryGetValue("_BaseColor", out var c)) m.SetColor(unlit ? "_UnlitColor" : "_BaseColor", (Color)c);
            if (!unlit)
            {
                if (keep.TryGetValue("_BumpMap", out var n) && n != null) m.SetTexture("_NormalMap", (Texture)n);
                if (keep.TryGetValue("_Metallic", out var me)) m.SetFloat("_Metallic", (float)me);
                if (keep.TryGetValue("_Smoothness", out var sm)) m.SetFloat("_Smoothness", (float)sm);
                if (keep.TryGetValue("_EmissionMap", out var em) && em != null) m.SetTexture("_EmissiveColorMap", (Texture)em);
                if (keep.TryGetValue("_EmissionColor", out var ec)) m.SetColor("_EmissiveColor", (Color)ec);
            }
            EditorUtility.SetDirty(m);
            converted++;
            Debug.Log($"[hdrp] DONE material {path} -> {m.shader.name}");
        }
        Debug.Log($"[hdrp] DONE {converted} URP material(s) converted");
    }
}
