using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// Dựng scene demo khung đá quý cho thẻ UI: Tools > Card Demo > Build Gem Frame Scene
/// Từ trái qua: ảnh gốc, khung đá quý tráng gương, satin, dòng màu cyan/tím có glow, đá mọc dần phủ viền.
/// Bấm Play để xem chuyển động.
/// Material đã có thì giữ nguyên (có thể đã chỉnh tay trong Inspector), chỉ tạo khi chưa có.
public static class GemFrameBuilder
{
    const string Art = CardDemoBuilder.Tex + "GemFrame/card_halloween.png";
    const string ScenePath = "Assets/Scenes/GemFrameDemo.unity";
    const string MirrorMat = "GemFrame_Halloween";
    const string SatinMat = "GemFrame_Halloween_Satin";
    const string FlowMat = "GemFrame_Halloween_Flow";
    const string GrowMat = "GemFrame_Halloween_Grow";
    const float CardScale = 0.85f;
    const float CardSpacing = 375f;
    const float GlowPadding = 36f; // đủ chỗ cho glow ~3 lần _GlowWidth

    // Bảng màu xanh tím + màu bóng xanh biển = giá trị mặc định trong shader
    static readonly string[] PaletteProps =
    {
        "_ColorDeep", "_ColorDeepAlt", "_ColorMid", "_ColorMidAlt", "_ColorLight", "_ColorLightAlt",
        "_OutlineColor", "_LipHighlight", "_SheenColor",
    };

    [MenuItem("Tools/Card Demo/Build Gem Frame Scene")]
    public static void Build()
    {
        var shader = Shader.Find("SharderUltimate/GemFrame");
        if (shader == null) { Debug.LogError("[CardDemo] Không thấy shader SharderUltimate/GemFrame"); return; }
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        var sprite = SetupSprite(Art);
        if (sprite == null) { Debug.LogError("[CardDemo] Không thấy " + Art); return; }

        // mainTexture chỉ để xem preview trong Inspector, Image tự gán sprite khi vẽ
        var mirror = LoadOrCreateMaterial(MirrorMat, () => new Material(shader) { mainTexture = sprite.texture });
        // Satin copy từ bản tráng gương, mặt cắt dịu hơn để dải bóng mềm đọc ra chất vải
        var satin = LoadOrCreateMaterial(SatinMat, () =>
        {
            var m = new Material(mirror);
            m.SetFloat("_SheenType", 1f);
            m.SetFloat("_FacetContrast", Mathf.Min(m.GetFloat("_FacetContrast"), 0.55f));
            m.SetFloat("_FacetRandom", Mathf.Min(m.GetFloat("_FacetRandom"), 0.25f));
            return m;
        });
        // Dòng màu: mảng cyan/tím trôi quanh khung, mặt cắt mờ, viền sáng 2 mép, glow ngoài, không có lớp bóng
        var flow = LoadOrCreateMaterial(FlowMat, () =>
        {
            var m = new Material(shader) { mainTexture = sprite.texture };
            m.SetFloat("_Flow", 1f);
            m.SetFloat("_Glow", 0.45f);
            m.SetFloat("_GlowWidth", 8f);
            m.SetFloat("_SheenType", 2f);
            m.SetFloat("_FacetContrast", 0.5f);
            m.SetFloat("_FacetRandom", 0.3f);
            m.SetFloat("_Specular", 0.5f);
            m.SetFloat("_Sparkle", 0.6f);
            m.SetFloat("_InnerShadow", 0.2f);
            m.SetColor("_OutlineColor", new Color(0.92f, 0.88f, 1f));
            return m;
        });
        // Đá mọc dần từ đỉnh, lan 2 chiều quanh khung phủ lên viền gốc; tự lặp để xem trong demo,
        // dùng thật thì đặt _CoverLoop = 0 và cho script chạy _Cover từ 0 lên 1
        var grow = LoadOrCreateMaterial(GrowMat, () =>
        {
            var m = new Material(shader) { mainTexture = sprite.texture };
            m.SetFloat("_CoverLoop", 7f);
            return m;
        });

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
        var cam = camGo.AddComponent<Camera>();
        cam.orthographic = true;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.1f, 0.06f, 0.18f);

        var canvasGo = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGo.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        float x0 = -2f * CardSpacing;
        CreateCard(canvasGo.transform, "Card_Original", sprite, null, new Vector2(x0, 0f));
        CreateCard(canvasGo.transform, "Card_GemMirror", sprite, mirror, new Vector2(x0 + CardSpacing, 0f));
        CreateCard(canvasGo.transform, "Card_GemSatin", sprite, satin, new Vector2(x0 + CardSpacing * 2f, 0f));
        var flowCard = CreateCard(canvasGo.transform, "Card_GemFlow", sprite, flow, new Vector2(x0 + CardSpacing * 3f, 0f));
        flowCard.gameObject.AddComponent<UIQuadPadding>().padding = GlowPadding;
        CreateCard(canvasGo.transform, "Card_GemGrow", sprite, grow, new Vector2(x0 + CardSpacing * 4f, 0f));

        EditorSceneManager.SaveScene(scene, ScenePath);
        CardDemoBuilder.AddToBuildSettings(ScenePath);
        AssetDatabase.SaveAssets();
        Debug.Log("[CardDemo] Built " + ScenePath);
    }

    /// Đặt lại các màu của 2 material demo về bảng xanh tím mặc định của shader, các thông số khác giữ nguyên. Ctrl+Z để hoàn tác
    [MenuItem("Tools/Card Demo/Gem Frame Palette Blue Violet")]
    public static void ApplyPalette()
    {
        var shader = Shader.Find("SharderUltimate/GemFrame");
        if (shader == null) { Debug.LogError("[CardDemo] Không thấy shader SharderUltimate/GemFrame"); return; }

        foreach (var name in new[] { MirrorMat, SatinMat })
        {
            var m = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath(name));
            if (m == null) continue;
            Undo.RecordObject(m, "Gem Frame Palette Blue Violet");
            foreach (var prop in PaletteProps)
                m.SetColor(prop, shader.GetPropertyDefaultVectorValue(shader.FindPropertyIndex(prop)));
            EditorUtility.SetDirty(m);
        }
        AssetDatabase.SaveAssets();
        Debug.Log("[CardDemo] Gem frame palette -> blue violet");
    }

    static string MaterialPath(string name) => CardDemoBuilder.Root + "/Materials/" + name + ".mat";

    static Material LoadOrCreateMaterial(string name, System.Func<Material> create)
    {
        var m = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath(name));
        return m != null ? m : CardDemoBuilder.SaveMaterial(create(), name);
    }

    /// Sprite đơn, Full Rect, không mipmap: shader cần UV 0..1 phủ đúng cả ảnh
    internal static Sprite SetupSprite(string path)
    {
        var imp = AssetImporter.GetAtPath(path) as TextureImporter;
        if (imp == null) return null;
        imp.textureType = TextureImporterType.Sprite;
        imp.spriteImportMode = SpriteImportMode.Single;
        var ts = new TextureImporterSettings();
        imp.ReadTextureSettings(ts);
        ts.spriteMeshType = SpriteMeshType.FullRect;
        imp.SetTextureSettings(ts);
        imp.mipmapEnabled = false;
        imp.alphaIsTransparency = true;
        imp.wrapMode = TextureWrapMode.Clamp;
        imp.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    static Image CreateCard(Transform parent, string name, Sprite sprite, Material mat, Vector2 pos)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        rt.anchoredPosition = pos;
        rt.sizeDelta = sprite.rect.size * CardScale;
        var img = go.GetComponent<Image>();
        img.sprite = sprite;
        img.material = mat;
        img.raycastTarget = false;
        return img;
    }
}
