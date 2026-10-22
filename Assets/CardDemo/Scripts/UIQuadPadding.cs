using UnityEngine;
using UnityEngine.UI;

/// Nới quad của UI Image ra mỗi phía `padding` đơn vị UI, UV nội suy tiếp ra ngoài 0..1,
/// để shader vẽ được phần tràn ra ngoài ảnh (vd. glow của SharderUltimate/GemFrame). Raycast vẫn theo rect gốc.
/// Dùng cho Image kiểu Simple.
[RequireComponent(typeof(Graphic))]
public class UIQuadPadding : BaseMeshEffect
{
    [Min(0f)] public float padding = 36f;

    public override void ModifyMesh(VertexHelper vh)
    {
        if (!IsActive() || padding <= 0f || vh.currentVertCount == 0) return;

        // Ánh xạ tuyến tính vị trí -> UV lấy từ khung bao các đỉnh
        var v = new UIVertex();
        Vector2 pMin = Vector2.positiveInfinity, pMax = Vector2.negativeInfinity;
        Vector2 uMin = Vector2.positiveInfinity, uMax = Vector2.negativeInfinity;
        for (int i = 0; i < vh.currentVertCount; i++)
        {
            vh.PopulateUIVertex(ref v, i);
            pMin = Vector2.Min(pMin, v.position);
            pMax = Vector2.Max(pMax, v.position);
            uMin = Vector2.Min(uMin, v.uv0);
            uMax = Vector2.Max(uMax, v.uv0);
        }
        Vector2 size = pMax - pMin;
        if (size.x <= 0f || size.y <= 0f) return;
        Vector2 center = (pMin + pMax) * 0.5f;

        for (int i = 0; i < vh.currentVertCount; i++)
        {
            vh.PopulateUIVertex(ref v, i);
            Vector2 pos = v.position;
            pos.x += Mathf.Sign(pos.x - center.x) * padding;
            pos.y += Mathf.Sign(pos.y - center.y) * padding;
            Vector2 t = (pos - pMin) / size;
            v.position = new Vector3(pos.x, pos.y, v.position.z);
            v.uv0 = new Vector4(Mathf.LerpUnclamped(uMin.x, uMax.x, t.x), Mathf.LerpUnclamped(uMin.y, uMax.y, t.y), 0f, 0f);
            vh.SetUIVertex(v, i);
        }
    }
}
