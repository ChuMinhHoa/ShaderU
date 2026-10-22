using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// Card 3D: nghiêng theo chuột, nhấc lên khi hover, click để lật.
/// Root giữ collider đứng yên, child "visual" mới xoay để raycast ổn định.
[RequireComponent(typeof(BoxCollider))]
public class CardTilt3D : MonoBehaviour
{
    public Transform visual;
    public Transform shadow;
    public Camera cam;

    [Header("Tilt")]
    public float maxTilt = 20f;
    public float tiltSmooth = 10f;

    [Header("Hover")]
    public float hoverLift = 0.6f;
    public float hoverScale = 1.08f;
    public float hoverSmooth = 10f;

    [Header("Flip")]
    public float flipDuration = 0.55f;
    public float flipArc = 1.2f;

    [Header("Idle")]
    public float idleAmplitude = 0.08f;
    public float idleTilt = 6f;

    BoxCollider box;
    Vector2 tilt, tiltTarget;
    float hover;
    bool flipped;
    float flipT = 1f;
    float flipFrom, flipTo;
    float idleSeed;
    Material shadowMat;
    Vector3 shadowBasePos;
    Color shadowBaseColor;
    Material faceMat;
    // Material HoloCard trên mặt thẻ và poster: nhận điểm chạm / hover (Ice nứt, Glitter rọi đèn, Hologram glitch)
    readonly List<Material> hitMats = new List<Material>();
    // uv điểm chạm trên mặt thẻ, tuổi cú chạm (giây, < 0 = chưa chạm), mức hover
    Vector2 iceHitUv = new Vector2(0.5f, 0.5f), iceHoverUv = new Vector2(0.5f, 0.5f);
    float iceAge = -1f, iceHover;

    static readonly int ActiveId = Shader.PropertyToID("_Active");
    static readonly int FxTimeOffsetId = Shader.PropertyToID("_FxTimeOffset");
    static readonly int IceHitId = Shader.PropertyToID("_IceHit");
    static readonly int IceHoverId = Shader.PropertyToID("_IceHover");
    static readonly int BurstId = Shader.PropertyToID("_Burst");

    [Header("Element Fx")]
    [Tooltip("Hiệu ứng nguyên tố chạy nhanh hơn bao nhiêu lần khi active")]
    public float activeSpeedBoost = 1.8f;
    [Tooltip("Thời gian (giây) của cú bùng cháy lúc vừa kích hoạt")]
    public float burstDuration = 0.8f;
    [Tooltip("Lúc bùng, hiệu ứng chạy nhanh thêm bao nhiêu lần")]
    public float burstSpeedBoost = 1.2f;
    float active, fxExtraTime;
    float burstT = 1f;
    bool wasHovered;

    void Awake()
    {
        box = GetComponent<BoxCollider>();
        // Shader nguyên tố (nếu có): idle -> active khi rê chuột hoặc đang lật
        var faceRenderer = visual != null ? visual.GetComponentInChildren<Renderer>() : null;
        if (faceRenderer != null && faceRenderer.sharedMaterial.HasProperty(ActiveId))
            faceMat = faceRenderer.material;
        if (visual != null)
            foreach (var r in visual.GetComponentsInChildren<Renderer>())
                if (r.sharedMaterial != null && r.sharedMaterial.HasProperty(IceHitId))
                    hitMats.Add(r.material);
        if (cam == null) cam = Camera.main;
        idleSeed = Random.value * 10f;
        if (shadow != null)
        {
            shadowBasePos = shadow.localPosition;
            shadowMat = shadow.GetComponentInChildren<Renderer>().material;
            shadowBaseColor = shadowMat.GetColor("_Color");
        }
    }

    void Update()
    {
        bool hovered = false;
        Vector2 local = Vector2.zero;
        var mouse = Mouse.current;

        if (mouse != null && cam != null)
        {
            Ray ray = cam.ScreenPointToRay(mouse.position.ReadValue());
            if (box.Raycast(ray, out RaycastHit hit, 100f))
            {
                hovered = true;
                Vector3 p = transform.InverseTransformPoint(hit.point);
                local = new Vector2(p.x / (box.size.x * 0.5f), p.y / (box.size.y * 0.5f));
                local = Vector2.ClampMagnitude(local, 1.4f);
                iceHoverUv = new Vector2(p.x / box.size.x + 0.5f, p.y / box.size.y + 0.5f);
                if (mouse.leftButton.wasPressedThisFrame)
                {
                    iceHitUv = iceHoverUv;
                    iceAge = 0f;
                    Flip();
                }
            }
        }

        if (hovered && !wasHovered) burstT = 0f;
        wasHovered = hovered;

        float t = Time.time + idleSeed;
        if (hovered)
            tiltTarget = new Vector2(local.y * maxTilt, -local.x * maxTilt);
        else
            tiltTarget = new Vector2(Mathf.Sin(t * 0.9f) * idleTilt, Mathf.Sin(t * 0.7f + 1.3f) * idleTilt);

        float dt = Time.deltaTime;
        tilt = Vector2.Lerp(tilt, tiltTarget, 1f - Mathf.Exp(-tiltSmooth * dt));
        hover = Mathf.Lerp(hover, hovered ? 1f : 0f, 1f - Mathf.Exp(-hoverSmooth * dt));

        float yaw = flipTo;
        float arc = 0f;
        if (flipT < 1f)
        {
            flipT = Mathf.Min(1f, flipT + dt / flipDuration);
            float e = Mathf.SmoothStep(0f, 1f, flipT);
            yaw = Mathf.Lerp(flipFrom, flipTo, e);
            arc = Mathf.Sin(flipT * Mathf.PI);
        }

        // Camera nằm phía -Z nên "nhấc lên" là dịch về -Z
        float lift = hover * hoverLift + arc * flipArc;
        float idleBob = Mathf.Sin(t * 1.3f) * idleAmplitude * (1f - hover);
        visual.localPosition = new Vector3(0f, idleBob, -lift);
        visual.localRotation = Quaternion.Euler(tilt.x, tilt.y, 0f) * Quaternion.Euler(0f, yaw, 0f);
        visual.localScale = Vector3.one * Mathf.Lerp(1f, hoverScale, hover);
        if (faceMat != null)
        {
            // Active mượt: rê chuột hoặc lúc lật thì lên 1, rời ra thì về idle
            active = Mathf.Lerp(active, Mathf.Max(hover, arc), 1f - Mathf.Exp(-8f * dt));

            // Bùng cháy: vọt lên rất nhanh (15% đầu) rồi hạ dần về mức active
            burstT = Mathf.Min(1f, burstT + dt / Mathf.Max(0.01f, burstDuration));
            float burst = Mathf.SmoothStep(0f, 1f, burstT / 0.15f) * (1f - Mathf.SmoothStep(0f, 1f, (burstT - 0.15f) / 0.85f));

            // Cộng thêm thời gian để active/bùng chạy nhanh hơn mà không bị nhảy pha khi chuyển trạng thái
            fxExtraTime += dt * ((activeSpeedBoost - 1f) * active + burstSpeedBoost * burst);
            faceMat.SetFloat(ActiveId, active);
            faceMat.SetFloat(FxTimeOffsetId, fxExtraTime);
            faceMat.SetFloat(BurstId, burst);
        }

        if (hitMats.Count > 0)
        {
            if (iceAge >= 0f) iceAge = Mathf.Min(iceAge + dt, 100f);
            iceHover = Mathf.Lerp(iceHover, hovered ? 1f : 0f, 1f - Mathf.Exp(-6f * dt));
            foreach (var m in hitMats)
            {
                m.SetVector(IceHitId, new Vector4(iceHitUv.x, iceHitUv.y, iceAge, 0f));
                m.SetVector(IceHoverId, new Vector4(iceHoverUv.x, iceHoverUv.y, iceHover, 0f));
            }
        }

        if (shadow != null)
        {
            // Card càng cao thì bóng càng lệch, to và nhạt
            shadow.localPosition = shadowBasePos + new Vector3(0.25f, -0.3f, 0f) * lift + new Vector3(tilt.y, -tilt.x, 0f) * 0.01f;
            shadow.localScale = Vector3.one * (1f + lift * 0.12f);
            Color c = shadowBaseColor;
            c.a *= Mathf.Lerp(1f, 0.55f, Mathf.Clamp01(lift / (hoverLift + flipArc)));
            shadowMat.SetColor("_Color", c);
        }
    }

    public void Flip()
    {
        if (flipT < 1f) return;
        flipped = !flipped;
        flipFrom = flipTo;
        flipTo += 180f;
        flipT = 0f;
        burstT = 0f;
    }
}
