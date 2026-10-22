using System.Collections.Generic;
using UnityEngine;

/// Thẻ holo có cửa sổ portal 3D. Những thứ hay đổi đều nằm ở đây, chỉnh trong Inspector là thẻ tự dựng lại
/// (cả Edit mode lẫn Prefab mode):
/// - Cửa sổ: hình dạng (elip / chữ nhật / đa giác), vị trí, kích thước. Lỗ + viền vàng cắt trong shader HoloCard.
/// - Nhân vật: PNG nền trong suốt, ảnh phẳng đặt ở độ sâu riêng sau cửa sổ.
/// - Không gian: mái vòm phía sau cửa sổ, ảnh tự phủ kín và giữ tỉ lệ.
[ExecuteAlways]
public class HoloCardView : MonoBehaviour
{
    public const float Width = 2.5f, Height = 3.5f;

    public enum WindowShape { Ellipse, Rectangle, Polygon }

    [Header("Cửa sổ portal")]
    public WindowShape shape = WindowShape.Ellipse;
    [Tooltip("Tâm cửa sổ theo UV mặt thẻ (0..1)")]
    public Vector2 windowCenter = new Vector2(0.5f, 0.5595f);
    [Tooltip("Rộng x cao, tính theo bề ngang thẻ (1 = rộng bằng thẻ). Hai số bằng nhau = vuông / tròn")]
    public Vector2 windowSize = new Vector2(0.597f, 0.728f);
    [Tooltip("Bo góc cho Rectangle và Polygon, tính theo bề ngang thẻ. 0 = góc sắc")]
    [Range(0f, 0.5f)] public float cornerRadius = 0.04f;
    [Tooltip("Số cạnh khi Shape = Polygon: 3 tam giác, 4 kim cương (xoay 45 = vuông), 6 lục giác...")]
    [Range(3, 12)] public int polygonSides = 6;
    [Range(-180f, 180f)] public float polygonRotation = 0f;
    [Tooltip("Độ dày viền vàng, tính theo bề ngang thẻ")]
    [Range(0f, 0.08f)] public float ringWidth = 0.022f;
    public Color ringColor = new Color(1f, 0.85f, 0.5f);
    [Range(0f, 0.3f)] public float bezelDepth = 0.07f;
    [Tooltip("Độ rộng dải bóng tối sát mép cửa sổ, tính theo bề ngang thẻ")]
    [Range(0f, 0.3f)] public float edgeShadowWidth = 0.09f;
    [Range(0f, 1f)] public float edgeShadowStrength = 0.8f;

    // Vị trí / xoay / scale của Character, SpaceBackdrop, StarField chỉnh trực tiếp trên Transform của chúng
    // (con của Portal, gốc tại tâm cửa sổ, +Z là lùi sâu vào trong thẻ). Component không ghi đè.
    [Header("Nhân vật (PNG nền trong suốt)")]
    public Texture2D character;
    [Tooltip("Cạnh dài nhất của ảnh, theo đơn vị thẻ (thẻ rộng 2.5)")]
    public float characterSize = 1.45f;
    [Tooltip("Mesh nhân vật do component khác sinh (vd Spine, xem SpinePortalCharacter): không dựng quad, không gán texture. " +
             "Character vẫn cần ảnh gốc để vùng pop-out trong Scene view khớp khung ảnh")]
    public bool externalCharacterMesh;

    [Header("Nhân vật nổi ra ngoài (pop-out)")]
    [Tooltip("Phần nhân vật phía trên đường cắt được vẽ đè lên mặt thẻ, tràn ra khỏi cửa sổ. " +
             "Đặt Character ở Position Z âm (nhô ra trước mặt thẻ) để khi nghiêng thấy nổi hẳn lên")]
    public bool popOut;
    [Tooltip("Chỉ dùng khi chưa có Pop Out Zones / Pop Out Mask. Đường cắt ngang, theo nửa chiều cao cửa sổ: " +
             "-1 = đáy, 0 = giữa, 1 = đỉnh. Phần nhân vật nằm ngoài cửa sổ mà thấp hơn đường này bị ẩn " +
             "(như bị mép dưới khung che). -1.5 = không cắt gì")]
    [Range(-1.5f, 1.5f)] public float popOutLine = -1f;
    [Tooltip("Độ mờ dần tại đường cắt, theo bề ngang thẻ")]
    [Range(0f, 0.2f)] public float popOutFeather = 0.02f;
    [Tooltip("Các vùng elip trên ảnh nhân vật được tràn ra ngoài (tối đa 4), phần ngoài vùng chỉ hiện trong cửa sổ. " +
             "Chọn thẻ để kéo vùng trực tiếp trong Scene view. Để trống = cả nhân vật được tràn ra")]
    public List<PopOutZone> popOutZones = new List<PopOutZone>();
    [Tooltip("Ảnh mask cùng khung với ảnh nhân vật: tô trắng phần được tràn ra ngoài (đầu, tay...), " +
             "phần đen chỉ hiện trong cửa sổ. Dùng kèm được với Pop Out Zones. " +
             "Tạo mẫu: Tools > Card Demo > Create Pop-Out Mask Template")]
    public Texture2D popOutMask;

    /// Vùng elip theo UV ảnh nhân vật (0..1, gốc dưới-trái)
    [System.Serializable]
    public struct PopOutZone
    {
        public Vector2 center;
        [Tooltip("Bán kính theo chiều ngang / dọc của ảnh (0..1)")]
        public Vector2 radius;
        [Tooltip("Dải mờ ở mép vùng, theo tỉ lệ bán kính: 0.01 = cắt gọn, 1 = mờ dần từ tâm ra. " +
                 "Trong Scene view là khoảng giữa elip ngoài và elip trong (nét mờ)")]
        [Range(0.01f, 1f)] public float softness;
    }

    public const int MaxPopOutZones = 4;
    public const float DefaultZoneSoftness = 0.25f;
    readonly Vector4[] zoneData = new Vector4[MaxPopOutZones];

    /// Vùng lưu từ trước khi có softness riêng sẽ đọc ra 0 -> dùng mặc định
    public static float ZoneSoftness(PopOutZone zone) => zone.softness > 0f ? zone.softness : DefaultZoneSoftness;

    [Header("Không gian")]
    public Texture2D spaceTexture;
    public Color spaceTint = new Color(0.75f, 0.75f, 0.85f);
    [Tooltip("Bán kính mái vòm nền: càng xa thì thị sai khi nghiêng càng mạnh")]
    [Range(2f, 15f)] public float spaceDistance = 6f;
    [Tooltip("1 = ảnh vừa khít phần nhìn thấy khi nghiêng tối đa (tự tính theo kích thước cửa sổ). " +
             "Lớn hơn 1 phóng to ảnh, nhỏ hơn 1 thấy nhiều ảnh hơn nhưng nghiêng mạnh có thể chạm mép ảnh")]
    [Range(0.5f, 3f)] public float spaceZoom = 1f;

    [Header("Tham chiếu (builder gán sẵn)")]
    public Renderer face;
    public Transform portal;
    public MeshFilter portalMask, portalDepthReset, bezel, vignette, characterMesh;
    public Renderer spaceBackdrop;

    static readonly Color BezelRim = new Color(0.85f, 0.68f, 0.36f);
    static readonly Color BezelDeep = new Color(0.1f, 0.07f, 0.04f);
    static int nextStencil;

    /// Stencil ref của thẻ này (play mode, chọn trong OnEnable). Renderer tự thay material (Spine) dùng lại giá trị này
    public int StencilRef { get; private set; } = 1;

    readonly Mesh[] generated = new Mesh[4];   // 0 phủ cửa sổ, 1 gờ, 2 nhân vật, 3 mái vòm
    readonly List<Material> stencilMaterials = new List<Material>();
    MaterialPropertyBlock mpb;
    Vector3 domePosition, domeScale;   // Transform mái vòm lúc dựng gần nhất, đổi thì tính lại độ phủ ảnh
    Vector2 windowHalf;
    float windowCorner;

    void OnEnable()
    {
        if (Application.isPlaying) AssignStencilRef();
        Rebuild();
    }

    void OnDisable()
    {
        for (int i = 0; i < generated.Length; i++) DestroySafe(generated[i]);
    }

    void OnDestroy()
    {
        foreach (var m in stencilMaterials) DestroySafe(m);
    }

    void Update()
    {
        // Kéo Transform của SpaceBackdrop không gọi OnValidate, tự phát hiện để tính lại độ phủ ảnh nền
        if (spaceBackdrop == null) return;
        var t = spaceBackdrop.transform;
        if (t.localPosition != domePosition || t.localScale != domeScale) Rebuild();
    }

    void LateUpdate()
    {
        // Thẻ nghiêng / nhân vật trôi mỗi frame nên ma trận không gian cửa sổ phải cập nhật liên tục
        if (popOut) ApplyPopOut();
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        // Vùng mới thêm (toàn 0) -> đặt sẵn ở nửa trên ảnh, chỗ thường là đầu nhân vật.
        // Vùng cũ chưa có softness -> gán mặc định để Inspector hiện đúng giá trị đang dùng
        for (int i = 0; popOutZones != null && i < popOutZones.Count; i++)
        {
            var zone = popOutZones[i];
            if (zone.radius == Vector2.zero)
                zone = new PopOutZone { center = new Vector2(0.5f, 0.72f), radius = new Vector2(0.3f, 0.25f) };
            zone.softness = ZoneSoftness(zone);
            popOutZones[i] = zone;
        }

        // Không được tạo mesh/đổi renderer ngay trong OnValidate, dời sang frame editor kế tiếp
        UnityEditor.EditorApplication.delayCall += () => { if (this != null && isActiveAndEnabled) Rebuild(); };
    }
#endif

    public void Rebuild()
    {
        // Prefab asset trong Project (chưa nằm trong scene nào) thì bỏ qua
        if (face == null || portal == null || !gameObject.scene.IsValid()) return;
        mpb ??= new MaterialPropertyBlock();

        // Mọi kích thước đổi sang đơn vị thẻ (thẻ rộng Width)
        var half = windowSize * Width * 0.5f;
        float corner = cornerRadius * Width;
        windowHalf = half;
        windowCorner = corner;
        portal.localPosition = new Vector3((windowCenter.x - 0.5f) * Width, (windowCenter.y - 0.5f) * Height, 0f);

        face.GetPropertyBlock(mpb);
        mpb.SetVector("_CardSize", new Vector4(Width, Height, 0f, 0f));
        mpb.SetVector("_WindowCenter", new Vector4(windowCenter.x, windowCenter.y, 0f, 0f));
        mpb.SetVector("_WindowShape", new Vector4(half.x, half.y, corner, (float)shape));
        mpb.SetVector("_WindowPoly", new Vector4(polygonSides, polygonRotation * Mathf.Deg2Rad, 0f, 0f));
        mpb.SetFloat("_RingWidth", ringWidth * Width);
        mpb.SetColor("_RingColor", ringColor);
        face.SetPropertyBlock(mpb);

        var contour = WindowContour(half, corner);
        // Mesh phủ cửa sổ dùng chung cho mask, reset depth và bóng mép; shader cắt chính xác theo cửa sổ + viền thẻ
        SetMesh(0, portalMask, PortalMeshes.Fan(PortalMeshes.Offset(contour, 0.004f)));
        if (portalDepthReset != null) portalDepthReset.sharedMesh = generated[0];
        if (vignette != null) vignette.sharedMesh = generated[0];
        SetMesh(1, bezel, PortalMeshes.Tube(contour, bezelDepth, BezelRim, BezelDeep));

        // Viền thẻ tính từ tâm cửa sổ, để mask và bóng mép không tràn ra ngoài góc bo của thẻ
        float cardCorner = face.sharedMaterial != null ? face.sharedMaterial.GetFloat("_Corner") * Height : 0f;
        var cardRect = new Vector4(-portal.localPosition.x, -portal.localPosition.y, cardCorner, 0f);
        var cardHalf = new Vector4(Width * 0.5f, Height * 0.5f, 0f, 0f);
        foreach (var filter in new[] { portalMask, vignette })
        {
            if (filter == null) continue;
            var r = filter.GetComponent<Renderer>();
            r.GetPropertyBlock(mpb);
            mpb.SetVector("_WindowShape", new Vector4(half.x, half.y, corner, (float)shape));
            mpb.SetVector("_WindowPoly", new Vector4(polygonSides, polygonRotation * Mathf.Deg2Rad, 0f, 0f));
            mpb.SetVector("_CardRect", cardRect);
            mpb.SetVector("_CardHalf", cardHalf);
            mpb.SetVector("_Vignette", new Vector4(edgeShadowWidth * Width, edgeShadowStrength, 0f, 0f));
            r.SetPropertyBlock(mpb);
        }

        if (character != null && characterMesh != null && !externalCharacterMesh)
        {
            SetMesh(2, characterMesh, PortalMeshes.Quad(character, characterSize));
            var r = characterMesh.GetComponent<Renderer>();
            r.GetPropertyBlock(mpb);
            mpb.SetTexture("_MainTex", character);
            r.SetPropertyBlock(mpb);
        }
        ApplyPopOut();

        if (spaceBackdrop != null)
        {
            SetMesh(3, spaceBackdrop.GetComponent<MeshFilter>(), PortalMeshes.Dome(spaceDistance, SpaceUvScale(contour)));
            domePosition = spaceBackdrop.transform.localPosition;
            domeScale = spaceBackdrop.transform.localScale;

            spaceBackdrop.GetPropertyBlock(mpb);
            if (spaceTexture != null) mpb.SetTexture("_MainTex", spaceTexture);
            mpb.SetColor("_Color", spaceTint);
            mpb.SetVector("_MainTex_ST", new Vector4(1f, 1f, 0f, 0f));
            spaceBackdrop.SetPropertyBlock(mpb);
        }
    }

    /// Thông số cho pass PopOut của shader PortalPopOut: hình cửa sổ, đường cắt, vùng, mask và ma trận không gian cửa sổ
    public void ApplyPopOut()
    {
        if (characterMesh == null || portal == null) return;
        var r = characterMesh.GetComponent<Renderer>();
        mpb ??= new MaterialPropertyBlock();
        r.GetPropertyBlock(mpb);
        mpb.SetVector("_WindowShape", new Vector4(windowHalf.x, windowHalf.y, windowCorner, (float)shape));
        mpb.SetVector("_WindowPoly", new Vector4(polygonSides, polygonRotation * Mathf.Deg2Rad, 0f, 0f));
        int zones = popOutZones != null ? Mathf.Min(popOutZones.Count, MaxPopOutZones) : 0;
        var softness = Vector4.zero;
        for (int i = 0; i < MaxPopOutZones; i++)
        {
            zoneData[i] = i < zones ? new Vector4(popOutZones[i].center.x, popOutZones[i].center.y, popOutZones[i].radius.x, popOutZones[i].radius.y) : Vector4.zero;
            if (i < zones) softness[i] = ZoneSoftness(popOutZones[i]);
        }
        // Đã chọn vùng hoặc mask thì chỉ theo đó, bỏ đường cắt (nếu không chân / phần dưới khung bị cắt dù nằm trong vùng)
        float line = zones > 0 || popOutMask != null ? -1e4f : popOutLine * windowHalf.y;
        mpb.SetVector("_PopOut", new Vector4(line, popOutFeather * Width, popOut ? 1f : 0f, zones));
        mpb.SetVectorArray("_PopZones", zoneData);   // luôn đủ 4 phần tử: Unity cố định độ dài mảng ở lần set đầu
        mpb.SetVector("_PopZoneSoftness", softness);
        mpb.SetTexture("_PopMask", popOutMask != null ? popOutMask : Texture2D.whiteTexture);
        mpb.SetMatrix("_PortalWorldToLocal", portal.worldToLocalMatrix);
        r.SetPropertyBlock(mpb);
    }

    /// Khung ảnh nhân vật trong không gian Character: tâm tại gốc, cạnh dài = characterSize (khớp PortalMeshes.Quad).
    /// UV 0..1 của ảnh trải trên khung này, kể cả khi mesh do Spine sinh (mesh chỉ phủ phần có hình)
    public Rect CharacterRect()
    {
        float aspect = character != null ? (float)character.width / character.height : 1f;
        float w = characterSize * Mathf.Min(1f, aspect), h = characterSize * Mathf.Min(1f, 1f / aspect);
        return new Rect(-w * 0.5f, -h * 0.5f, w, h);
    }

    /// Độ phủ UV (theo từng trục) của mái vòm sao cho phần nhìn thấy qua cửa sổ ở góc nghiêng tối đa
    /// nằm gọn trong ảnh, và ảnh giữ đúng tỉ lệ (kiểu "cover").
    /// Tia đi qua mép cửa sổ (cách tâm reach) với góc nghiêng tilt chạm vòm ở góc khoảng tilt + reach / R,
    /// phép chiếu stereographic đưa góc đó về tan(góc / 2), giữ UV trong 0.5 ± 0.48.
    Vector2 SpaceUvScale(List<Vector2> contour)
    {
        // Phần cửa sổ tràn ra ngoài thẻ bị mask cắt nên không cần phủ
        var limit = new Vector2(Width * 0.5f + Mathf.Abs(portal.localPosition.x), Height * 0.5f + Mathf.Abs(portal.localPosition.y));
        var reach = Vector2.zero;
        foreach (var p in contour) reach = Vector2.Max(reach, new Vector2(Mathf.Abs(p.x), Mathf.Abs(p.y)));
        reach = Vector2.Min(reach, limit);

        // Mái vòm có thể bị dịch / scale qua Transform: tính từ tâm vòm và bán kính thực
        var dome = spaceBackdrop.transform;
        reach += new Vector2(Mathf.Abs(dome.localPosition.x), Mathf.Abs(dome.localPosition.y));
        float radius = spaceDistance * Mathf.Min(Mathf.Abs(dome.localScale.x), Mathf.Abs(dome.localScale.y)) + dome.localPosition.z;
        radius = Mathf.Max(radius, 0.5f);

        var tilt = GetComponent<CardTilt3D>();
        float maxTilt = ((tilt != null ? tilt.maxTilt : 28f) + 10f) * Mathf.Deg2Rad;   // cộng thêm cho lắc idle và lệch góc camera
        float tanX = Mathf.Tan(Mathf.Min(maxTilt + reach.x / radius, 1.5f) * 0.5f);
        float tanY = Mathf.Tan(Mathf.Min(maxTilt + reach.y / radius, 1.5f) * 0.5f);

        // Giữ tỉ lệ ảnh: kx * rộng = ky * cao (cùng số pixel cho mỗi đơn vị theo 2 trục)
        Texture tex = spaceTexture;
        if (tex == null && spaceBackdrop.sharedMaterial != null) tex = spaceBackdrop.sharedMaterial.GetTexture("_MainTex");
        float aspect = tex != null ? (float)tex.width / tex.height : 1f;
        float ky = Mathf.Min(0.48f / tanY, 0.48f * aspect / tanX) / spaceZoom;
        return new Vector2(ky / aspect, ky);
    }

    /// Viền cửa sổ theo shape, khớp với WindowDistance trong HoloCard.shader
    List<Vector2> WindowContour(Vector2 half, float corner)
    {
        switch (shape)
        {
            case WindowShape.Rectangle:
                return PortalMeshes.RoundedRectContour(half, corner);
            case WindowShape.Polygon:
                float roundness = Mathf.Clamp(corner / Mathf.Min(half.x, half.y), 0f, 0.9f);
                return PortalMeshes.PolygonContour(half, polygonSides, polygonRotation, roundness);
            default:
                return PortalMeshes.EllipseContour(half);
        }
    }

    /// Mỗi thẻ cần stencil ref riêng, nếu không nội dung portal của thẻ này sẽ lộ qua cửa sổ của thẻ khác.
    /// Stencil ref là render state nên phải dùng material instance, không dùng được MaterialPropertyBlock.
    void AssignStencilRef()
    {
        if (portal == null) return;
        int stencil = nextStencil++ % 255 + 1;
        StencilRef = stencil;
        foreach (var r in portal.GetComponentsInChildren<Renderer>(true))
        {
            // play mode: tạo instance riêng cho mọi material của renderer (Character, PortalDepthReset có 2 material)
            foreach (var m in r.materials)
            {
                m.SetInt("_StencilRef", stencil);
                stencilMaterials.Add(m);
            }
        }
    }

    void SetMesh(int slot, MeshFilter filter, Mesh mesh)
    {
        DestroySafe(generated[slot]);
        mesh.hideFlags = HideFlags.DontSave;   // sinh lại mỗi lần bật, không lưu vào scene/prefab
        generated[slot] = mesh;
        if (filter != null) filter.sharedMesh = mesh;
    }

    static void DestroySafe(Object o)
    {
        if (o == null) return;
        if (Application.isPlaying) Destroy(o);
        else DestroyImmediate(o);
    }
}
