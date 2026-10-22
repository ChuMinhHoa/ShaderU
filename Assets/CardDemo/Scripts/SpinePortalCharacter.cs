using Spine.Unity;
using UnityEngine;

/// Nhân vật Spine trong cửa sổ portal của HoloCardView, thay cho quad ảnh phẳng (gắn cùng SkeletonAnimation lên
/// object Character, bật HoloCardView.externalCharacterMesh).
/// - Spine tự gán material của atlas lên MeshRenderer. Component này thay bằng cặp material của thẻ: [0] pass Inside
///   (chỉ vẽ trong stencil cửa sổ), [1] pass PopOut (material thừa -> Unity vẽ lại mesh lần 2, phần tràn ra khung).
/// - Kích thước theo HoloCardView.characterSize như quad: SkeletonData scale = 1 / cạnh dài ảnh (skeleton dài 1 đơn vị),
///   skeleton được nhân thêm characterSize.
/// - Play mode: material instance riêng, stencil ref lấy từ thẻ (mỗi thẻ một ref, xem HoloCardView.AssignStencilRef).
[ExecuteAlways]
[DefaultExecutionOrder(-10)]   // đặt scale trước SkeletonAnimation.Update tính world transform
[RequireComponent(typeof(SkeletonRenderer))]
public class SpinePortalCharacter : MonoBehaviour
{
    [Tooltip("Material pass Inside của PortalPopOut (giống Holo_Cat), _MainTex là texture atlas Spine")]
    public Material insideMaterial;
    [Tooltip("Material pass PopOut của PortalPopOut (giống Holo_CatPopOut), _MainTex là texture atlas Spine")]
    public Material popOutMaterial;

    [Header("Anim")]
    public string idleAnimation = "idle";
    public string touchAnimation = "touch";
    [Tooltip("Play mode: tự chơi anim touch mỗi N giây để xem thử (0 = tắt). Gọi PlayTouch() khi cần phản hồi chạm")]
    public float autoTouchEvery;

    SkeletonRenderer skeletonRenderer;
    float nextAutoTouch;
    MeshRenderer meshRenderer;
    HoloCardView card;
    Material[] materials;

    void OnEnable()
    {
        skeletonRenderer = GetComponent<SkeletonRenderer>();
        meshRenderer = GetComponent<MeshRenderer>();
        card = GetComponentInParent<HoloCardView>();
        skeletonRenderer.OnRebuild += ApplySize;
        skeletonRenderer.OnMeshAndMaterialsUpdated += ApplyMaterials;
        ApplySize(skeletonRenderer);
        ApplyMaterials(skeletonRenderer);
    }

    void OnDisable()
    {
        skeletonRenderer.OnRebuild -= ApplySize;
        skeletonRenderer.OnMeshAndMaterialsUpdated -= ApplyMaterials;
    }

#if UNITY_EDITOR
    // Đổi material trong Inspector (edit mode) thì gán lại
    void OnValidate() { if (!Application.isPlaying) materials = null; }
#endif

    void Start()
    {
        // Stencil ref do HoloCardView chọn trong OnEnable, lúc này mọi OnEnable đã chạy xong
        if (!Application.isPlaying || materials == null || card == null) return;
        foreach (var m in materials) m.SetInt("_StencilRef", card.StencilRef);
    }

    void OnDestroy()
    {
        if (!Application.isPlaying || materials == null) return;
        foreach (var m in materials) Destroy(m);
    }

    void Update()
    {
        // characterSize đổi trong Inspector thì cập nhật luôn, không cần dựng lại skeleton
        var skeleton = skeletonRenderer.skeleton;
        if (skeleton != null && card != null && skeleton.ScaleY != card.characterSize) ApplySize(skeletonRenderer);

        if (Application.isPlaying && autoTouchEvery > 0f && Time.time >= nextAutoTouch)
        {
            if (nextAutoTouch > 0f) PlayTouch();
            nextAutoTouch = Time.time + autoTouchEvery;
        }
    }

    /// Phản hồi chạm: chơi touch một lần rồi về idle lặp
    [ContextMenu("Play Touch")]
    public void PlayTouch()
    {
        var anim = skeletonRenderer as SkeletonAnimation;
        if (anim == null || anim.AnimationState == null) return;
        anim.AnimationState.SetAnimation(0, touchAnimation, false);
        anim.AnimationState.AddAnimation(0, idleAnimation, true, 0f);
    }

    void ApplySize(SkeletonRenderer r)
    {
        if (r.skeleton == null || card == null) return;
        r.skeleton.ScaleX = r.skeleton.ScaleY = card.characterSize;
        r.skeleton.UpdateWorldTransform(Spine.Skeleton.Physics.Update);
    }

    /// Spine gán lại material atlas mỗi khi tam giác / material đổi (2 frame đầu do mesh double-buffer, đổi attachment)
    void ApplyMaterials(SkeletonRenderer r)
    {
        if (insideMaterial == null || popOutMaterial == null || meshRenderer == null) return;
        if (materials == null)
        {
            materials = Application.isPlaying
                ? new[] { new Material(insideMaterial), new Material(popOutMaterial) }
                : new[] { insideMaterial, popOutMaterial };
            if (Application.isPlaying && card != null)
                foreach (var m in materials) m.SetInt("_StencilRef", card.StencilRef);
        }
        var current = meshRenderer.sharedMaterials;
        if (current.Length == 2 && current[0] == materials[0] && current[1] == materials[1]) return;
        meshRenderer.sharedMaterials = materials;
    }
}
