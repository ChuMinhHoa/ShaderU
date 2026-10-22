using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// Dựng scene demo tráng gương cho khung pha lê tím:
/// - Tools > Card Demo > Build Frame Shine Scene: tráng xoay tròn. Từ trái qua: khung gốc, option 1 tráng cả phần viền dày
///   ở dưới, option 2 không tráng phần đó. Card nào cũng có ảnh đặt bên dưới khung (cắt theo mask ô cửa) để thấy vệt sáng
///   chỉ phủ lên khung, ảnh bên dưới không đổi.
/// - Tools > Card Demo > Build Frame Sweep Shine Scene: tráng quét từ dưới lên, chỉ có khung (không ảnh bên trong,
///   không tráng phần viền dày dưới). Trái là khung gốc, phải là khung có hiệu ứng.
/// Bấm Play để xem chuyển động. Material đã có thì giữ nguyên (có thể đã chỉnh tay trong Inspector), chỉ tạo khi chưa có.
public static class FrameShineBuilder
{
    const string Dir = CardDemoBuilder.Tex + "FrameShine/";
    const string FrameArt = Dir + "frame_crystal_purple.png";
    const string WindowMask = Dir + "frame_crystal_purple_window.png"; // ô cửa nới 8px, cùng kích thước với khung
    const string LightArt = Dir + "light.png"; // dải sáng: ngang ảnh = ngang dải, alpha = độ sáng. Cần wrap Clamp
    const string PictureBg = CardDemoBuilder.Tex + "Holo/space_bg.png";
    const string PictureFg = CardDemoBuilder.Tex + "Holo/cat_astronaut.png";
    const string ScenePath = "Assets/Scenes/FrameShineDemo.unity";
    const string PanelMat = "FrameSpinShine_Crystal_Panel";     // option 1: tráng cả phần viền dày dưới
    const string NoPanelMat = "FrameSpinShine_Crystal_NoPanel"; // option 2: chỉ tráng vành khung
    const string SweepScenePath = "Assets/Scenes/FrameSweepShineDemo.unity";
    const string SweepMat = "FrameSweepShine_Crystal";         // quét từ dưới lên, chỉ tráng vành khung
    const float CardScale = 1.35f;   // card dựng theo px ảnh gốc rồi phóng to cả khối
    const float CardSpacing = 580f;

    // Ô cửa của ảnh khung (px, gốc góc dưới trái): tâm và cạnh hình vuông phủ kín ô cửa
    static readonly Vector2 WindowCenter = new Vector2(194.5f, 299.5f);
    const float PictureSize = 345f;

    [MenuItem("Tools/Card Demo/Build Frame Shine Scene")]
    public static void Build()
    {
        var shader = Shader.Find("SharderUltimate/FrameSpinShine");
        if (shader == null) { Debug.LogError("[CardDemo] Không thấy shader SharderUltimate/FrameSpinShine"); return; }
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        var frame = GemFrameBuilder.SetupSprite(FrameArt);
        var window = GemFrameBuilder.SetupSprite(WindowMask);
        if (frame == null || window == null) { Debug.LogError("[CardDemo] Không thấy " + FrameArt + " hoặc " + WindowMask); return; }

        var light = CardDemoBuilder.Load(LightArt);
        if (light == null) { Debug.LogError("[CardDemo] Không thấy " + LightArt); return; }

        var panel = LoadOrCreateMaterial(PanelMat, shader, frame, light, 1f);
        var noPanel = LoadOrCreateMaterial(NoPanelMat, shader, frame, light, 0f);

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var canvas = CreateCameraAndCanvas();

        CreateCard(canvas, "Card_Original", frame, window, null, new Vector2(-CardSpacing, 0f));
        CreateCard(canvas, "Card_Shine_Panel", frame, window, panel, Vector2.zero);
        CreateCard(canvas, "Card_Shine_NoPanel", frame, window, noPanel, new Vector2(CardSpacing, 0f));

        EditorSceneManager.SaveScene(scene, ScenePath);
        CardDemoBuilder.AddToBuildSettings(ScenePath);
        AssetDatabase.SaveAssets();
        Debug.Log("[CardDemo] Built " + ScenePath);
    }

    [MenuItem("Tools/Card Demo/Build Frame Sweep Shine Scene")]
    public static void BuildSweep()
    {
        var shader = Shader.Find("SharderUltimate/FrameSweepShine");
        if (shader == null) { Debug.LogError("[CardDemo] Không thấy shader SharderUltimate/FrameSweepShine"); return; }
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        var frame = GemFrameBuilder.SetupSprite(FrameArt);
        if (frame == null) { Debug.LogError("[CardDemo] Không thấy " + FrameArt); return; }

        var light = CardDemoBuilder.Load(LightArt);
        if (light == null) { Debug.LogError("[CardDemo] Không thấy " + LightArt); return; }

        var sweep = LoadOrCreateMaterial(SweepMat, shader, frame, light, 0f);

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var canvas = CreateCameraAndCanvas();

        CreateCard(canvas, "Card_Original", frame, null, null, new Vector2(-CardSpacing * 0.5f, 0f));
        CreateCard(canvas, "Card_Sweep", frame, null, sweep, new Vector2(CardSpacing * 0.5f, 0f));

        EditorSceneManager.SaveScene(scene, SweepScenePath);
        CardDemoBuilder.AddToBuildSettings(SweepScenePath);
        AssetDatabase.SaveAssets();
        Debug.Log("[CardDemo] Built " + SweepScenePath);
    }

    /// Camera nền tím đậm + Canvas overlay scale theo 1920x1080, trả về transform của Canvas
    static Transform CreateCameraAndCanvas()
    {
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
        return canvasGo.transform;
    }

    /// Các thông số khác lấy mặc định của shader, chỉ khác có tráng phần viền dày dưới hay không.
    /// Material đã có mà chưa gán ảnh light (tạo từ bản shader cũ) thì chỉ điền ảnh light, không đụng thông số khác
    static Material LoadOrCreateMaterial(string name, Shader shader, Sprite frame, Texture2D light, float panelShine)
    {
        var m = AssetDatabase.LoadAssetAtPath<Material>(CardDemoBuilder.Root + "/Materials/" + name + ".mat");
        if (m != null)
        {
            if (m.GetTexture("_LightTex") == null)
            {
                m.SetTexture("_LightTex", light);
                EditorUtility.SetDirty(m);
            }
            return m;
        }
        m = new Material(shader) { mainTexture = frame.texture };
        m.SetTexture("_LightTex", light);
        m.SetFloat("_PanelShine", panelShine);
        return CardDemoBuilder.SaveMaterial(m, name);
    }

    /// Card = ảnh (nền + nhân vật) nằm trong mask ô cửa, khung vẽ đè lên trên cùng. window = null thì chỉ có khung
    static void CreateCard(Transform parent, string name, Sprite frame, Sprite window, Material mat, Vector2 pos)
    {
        var size = frame.rect.size;
        var root = new GameObject(name, typeof(RectTransform));
        root.transform.SetParent(parent, false);
        var rt = (RectTransform)root.transform;
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        rt.localScale = Vector3.one * CardScale;

        if (window != null)
        {
            var maskImg = CreateImage(root.transform, "Window", window, null, size, Vector2.zero);
            maskImg.gameObject.AddComponent<Mask>().showMaskGraphic = false;
            var picPos = WindowCenter - size * 0.5f;
            CreateRawImage(maskImg.transform, "Picture_Bg", PictureBg, picPos);
            CreateRawImage(maskImg.transform, "Picture_Character", PictureFg, picPos);
        }

        CreateImage(root.transform, "Frame", frame, mat, size, Vector2.zero);
    }

    static Image CreateImage(Transform parent, string name, Sprite sprite, Material mat, Vector2 size, Vector2 pos)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        var img = go.GetComponent<Image>();
        img.sprite = sprite;
        img.material = mat;
        img.raycastTarget = false;
        return img;
    }

    static void CreateRawImage(Transform parent, string name, string texPath, Vector2 pos)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(RawImage));
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(PictureSize, PictureSize);
        var img = go.GetComponent<RawImage>();
        img.texture = CardDemoBuilder.Load(texPath);
        img.raycastTarget = false;
    }
}
