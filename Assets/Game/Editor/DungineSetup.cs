using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UIElements;

namespace Dungine.EditorTools
{
    /// <summary>One-time project setup: material templates (keeps URP variants in builds), UI panel settings, boot scene.</summary>
    [InitializeOnLoad]
    public static class ProjectSetup
    {
        const string MatDir = "Assets/Game/Resources/Mat";
        const string UIDir = "Assets/Game/Resources/UI";
        const string ScenePath = "Assets/Scenes/Main.unity";

        static ProjectSetup()
        {
            EditorApplication.delayCall += () =>
            {
                if (!File.Exists(MatDir + "/LitTemplate_Cutout.mat") || !File.Exists(UIDir + "/PanelSettings.asset") || !File.Exists(ScenePath))
                    Run();
            };
        }

        [MenuItem("Dungine/Run Project Setup")]
        public static void Run()
        {
            Directory.CreateDirectory(MatDir);
            Directory.CreateDirectory(UIDir);
            var lit = Shader.Find("Universal Render Pipeline/Lit");
            var unlit = Shader.Find("Universal Render Pipeline/Unlit");
            var terrain = Shader.Find("Universal Render Pipeline/Terrain/Lit");

            var m = new Material(lit);
            m.EnableKeyword("_NORMALMAP"); m.EnableKeyword("_EMISSION");
            m.SetColor("_EmissionColor", Color.black);
            m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
            Save(m, "LitTemplate");

            var m2 = new Material(lit);
            m2.EnableKeyword("_EMISSION");
            m2.SetColor("_EmissionColor", Color.black);
            Save(m2, "LitTemplate_NoNormal");

            var m3 = new Material(lit);
            m3.SetFloat("_Surface", 1);
            m3.SetOverrideTag("RenderType", "Transparent");
            m3.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            m3.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            m3.SetFloat("_ZWrite", 0);
            m3.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m3.renderQueue = 3000;
            Save(m3, "LitTemplate_Transparent");

            var m4 = new Material(lit);
            m4.EnableKeyword("_NORMALMAP"); m4.EnableKeyword("_EMISSION");
            m4.SetFloat("_Cull", 0);
            Save(m4, "LitTemplate_CullOff");

            var m5 = new Material(lit);
            m5.SetFloat("_AlphaClip", 1); m5.SetFloat("_Cutoff", 0.4f); m5.EnableKeyword("_ALPHATEST_ON"); m5.EnableKeyword("_EMISSION"); m5.SetFloat("_Cull", 0);
            m5.renderQueue = 2450;
            Save(m5, "LitTemplate_Cutout");

            Save(new Material(unlit), "UnlitTemplate");
            if (terrain) Save(new Material(terrain), "TerrainTemplate");

            // UI theme + panel settings
            string tss = UIDir + "/DungineTheme.tss";
            if (!File.Exists(tss)) File.WriteAllText(tss, "@import url(\"unity-theme://default\");\n");
            AssetDatabase.ImportAsset(tss);
            var theme = AssetDatabase.LoadAssetAtPath<ThemeStyleSheet>(tss);
            string psPath = UIDir + "/PanelSettings.asset";
            var ps = AssetDatabase.LoadAssetAtPath<PanelSettings>(psPath);
            if (!ps)
            {
                ps = ScriptableObject.CreateInstance<PanelSettings>();
                AssetDatabase.CreateAsset(ps, psPath);
            }
            ps.themeStyleSheet = theme;
            ps.scaleMode = PanelScaleMode.ScaleWithScreenSize;
            ps.referenceResolution = new Vector2Int(1920, 1080);
            ps.screenMatchMode = PanelScreenMatchMode.MatchWidthOrHeight;
            ps.match = 0.5f;
            ps.sortingOrder = 10;
            EditorUtility.SetDirty(ps);

            // Boot scene
            if (!File.Exists(ScenePath))
            {
                Directory.CreateDirectory("Assets/Scenes");
                var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                EditorSceneManager.SaveScene(scene, ScenePath);
            }
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Dungine] Project setup complete.");
        }

        static void Save(Material m, string name)
        {
            string p = $"{MatDir}/{name}.mat";
            var existing = AssetDatabase.LoadAssetAtPath<Material>(p);
            if (existing) { existing.CopyPropertiesFromMaterial(m); existing.shaderKeywords = m.shaderKeywords; EditorUtility.SetDirty(existing); }
            else AssetDatabase.CreateAsset(m, p);
        }
    }
}
