using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// Dựng scene demo card 3D: Tools > Card Demo > Build Scene
/// Hoặc batchmode: -executeMethod CardDemoBuilder.Build
public static class CardDemoBuilder
{
    internal const string Root = "Assets/CardDemo";
    internal const string Tex = Root + "/Textures/";
    const string Mat = Root + "/Materials/";
    const string ScenePath = "Assets/Scenes/CardDemo.unity";

    static readonly string[] Fronts = { "card_front_fire", "card_front_water", "card_front_forest" };

    [MenuItem("Tools/Card Demo/Build Scene")]
    public static void Build()
    {
        if (!AssetDatabase.IsValidFolder(Root + "/Materials"))
            AssetDatabase.CreateFolder(Root, "Materials");

        foreach (var f in Fronts) SetupTexture(Tex + f + ".png", false);
        SetupTexture(Tex + "card_back.png", false);
        SetupTexture(Tex + "card_foil_mask.png", true);
        SetupTexture(Tex + "shadow.png", false);
        SetupTexture(Tex + "bg.png", false);

        var cardShader = Shader.Find("SharderUltimate/Card3D");
        var softShader = Shader.Find("SharderUltimate/SoftShadow");
        var back = Load(Tex + "card_back.png");
        var mask = Load(Tex + "card_foil_mask.png");

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // Camera + bloom để foil/glare toả sáng nhẹ
        var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
        camGo.transform.position = new Vector3(0f, 0f, -9f);
        var cam = camGo.AddComponent<Camera>();
        cam.fieldOfView = 35f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.03f, 0.03f, 0.06f);
        cam.allowHDR = true;
        cam.GetUniversalAdditionalCameraData().renderPostProcessing = true;
        camGo.AddComponent<CameraFit>();

        var volGo = new GameObject("Global Volume");
        var vol = volGo.AddComponent<Volume>();
        vol.isGlobal = true;
        vol.sharedProfile = BuildVolumeProfile(Root + "/CardDemoVolume.asset", 0.6f);

        // Gán thuộc tính trước khi CreateAsset để chắc chắn được ghi xuống file .mat
        var bgMat = new Material(softShader);
        bgMat.SetTexture("_MainTex", Load(Tex + "bg.png"));
        SaveMaterial(bgMat, "Background");
        var bg = CreateQuad("Background", null, bgMat);
        bg.position = new Vector3(0f, 0f, 3f);
        bg.localScale = new Vector3(40f, 40f, 1f);

        var shadowMat = new Material(softShader);
        shadowMat.SetTexture("_MainTex", Load(Tex + "shadow.png"));
        shadowMat.SetColor("_Color", new Color(0f, 0f, 0f, 0.75f));
        SaveMaterial(shadowMat, "CardShadow");

        var cards = new GameObject("Cards");
        for (int i = 0; i < Fronts.Length; i++)
        {
            var m = new Material(cardShader);
            m.SetTexture("_FrontTex", Load(Tex + Fronts[i] + ".png"));
            m.SetTexture("_BackTex", back);
            m.SetTexture("_FoilMask", mask);
            SaveMaterial(m, "Card_" + Fronts[i].Replace("card_front_", ""));
            CreateCard(cards.transform, "Card_" + i, new Vector3((i - 1) * 3.2f, 0f, 0f), m, shadowMat, cam);
        }

        EditorSceneManager.SaveScene(scene, ScenePath);
        AddToBuildSettings(ScenePath);
        AssetDatabase.SaveAssets();
        Debug.Log("[CardDemo] Built " + ScenePath);
    }

    /// Chụp Game view ở độ phân giải thật (cần đang Play), lưu vào thư mục Screenshots ngoài Assets
    [MenuItem("Tools/Card Demo/Capture Screenshot _F9")]
    public static void CaptureScreenshot()
    {
        System.IO.Directory.CreateDirectory("Screenshots");
        string path = "Screenshots/card_" + System.DateTime.Now.ToString("HHmmss") + ".png";
        ScreenCapture.CaptureScreenshot(path);
        Debug.Log("[CardDemo] Screenshot -> " + System.IO.Path.GetFullPath(path));
    }

    static void CreateCard(Transform parent, string name, Vector3 pos, Material mat, Material shadowMat, Camera cam)
    {
        const float w = 2f, h = 3f;

        var root = new GameObject(name);
        root.transform.SetParent(parent, false);
        root.transform.localPosition = pos;
        var box = root.AddComponent<BoxCollider>();
        box.size = new Vector3(w, h, 0.1f);

        // Pivot giữ scale 1 để xoay/lật, quad con mang kích thước card
        var pivot = new GameObject("Pivot").transform;
        pivot.SetParent(root.transform, false);
        var face = CreateQuad("Face", pivot, mat);
        face.localScale = new Vector3(w, h, 1f);

        var shadowPivot = new GameObject("ShadowPivot").transform;
        shadowPivot.SetParent(root.transform, false);
        shadowPivot.localPosition = new Vector3(0.12f, -0.15f, 0.6f);
        var shadow = CreateQuad("Shadow", shadowPivot, shadowMat);
        shadow.localScale = new Vector3(w * 1.9f, h * 1.55f, 1f);

        var tilt = root.AddComponent<CardTilt3D>();
        tilt.visual = pivot;
        tilt.shadow = shadowPivot;
        tilt.cam = cam;
    }

    internal static Transform CreateQuad(string name, Transform parent, Material mat)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
        go.name = name;
        Object.DestroyImmediate(go.GetComponent<Collider>());
        go.transform.SetParent(parent, false);
        var r = go.GetComponent<Renderer>();
        r.sharedMaterial = mat;
        r.shadowCastingMode = ShadowCastingMode.Off;
        return go.transform;
    }

    internal static VolumeProfile BuildVolumeProfile(string path, float bloomIntensity)
    {
        AssetDatabase.DeleteAsset(path);
        var profile = ScriptableObject.CreateInstance<VolumeProfile>();
        AssetDatabase.CreateAsset(profile, path);

        var bloom = profile.Add<Bloom>(true);
        bloom.threshold.Override(0.95f);
        bloom.intensity.Override(bloomIntensity);
        bloom.scatter.Override(0.6f);
        var vignette = profile.Add<Vignette>(true);
        vignette.intensity.Override(0.3f);
        var tone = profile.Add<Tonemapping>(true);
        tone.mode.Override(TonemappingMode.Neutral);

        foreach (var c in profile.components) AssetDatabase.AddObjectToAsset(c, profile);
        EditorUtility.SetDirty(profile);
        return profile;
    }

    internal static Material SaveMaterial(Material m, string name)
    {
        if (!AssetDatabase.IsValidFolder(Root + "/Materials"))
            AssetDatabase.CreateFolder(Root, "Materials");
        string path = Mat + name + ".mat";
        AssetDatabase.DeleteAsset(path);
        AssetDatabase.CreateAsset(m, path);
        return m;
    }

    internal static Texture2D Load(string path) => AssetDatabase.LoadAssetAtPath<Texture2D>(path);

    internal static void SetupTexture(string path, bool linear, TextureWrapMode wrap = TextureWrapMode.Clamp)
    {
        var imp = AssetImporter.GetAtPath(path) as TextureImporter;
        if (imp == null) return;
        imp.textureType = TextureImporterType.Default;
        imp.sRGBTexture = !linear;
        imp.mipmapEnabled = true;
        imp.anisoLevel = 8;
        imp.wrapMode = wrap;
        imp.alphaIsTransparency = imp.DoesSourceTextureHaveAlpha();
        imp.maxTextureSize = 1024;
        imp.SaveAndReimport();
    }

    internal static void AddToBuildSettings(string path)
    {
        var list = new System.Collections.Generic.List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        if (list.Exists(s => s.path == path)) return;
        list.Insert(0, new EditorBuildSettingsScene(path, true));
        EditorBuildSettings.scenes = list.ToArray();
    }
}
