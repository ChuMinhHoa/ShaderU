using System.Collections.Generic;
using Spine.Unity;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using static CardDemoBuilder;

/// Dựng prefab thẻ holographic có cửa sổ portal 3D và scene demo: Tools > Card Demo > Build Holo Card Scene
/// Sau khi dựng, mọi chỉnh sửa (oval, nhân vật, không gian) làm trên component HoloCardView của prefab.
public static class HoloCardBuilder
{
    const string HTex = Tex + "Holo/";
    const string PrefabPath = Root + "/Prefabs/HoloCard.prefab";
    const string ScenePath = "Assets/Scenes/HoloCardDemo.unity";
    const string VariantPath = Root + "/Prefabs/HoloCard_PopOut.prefab";
    const string PopOutScenePath = "Assets/Scenes/HoloCardPopOutDemo.unity";
    const string VolumePath = Root + "/HoloCardVolume.asset";
    const string SpineDir = Root + "/Spine/HoloGirl/";
    const string SpineVariantPath = Root + "/Prefabs/HoloCard_Spine.prefab";
    const string SpineScenePath = "Assets/Scenes/HoloCardSpineDemo.unity";
    const string ShowcaseScenePath = "Assets/Scenes/HoloEffectsShowcase.unity";
    static readonly string[] ShowcasePrefabs = { "HoloCard_ThinFilm", "HoloCard_Glitter", "HoloCard_Hologram" };
    // Hiệu ứng mặt thẻ -> có material poster riêng không (Hologram ám cả 2 tấm poster)
    static readonly (string effect, bool posters)[] SpineEffects = { ("ThinFilm", false), ("Glitter", false), ("Hologram", true) };
    const string LenticularPrefabPath = Root + "/Prefabs/HoloCard_Lenticular.prefab";
    const string LenticularScenePath = "Assets/Scenes/HoloLenticularDemo.unity";
    // Khung 0 là _MainTex, các khung sau vào _LentTex1.._LentTex3 (tối đa 4)
    static readonly string[] LenticularFrames = { "lenticular_0", "lenticular_1" };

    // Layout poster phải khớp với texture holo_front.png (720x1008)
    const float CardW = HoloCardView.Width, CardH = HoloCardView.Height;
    const float PxW = 720f, PxH = 1008f;
    static readonly Rect TitleRect = Rect.MinMaxRect(50, 44, 670, 124);
    static readonly Rect TextRect = Rect.MinMaxRect(50, 735, 670, 960);

    // Ref mặc định trong material; khi chạy mỗi thẻ tự lấy ref riêng (HoloCardView.AssignStencilRef)
    const int StencilRef = 1;

    // Thứ tự vẽ: scene (mặt thẻ 2450) -> mask -> reset depth -> nội dung portal -> khôi phục depth mặt cửa sổ
    // -> phần nhân vật pop-out (để mặt cửa sổ của thẻ phía trước che được pop-out của thẻ phía sau).
    // Mask nằm trong dải transparent (>2500) để các thẻ xếp từ xa tới gần: cửa sổ gần nhất thắng khi chồng nhau.
    const int QueueMask = 2501, QueueDepthReset = 2502, QueueBezel = 2503, QueueSpace = 2504;
    const int QueueStars = 3000, QueueCharacter = 3010, QueueVignette = 3100;
    const int QueueDepthRestore = 3200, QueuePopOut = 3300;

    [MenuItem("Tools/Card Demo/Build Holo Card Scene")]
    public static void Build()
    {
        foreach (var n in new[] { "holo_front", "holo_back", "poster_title", "poster_text", "cat_astronaut", "bg_purple", "star_dot" })
            SetupTexture(HTex + n + ".png", false);
        SetupTexture(HTex + "holo_mask.png", true);
        SetupTexture(HTex + "facets.png", true, TextureWrapMode.Mirror);
        SetupTexture(HTex + "space_bg.png", false, TextureWrapMode.Mirror);
        SetupTexture(HTex + "holo_color.png", false, TextureWrapMode.Repeat);
        SetupTexture(Tex + "shadow.png", false);

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        var bgMat = new Material(Shader.Find("SharderUltimate/SoftShadow"));
        bgMat.SetTexture("_MainTex", Load(HTex + "bg_purple.png"));
        SaveMaterial(bgMat, "Holo_Background");
        CreateEnvironment(bgMat, BuildVolumeProfile(VolumePath, 1.0f));

        var card = BuildCard();
        if (!AssetDatabase.IsValidFolder(Root + "/Prefabs")) AssetDatabase.CreateFolder(Root, "Prefabs");
        PrefabUtility.SaveAsPrefabAssetAndConnect(card, PrefabPath, InteractionMode.AutomatedAction);

        EditorSceneManager.SaveScene(scene, ScenePath);
        AddToBuildSettings(ScenePath);
        AssetDatabase.SaveAssets();
        Debug.Log("[CardDemo] Built " + PrefabPath + " + " + ScenePath);
    }

    /// Biến thể nhân vật nổi ra ngoài cửa sổ: Prefab Variant của HoloCard + scene demo riêng.
    /// Không dựng lại prefab gốc, không tạo lại material / volume dùng chung (scene khác đang tham chiếu).
    [MenuItem("Tools/Card Demo/Build Pop-Out Variant")]
    public static void BuildPopOutVariant()
    {
        var basePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        if (basePrefab == null)
        {
            Debug.LogError("[CardDemo] Chưa có " + PrefabPath + ", chạy Build Holo Card Scene trước");
            return;
        }

        // Mở thêm (additive) để không đóng / mất thay đổi chưa lưu của scene đang mở, xong thì đóng lại
        var previous = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        UnityEngine.SceneManagement.SceneManager.SetActiveScene(scene);
        CreateEnvironment(AssetDatabase.LoadAssetAtPath<Material>(Root + "/Materials/Holo_Background.mat"),
                          AssetDatabase.LoadAssetAtPath<VolumeProfile>(VolumePath));

        var card = (GameObject)PrefabUtility.InstantiatePrefab(basePrefab, scene);
        card.name = "HoloCard_PopOut";
        var view = card.GetComponent<HoloCardView>();
        view.popOut = true;
        view.characterSize = 1.9f;
        // Nhô ra trước mặt thẻ (Z âm) và đẩy lên để mũ bảo hiểm vượt qua mép trên cửa sổ
        view.characterMesh.transform.localPosition = new Vector3(0f, 0.32f, -0.12f);
        view.Rebuild();

        PrefabUtility.SaveAsPrefabAssetAndConnect(card, VariantPath, InteractionMode.AutomatedAction);
        EditorSceneManager.SaveScene(scene, PopOutScenePath);
        AddToBuildSettings(PopOutScenePath);
        AssetDatabase.SaveAssets();
        if (previous.IsValid()) UnityEngine.SceneManagement.SceneManager.SetActiveScene(previous);
        EditorSceneManager.CloseScene(scene, true);
        Debug.Log("[CardDemo] Built " + VariantPath + " + " + PopOutScenePath + " (mở scene này để xem)");
    }

    /// Nhân vật là Spine animation thay cho quad ảnh phẳng: Prefab Variant của HoloCard_PopOut + scene demo riêng.
    /// Skeleton sinh bằng Tools/SpineRigGen/HoloGirlGen.cs từ ảnh Textures/Holo/1 (4).png (atlas = chính ảnh đó, 1:1,
    /// nên UV mesh = UV ảnh và các vùng pop-out vẫn dùng được). Spine tự tạo HoloGirl_SkeletonData khi import json.
    [MenuItem("Tools/Card Demo/Build Spine Character Variant")]
    public static void BuildSpineVariant()
    {
        var basePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(VariantPath);
        var data = AssetDatabase.LoadAssetAtPath<SkeletonDataAsset>(SpineDir + "HoloGirl_SkeletonData.asset");
        if (basePrefab == null || data == null)
        {
            Debug.LogError("[CardDemo] Cần có " + VariantPath + " và " + SpineDir + "HoloGirl_SkeletonData.asset (spine-unity tự tạo khi import HoloGirl.json)");
            return;
        }

        // Atlas là ảnh nhân vật gốc: import giống các ảnh nhân vật khác (sRGB, clamp)
        string texPath = SpineDir + "HoloGirl.png";
        SetupTexture(texPath, false);
        var tex = Load(texPath);
        // Skeleton dài 1 đơn vị theo cạnh dài ảnh, SpinePortalCharacter nhân thêm characterSize
        ((TextureImporter)AssetImporter.GetAtPath(texPath)).GetSourceTextureWidthAndHeight(out int texW, out int texH);
        data.scale = 1f / Mathf.Max(texW, texH);
        data.Clear();
        EditorUtility.SetDirty(data);
        // Atlas là ảnh straight alpha (không premultiply): báo cho material mặc định của Spine (preview trong Inspector)
        foreach (var atlas in data.atlasAssets)
            foreach (var m in atlas.Materials)
            {
                m.SetFloat("_StraightAlphaInput", 1f);
                EditorUtility.SetDirty(m);
            }

        var inside = CopyCharacterMaterial("Holo_Cat", "Holo_HoloGirl", tex);
        var popOut = CopyCharacterMaterial("Holo_CatPopOut", "Holo_HoloGirlPopOut", tex);

        var previous = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        UnityEngine.SceneManagement.SceneManager.SetActiveScene(scene);
        CreateEnvironment(AssetDatabase.LoadAssetAtPath<Material>(Root + "/Materials/Holo_Background.mat"),
                          AssetDatabase.LoadAssetAtPath<VolumeProfile>(VolumePath));

        var card = (GameObject)PrefabUtility.InstantiatePrefab(basePrefab, scene);
        card.name = "HoloCard_Spine";
        var view = card.GetComponent<HoloCardView>();
        view.character = tex;
        view.externalCharacterMesh = true;
        // Vùng tràn ra khung chỉnh cho ảnh này (lấy từ thẻ trong HoloCardDemo): đầu, ngực, hai vai
        view.popOutZones = new List<HoloCardView.PopOutZone>
        {
            new HoloCardView.PopOutZone { center = new Vector2(0.4755f, 0.8718f), radius = new Vector2(0.3016f, 0.2133f), softness = 0.0807f },
            new HoloCardView.PopOutZone { center = new Vector2(0.5223f, 0.6606f), radius = new Vector2(0.2414f, 0.1574f), softness = 0.01f },
            new HoloCardView.PopOutZone { center = new Vector2(0.2810f, 0.6149f), radius = new Vector2(0.1799f, 0.1290f), softness = 0.01f },
            new HoloCardView.PopOutZone { center = new Vector2(0.3547f, 0.5899f), radius = new Vector2(0.1799f, 0.1290f), softness = 0.01f },
        };

        // SkeletonAnimation dùng luôn MeshFilter / MeshRenderer của Character, thêm trước để RequireComponent
        // của SpinePortalCharacter không tự thêm một SkeletonRenderer trơn
        var character = view.characterMesh.gameObject;
        var anim = character.AddComponent<SkeletonAnimation>();
        anim.skeletonDataAsset = data;
        anim.loop = true;
        anim.AnimationName = "idle";
        var portalCharacter = character.AddComponent<SpinePortalCharacter>();
        portalCharacter.insideMaterial = inside;
        portalCharacter.popOutMaterial = popOut;
        portalCharacter.autoTouchEvery = 5f;
        // Mesh Spine sinh lúc chạy, không lưu vào prefab
        view.characterMesh.sharedMesh = null;
        character.GetComponent<MeshRenderer>().sharedMaterials = new[] { inside, popOut };
        view.Rebuild();

        PrefabUtility.SaveAsPrefabAssetAndConnect(card, SpineVariantPath, InteractionMode.AutomatedAction);
        EditorSceneManager.SaveScene(scene, SpineScenePath);
        AddToBuildSettings(SpineScenePath);
        AssetDatabase.SaveAssets();
        if (previous.IsValid()) UnityEngine.SceneManagement.SceneManager.SetActiveScene(previous);
        EditorSceneManager.CloseScene(scene, true);
        Debug.Log("[CardDemo] Built " + SpineVariantPath + " + " + SpineScenePath + " (mở scene này rồi Play để xem anim)");
    }

    /// Variant của thẻ Spine (HoloCard_Spine) cho từng hiệu ứng mặt thẻ: chỉ đổi material Face (+ poster với Hologram),
    /// phần còn lại kế thừa HoloCard_Spine. Dựng trong preview scene nên không đụng scene đang mở,
    /// và không gọi SaveAssets để khỏi ghi xuống đĩa material đang sửa dở
    [MenuItem("Tools/Card Demo/Build Spine Effect Variants")]
    public static void BuildSpineEffectVariants()
    {
        var basePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(SpineVariantPath);
        if (basePrefab == null)
        {
            Debug.LogError("[CardDemo] Chưa có " + SpineVariantPath + ", chạy Build Spine Character Variant trước");
            return;
        }

        var preview = EditorSceneManager.NewPreviewScene();
        // Instance nằm dưới object cha đang tắt: HoloCardView / Spine (chạy cả edit mode) không khởi tạo,
        // nên không tự gán mesh / material nhân vật thành override thừa trong variant
        var holder = EditorUtility.CreateGameObjectWithHideFlags("VariantHolder", HideFlags.HideAndDontSave);
        holder.SetActive(false);
        UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(holder, preview);
        try
        {
            foreach (var (effect, posters) in SpineEffects)
            {
                var card = (GameObject)PrefabUtility.InstantiatePrefab(basePrefab, holder.transform);
                card.name = "HoloCard_Spine_" + effect;
                var pivot = card.transform.Find("Pivot");
                SetFaceMaterial(pivot, "Face", "Holo_Face_" + effect);
                if (posters)
                {
                    SetFaceMaterial(pivot, "PosterTitle", "Holo_PosterTitle_" + effect);
                    SetFaceMaterial(pivot, "PosterText", "Holo_PosterText_" + effect);
                }

                string path = Root + "/Prefabs/HoloCard_Spine_" + effect + ".prefab";
                PrefabUtility.SaveAsPrefabAsset(card, path);
                Object.DestroyImmediate(card);
                Debug.Log("[CardDemo] Built " + path);
            }
        }
        finally
        {
            Object.DestroyImmediate(holder);
            EditorSceneManager.ClosePreviewScene(preview);
        }
    }

    /// Gán material cho renderer con của Pivot và ghi thành override của prefab instance
    static void SetFaceMaterial(Transform pivot, string child, string material)
    {
        var t = pivot != null ? pivot.Find(child) : null;
        var m = AssetDatabase.LoadAssetAtPath<Material>(Root + "/Materials/" + material + ".mat");
        if (t == null || m == null)
        {
            Debug.LogError("[CardDemo] Thiếu Pivot/" + child + " hoặc material " + material);
            return;
        }
        var r = t.GetComponent<Renderer>();
        r.sharedMaterial = m;
        PrefabUtility.RecordPrefabInstancePropertyModifications(r);
    }

    /// Scene so sánh các hiệu ứng mặt thẻ (variant ThinFilm / Glitter / Hologram) xếp cạnh nhau.
    /// Dựng ở scene phụ rồi đóng lại như Build Pop-Out Variant, không đụng scene đang mở
    [MenuItem("Tools/Card Demo/Build Holo Effects Showcase")]
    public static void BuildEffectsShowcase()
    {
        var previous = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        UnityEngine.SceneManagement.SceneManager.SetActiveScene(scene);
        var fit = CreateEnvironment(AssetDatabase.LoadAssetAtPath<Material>(Root + "/Materials/Holo_Background.mat"),
                                    AssetDatabase.LoadAssetAtPath<VolumeProfile>(VolumePath));

        const float spacing = 2.9f;
        for (int i = 0; i < ShowcasePrefabs.Length; i++)
        {
            string path = Root + "/Prefabs/" + ShowcasePrefabs[i] + ".prefab";
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null)
            {
                Debug.LogError("[CardDemo] Không tìm thấy " + path);
                continue;
            }
            var card = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
            card.transform.position = new Vector3((i - (ShowcasePrefabs.Length - 1) * 0.5f) * spacing, 0f, 0f);
        }
        fit.width = ShowcasePrefabs.Length * spacing + 0.7f;

        EditorSceneManager.SaveScene(scene, ShowcaseScenePath);
        AddToBuildSettings(ShowcaseScenePath);
        AssetDatabase.SaveAssets();
        if (previous.IsValid()) UnityEngine.SceneManagement.SceneManager.SetActiveScene(previous);
        EditorSceneManager.CloseScene(scene, true);
        Debug.Log("[CardDemo] Built " + ShowcaseScenePath + " (mở scene này rồi Play để xem)");
    }

    /// Thẻ lenticular (hiệu ứng Lenticular của HoloCard): nhìn thẳng là lenticular_0 (cô gái trên sân thượng Santorini),
    /// nghiêng sang một bên thì đổi sang lenticular_1 (sân thượng trống). Thẻ trơn không cửa sổ portal nên không có HoloCardView.
    /// Dựng ở scene phụ rồi đóng lại; không gọi SaveAssets để khỏi ghi xuống đĩa material đang sửa dở
    [MenuItem("Tools/Card Demo/Build Lenticular Card")]
    public static void BuildLenticularCard()
    {
        var background = AssetDatabase.LoadAssetAtPath<Material>(Root + "/Materials/Holo_Background.mat");
        var shadowMat = AssetDatabase.LoadAssetAtPath<Material>(Root + "/Materials/Holo_CardShadow.mat");
        if (background == null || shadowMat == null)
        {
            Debug.LogError("[CardDemo] Thiếu Holo_Background / Holo_CardShadow, chạy Build Holo Card Scene trước");
            return;
        }
        foreach (var f in LenticularFrames) SetupTexture(HTex + f + ".png", false);

        var faceMat = new Material(Shader.Find("SharderUltimate/HoloCard"));
        faceMat.SetTexture("_MainTex", Load(HTex + LenticularFrames[0] + ".png"));
        for (int i = 1; i < LenticularFrames.Length; i++)
            faceMat.SetTexture("_LentTex" + i, Load(HTex + LenticularFrames[i] + ".png"));
        faceMat.SetFloat("_LentFrames", LenticularFrames.Length);
        faceMat.SetFloat("_Effect", (float)HoloCardEffect.Lenticular);
        faceMat.SetTexture("_BackTex", Load(HTex + "holo_back.png"));
        faceMat.SetTexture("_HoloTex", Load(HTex + "holo_color.png"));
        faceMat.SetTextureScale("_HoloTex", new Vector2(1.2f, 1.2f));
        faceMat.SetTexture("_PatternTex", Load(HTex + "facets.png"));
        faceMat.SetFloat("_Aspect", CardW / CardH);
        faceMat.SetFloat("_Corner", 60f / PxH);
        faceMat.SetFloat("_BackHolo", 0.3f);
        // Dựng lại: ghi đè vào material cũ. Xoá rồi tạo lại (SaveMaterial, cùng GUID) thì Face của prefab dựng lại
        // vẫn trỏ vào material đã xoá cho tới khi reimport prefab, mặt thẻ thành màu hồng
        const string matName = "Holo_Face_Lenticular";
        var existing = AssetDatabase.LoadAssetAtPath<Material>(Root + "/Materials/" + matName + ".mat");
        if (existing != null)
        {
            faceMat.name = matName;     // CopySerialized chép cả tên
            EditorUtility.CopySerialized(faceMat, existing);
            EditorUtility.SetDirty(existing);
            AssetDatabase.SaveAssetIfDirty(existing);
            faceMat = existing;
        }
        else SaveMaterial(faceMat, matName);

        var previous = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        UnityEngine.SceneManagement.SceneManager.SetActiveScene(scene);
        CreateEnvironment(background, AssetDatabase.LoadAssetAtPath<VolumeProfile>(VolumePath));

        var root = new GameObject("HoloCard_Lenticular");
        root.AddComponent<BoxCollider>().size = new Vector3(CardW, CardH, 0.1f);
        var pivot = new GameObject("Pivot").transform;
        pivot.SetParent(root.transform, false);
        CreateQuad("Face", pivot, faceMat).localScale = new Vector3(CardW, CardH, 1f);
        AddShadowAndTilt(root, pivot, shadowMat);

        if (!AssetDatabase.IsValidFolder(Root + "/Prefabs")) AssetDatabase.CreateFolder(Root, "Prefabs");
        PrefabUtility.SaveAsPrefabAssetAndConnect(root, LenticularPrefabPath, InteractionMode.AutomatedAction);
        EditorSceneManager.SaveScene(scene, LenticularScenePath);
        AddToBuildSettings(LenticularScenePath);
        if (previous.IsValid()) UnityEngine.SceneManagement.SceneManager.SetActiveScene(previous);
        EditorSceneManager.CloseScene(scene, true);
        Debug.Log("[CardDemo] Built " + LenticularPrefabPath + " + " + LenticularScenePath + " (mở scene này rồi Play để xem)");
    }

    /// Chép material nhân vật có sẵn (giữ màu, queue, pass đã tắt) rồi đổi texture
    static Material CopyCharacterMaterial(string source, string name, Texture2D tex)
    {
        string path = Root + "/Materials/" + name + ".mat";
        if (AssetDatabase.LoadAssetAtPath<Material>(path) == null)
            AssetDatabase.CopyAsset(Root + "/Materials/" + source + ".mat", path);
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        m.SetTexture("_MainTex", tex);
        EditorUtility.SetDirty(m);
        return m;
    }

    /// Camera tự fit thẻ, volume bloom, nền tím phía sau
    static CameraFit CreateEnvironment(Material background, VolumeProfile profile)
    {
        var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
        var cam = camGo.AddComponent<Camera>();
        cam.fieldOfView = 35f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.04f, 0.02f, 0.08f);
        cam.allowHDR = true;
        cam.GetUniversalAdditionalCameraData().renderPostProcessing = true;
        var fit = camGo.AddComponent<CameraFit>();
        fit.width = 3.6f;
        fit.height = 4.4f;

        var vol = new GameObject("Global Volume").AddComponent<Volume>();
        vol.isGlobal = true;
        vol.sharedProfile = profile;

        var bg = CreateQuad("Background", null, background);
        bg.position = new Vector3(0f, 0f, 4f);
        bg.localScale = new Vector3(40f, 40f, 1f);
        return fit;
    }

    static GameObject BuildCard()
    {
        var root = new GameObject("HoloCard");
        root.AddComponent<BoxCollider>().size = new Vector3(CardW, CardH, 0.1f);

        var pivot = new GameObject("Pivot").transform;
        pivot.SetParent(root.transform, false);

        // Mặt thẻ: holo + mặt sau. Lỗ oval cắt trong shader theo HoloCardView
        var faceMat = new Material(Shader.Find("SharderUltimate/HoloCard"));
        faceMat.SetTexture("_MainTex", Load(HTex + "holo_front.png"));
        faceMat.SetTexture("_BackTex", Load(HTex + "holo_back.png"));
        faceMat.SetTexture("_HoloMask", Load(HTex + "holo_mask.png"));
        faceMat.SetTexture("_HoloTex", Load(HTex + "holo_color.png"));
        faceMat.SetTextureScale("_HoloTex", new Vector2(1.2f, 1.2f));
        faceMat.SetTexture("_PatternTex", Load(HTex + "facets.png"));
        faceMat.SetFloat("_Aspect", CardW / CardH);
        faceMat.SetFloat("_Corner", 60f / PxH);
        faceMat.SetFloat("_BackHolo", 0.3f);
        SaveMaterial(faceMat, "Holo_Face");
        var face = CreateQuad("Face", pivot, faceMat);
        face.localScale = new Vector3(CardW, CardH, 1f);

        // 2 tấm poster nổi phía trước mặt thẻ
        CreatePoster(pivot, "PosterTitle", "poster_title", TitleRect, -0.07f);
        CreatePoster(pivot, "PosterText", "poster_text", TextRect, -0.07f);

        var view = root.AddComponent<HoloCardView>();
        view.face = face.GetComponent<Renderer>();
        BuildPortal(pivot, view);

        var shadowMat = new Material(Shader.Find("SharderUltimate/SoftShadow"));
        shadowMat.SetTexture("_MainTex", Load(Tex + "shadow.png"));
        shadowMat.SetColor("_Color", new Color(0f, 0f, 0f, 0.8f));
        SaveMaterial(shadowMat, "Holo_CardShadow");
        AddShadowAndTilt(root, pivot, shadowMat);

        view.character = Load(HTex + "cat_astronaut.png");
        view.spaceTexture = Load(HTex + "space_bg.png");
        view.Rebuild();
        return root;
    }

    /// Bóng mềm dưới thẻ + CardTilt3D (nghiêng theo chuột, bấm để lật) cho thẻ có mặt thẻ nằm dưới Pivot
    static void AddShadowAndTilt(GameObject root, Transform pivot, Material shadowMat)
    {
        var shadowPivot = new GameObject("ShadowPivot").transform;
        shadowPivot.SetParent(root.transform, false);
        shadowPivot.localPosition = new Vector3(0.15f, -0.2f, 0.8f);
        CreateQuad("Shadow", shadowPivot, shadowMat).localScale = new Vector3(CardW * 1.9f, CardH * 1.55f, 1f);

        // cam để trống: prefab không giữ được tham chiếu tới camera trong scene, CardTilt3D tự lấy Camera.main
        var tilt = root.AddComponent<CardTilt3D>();
        tilt.visual = pivot;
        tilt.shadow = shadowPivot;
        tilt.maxTilt = 28f;
        tilt.idleTilt = 12f;
        tilt.hoverLift = 0.4f;
        tilt.hoverScale = 1.04f;
    }

    static void CreatePoster(Transform parent, string name, string tex, Rect px, float z)
    {
        var m = new Material(Shader.Find("SharderUltimate/HoloCard"));
        m.SetTexture("_MainTex", Load(HTex + tex + ".png"));
        m.SetFloat("_HoloStrength", 0f);
        m.SetFloat("_Glare", 0.5f);
        m.SetFloat("_Cull", (float)CullMode.Back);
        SaveMaterial(m, "Holo_" + name);

        var t = CreateQuad(name, parent, m);
        t.localPosition = new Vector3((px.center.x / PxW - 0.5f) * CardW, (0.5f - px.center.y / PxH) * CardH, z);
        t.localScale = new Vector3(px.width / PxW * CardW, px.height / PxH * CardH, 1f);
    }

    /// Cửa sổ portal: mask ghi stencil, phía sau là một môi trường 3D (gờ kim loại, nền tinh vân ở xa,
    /// sao particle phân bố trong khối, nhân vật nổi khối). Mesh do HoloCardView sinh theo kích thước oval.
    static void BuildPortal(Transform pivot, HoloCardView view)
    {
        var portal = new GameObject("Portal").transform;
        portal.SetParent(pivot, false);
        view.portal = portal;

        var maskMat = new Material(Shader.Find("SharderUltimate/PortalMask"));
        maskMat.SetInt("_StencilRef", StencilRef);
        maskMat.renderQueue = QueueMask;
        SaveMaterial(maskMat, "Holo_PortalMask");
        view.portalMask = CreateMeshObject("PortalMask", portal, maskMat, -0.002f);

        var resetMat = new Material(Shader.Find("SharderUltimate/PortalDepthReset"));
        resetMat.SetInt("_StencilRef", StencilRef);
        resetMat.renderQueue = QueueDepthReset;
        SaveMaterial(resetMat, "Holo_PortalDepthReset");
        var restoreMat = new Material(Shader.Find("SharderUltimate/PortalDepthRestore"));
        restoreMat.SetInt("_StencilRef", StencilRef);
        restoreMat.renderQueue = QueueDepthRestore;
        SaveMaterial(restoreMat, "Holo_PortalDepthRestore");
        // 1 renderer, 2 material: reset (trước nội dung) và khôi phục (sau nội dung), cùng dùng mesh phủ cửa sổ
        view.portalDepthReset = CreateMeshObject("PortalDepthReset", portal, resetMat, -0.002f);
        view.portalDepthReset.GetComponent<MeshRenderer>().sharedMaterials = new[] { resetMat, restoreMat };

        // Gờ kim loại ngắn quanh cửa sổ: vàng ở miệng, tối dần vào trong
        var bezelMat = PortalMaterial("Holo_PortalBezel", null, Color.white, BlendMode.One, BlendMode.Zero, true, QueueBezel, vertexColor: true);
        view.bezel = CreateMeshObject("Bezel", portal, bezelMat, 0f);

        // Nền không gian: mái vòm do HoloCardView sinh, đặt xa để khi nghiêng thẻ thị sai lớn như nhìn ra vũ trụ thật
        var spaceMat = PortalMaterial("Holo_Space", "space_bg", new Color(0.75f, 0.75f, 0.85f), BlendMode.One, BlendMode.Zero, true, QueueSpace);
        view.spaceBackdrop = CreateMeshObject("SpaceBackdrop", portal, spaceMat, 0f).GetComponent<Renderer>();

        BuildStarField(portal);

        // Nhân vật: ảnh phẳng bán trong suốt, trôi lơ lửng sau cửa sổ (vị trí chỉnh trên Transform).
        // Shader PortalPopOut: bật popOut trên HoloCardView thì nhân vật tràn được ra khỏi cửa sổ
        // Shader có 2 pass, tách ra 2 material để pass pop-out vẽ sau khi mọi cửa sổ đã khôi phục depth
        var catMat = CharacterMaterial("Holo_Cat", QueueCharacter, "SRPDefaultUnlit");
        var catPopMat = CharacterMaterial("Holo_CatPopOut", QueuePopOut, "UniversalForward");
        view.characterMesh = CreateMeshObject("Character", portal, catMat, 0.55f);
        view.characterMesh.GetComponent<MeshRenderer>().sharedMaterials = new[] { catMat, catPopMat };

        // Bóng tối mờ sát mép kính, shader tính theo khoảng cách tới viền cửa sổ + viền thẻ
        var vigMat = new Material(Shader.Find("SharderUltimate/PortalVignette"));
        vigMat.SetInt("_StencilRef", StencilRef);
        vigMat.renderQueue = QueueVignette;
        SaveMaterial(vigMat, "Holo_PortalVignette");
        view.vignette = CreateMeshObject("Vignette", portal, vigMat, 0.002f);
    }

    /// Sao là particle rải trong một khối hộp sâu 4 đơn vị, mỗi ngôi sao có độ sâu riêng nên thị sai liên tục
    static void BuildStarField(Transform portal)
    {
        var mat = PortalMaterial("Holo_StarParticle", "star_dot", Color.white * 2.5f, BlendMode.One, BlendMode.One, false, QueueStars, vertexColor: true);

        var go = new GameObject("StarField");
        go.transform.SetParent(portal, false);
        go.transform.localPosition = new Vector3(0f, 0f, 2.3f);
        var ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = ps.main;
        main.loop = true;
        main.prewarm = true;
        main.duration = 20f;
        main.startLifetime = 20f;
        main.startSpeed = 0.015f;
        main.startSize = new ParticleSystem.MinMaxCurve(0.02f, 0.07f);
        main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.75f, 0.85f, 1f), new Color(1f, 0.8f, 0.95f));
        main.maxParticles = 600;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;

        var emission = ps.emission;
        emission.rateOverTime = 30f;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(5f, 5.5f, 4f);

        // Additive nên fade bằng màu: đen -> sáng -> đen
        var fade = new Gradient();
        fade.SetKeys(
            new[] { new GradientColorKey(Color.black, 0f), new GradientColorKey(Color.white, 0.1f), new GradientColorKey(Color.white, 0.9f), new GradientColorKey(Color.black, 1f) },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) });
        var col = ps.colorOverLifetime;
        col.enabled = true;
        col.color = fade;

        var r = go.GetComponent<ParticleSystemRenderer>();
        r.sharedMaterial = mat;
        r.renderMode = ParticleSystemRenderMode.Billboard;
        r.shadowCastingMode = ShadowCastingMode.Off;
    }

    /// Material nhân vật (shader PortalPopOut) chỉ chạy 1 trong 2 pass: tắt pass có LightMode disabledPass
    static Material CharacterMaterial(string name, int queue, string disabledPass)
    {
        var m = new Material(Shader.Find("SharderUltimate/PortalPopOut"));
        m.SetTexture("_MainTex", Load(HTex + "cat_astronaut.png"));
        m.SetColor("_Color", new Color(1.15f, 1.15f, 1.3f, 1f));
        m.SetInt("_StencilRef", StencilRef);
        m.SetShaderPassEnabled(disabledPass, false);
        m.renderQueue = queue;
        return SaveMaterial(m, name);
    }

    /// Mọi thuộc tính phải gán trước SaveMaterial, gán sau CreateAsset sẽ không được ghi xuống file .mat
    static Material PortalMaterial(string name, string tex, Color color, BlendMode src, BlendMode dst, bool zwrite, int queue,
        bool vertexColor = false, float tiling = 1f)
    {
        var m = new Material(Shader.Find("SharderUltimate/PortalContent"));
        if (tex != null) m.SetTexture("_MainTex", Load(HTex + tex + ".png"));
        m.SetTextureScale("_MainTex", Vector2.one * tiling);
        m.SetColor("_Color", color);
        m.SetInt("_StencilRef", StencilRef);
        m.SetFloat("_SrcBlend", (float)src);
        m.SetFloat("_DstBlend", (float)dst);
        m.SetFloat("_ZWrite", zwrite ? 1f : 0f);
        m.SetFloat("_UseVertexColor", vertexColor ? 1f : 0f);
        m.renderQueue = queue;
        return SaveMaterial(m, name);
    }

    static MeshFilter CreateMeshObject(string name, Transform parent, Material mat, float z)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = new Vector3(0f, 0f, z);
        var r = go.AddComponent<MeshRenderer>();
        r.sharedMaterial = mat;
        r.shadowCastingMode = ShadowCastingMode.Off;
        return go.AddComponent<MeshFilter>();
    }
}
