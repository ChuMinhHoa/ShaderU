using System.Collections.Generic;
using UnityEngine;

/// Sinh mesh cho cửa sổ portal và nhân vật của thẻ holo. Chạy được cả Edit mode lẫn runtime.
public static class PortalMeshes
{
    // ---------- Contour cửa sổ ----------
    // Mọi contour đi theo chiều kim đồng hồ khi nhìn từ camera (-Z), góc ψ đo từ trục +Y.
    // Phải khớp với hàm WindowDistance trong HoloCard.shader để lỗ trên mặt thẻ và mesh portal trùng nhau.

    /// Elip bán trục half
    public static List<Vector2> EllipseContour(Vector2 half, int seg = 192)
    {
        var c = new List<Vector2>(seg);
        for (int i = 0; i < seg; i++)
        {
            float a = 2f * Mathf.PI * i / seg;
            c.Add(new Vector2(Mathf.Sin(a) * half.x, Mathf.Cos(a) * half.y));
        }
        return c;
    }

    /// Chữ nhật nửa kích thước half, bo góc bán kính corner (0 = góc vuông sắc)
    public static List<Vector2> RoundedRectContour(Vector2 half, float corner, int segPerCorner = 16)
    {
        float r = Mathf.Clamp(corner, 0f, Mathf.Min(half.x, half.y));
        var inner = half - new Vector2(r, r);
        var centers = new[]
        {
            new Vector2(inner.x, inner.y), new Vector2(inner.x, -inner.y),
            new Vector2(-inner.x, -inner.y), new Vector2(-inner.x, inner.y),
        };
        var c = new List<Vector2>();
        for (int k = 0; k < 4; k++)
            AddArc(c, centers[k], r, k * 90f, k * 90f + 90f, segPerCorner);
        return Dedupe(c);
    }

    /// Đa giác đều (đỉnh đầu tiên hướng lên +Y), xoay rotationDeg, bo góc theo tỉ lệ round (0..0.9),
    /// rồi kéo giãn theo half (half.x = half.y thì là đa giác đều đúng nghĩa)
    public static List<Vector2> PolygonContour(Vector2 half, int sides, float rotationDeg, float round, int segPerCorner = 12)
    {
        sides = Mathf.Max(3, sides);
        float an = 180f / sides;
        float rin = 1f - round;
        var c = new List<Vector2>();
        for (int k = 0; k < sides; k++)
        {
            float phi = k * 2f * an;
            var vertex = new Vector2(Mathf.Sin(phi * Mathf.Deg2Rad), Mathf.Cos(phi * Mathf.Deg2Rad)) * rin;
            AddArc(c, vertex, round, phi - an, phi + an, segPerCorner);
        }
        var rot = Quaternion.Euler(0f, 0f, rotationDeg);
        for (int i = 0; i < c.Count; i++)
        {
            Vector2 p = rot * c[i];
            c[i] = new Vector2(p.x * half.x, p.y * half.y);
        }
        return Dedupe(c);
    }

    /// Đẩy contour ra ngoài theo pháp tuyến một đoạn d (âm = thu vào trong)
    public static List<Vector2> Offset(List<Vector2> contour, float d)
    {
        var o = new List<Vector2>(contour.Count);
        for (int i = 0; i < contour.Count; i++) o.Add(contour[i] + Normal(contour, i) * d);
        return o;
    }

    // ---------- Mesh theo contour ----------

    /// Mặt phẳng hình quạt phủ kín contour (contour lồi), mặt trước hướng -Z
    public static Mesh Fan(List<Vector2> contour)
    {
        int n = contour.Count;
        var v = new Vector3[n + 1];
        var tri = new int[n * 3];
        v[0] = Vector3.zero;
        for (int i = 0; i < n; i++)
        {
            v[i + 1] = contour[i];
            tri[i * 3] = 0;
            tri[i * 3 + 1] = i + 1;
            tri[i * 3 + 2] = (i + 1) % n + 1;
        }
        var m = new Mesh { name = "WindowFan", vertices = v, triangles = tri };
        m.RecalculateBounds();
        return m;
    }

    /// Ống chạy dọc contour từ mặt thẻ lùi vào trong, màu chuyển từ miệng ống (rim) vào đáy (deep)
    public static Mesh Tube(List<Vector2> contour, float depth, Color rim, Color deep)
    {
        int n = contour.Count;
        var v = new Vector3[(n + 1) * 2];
        var c = new Color[v.Length];
        var uv = new Vector2[v.Length];
        var tri = new int[n * 6];
        for (int i = 0; i <= n; i++)
        {
            Vector3 p = contour[i % n];
            v[i * 2] = p;
            v[i * 2 + 1] = p + new Vector3(0f, 0f, depth);
            c[i * 2] = rim;
            c[i * 2 + 1] = deep;
            uv[i * 2] = new Vector2((float)i / n, 0f);
            uv[i * 2 + 1] = new Vector2((float)i / n, 1f);
            if (i == n) break;
            int k = i * 6, b = i * 2;
            tri[k] = b; tri[k + 1] = b + 1; tri[k + 2] = b + 2;
            tri[k + 3] = b + 2; tri[k + 4] = b + 1; tri[k + 5] = b + 3;
        }
        var m = new Mesh { name = "Tube", vertices = v, colors = c, uv = uv, triangles = tri };
        m.RecalculateBounds();
        return m;
    }

    /// Nửa quả cầu úp sau cửa sổ (tâm tại mặt thẻ, phần lồi về +Z): mọi tia nhìn qua cửa sổ đều chạm mặt trong,
    /// nghiêng góc nào cũng không lộ mép như quad phẳng.
    /// UV chiếu stereographic từ tâm: thẳng phía trước = giữa ảnh, mép vòm = 0.5 ± uvScale (theo từng trục).
    public static Mesh Dome(float radius, Vector2 uvScale, int rings = 32, int segments = 96)
    {
        int cols = segments + 1;
        var v = new Vector3[(rings + 1) * cols];
        var uv = new Vector2[v.Length];
        var tri = new int[rings * segments * 6];
        for (int r = 0; r <= rings; r++)
        {
            float alpha = 0.5f * Mathf.PI * r / rings;        // 0 = thẳng phía trước, 90° = mép vòm
            for (int s = 0; s <= segments; s++)
            {
                float beta = 2f * Mathf.PI * s / segments;
                var d = new Vector3(Mathf.Sin(alpha) * Mathf.Cos(beta), Mathf.Sin(alpha) * Mathf.Sin(beta), Mathf.Cos(alpha));
                int i = r * cols + s;
                v[i] = d * radius;
                var proj = new Vector2(d.x, d.y) / (1f + d.z);
                uv[i] = new Vector2(0.5f, 0.5f) + Vector2.Scale(proj, uvScale);
            }
        }
        int k = 0;
        for (int r = 0; r < rings; r++)
        for (int s = 0; s < segments; s++)
        {
            int a = r * cols + s, b = a + 1, c = a + cols, e = c + 1;
            tri[k++] = a; tri[k++] = c; tri[k++] = b;
            tri[k++] = b; tri[k++] = c; tri[k++] = e;
        }
        var m = new Mesh { name = "SpaceDome", vertices = v, uv = uv, triangles = tri };
        m.RecalculateBounds();
        return m;
    }

    /// Cung tròn tâm center, bán kính r, góc ψ (đo từ +Y, theo chiều kim đồng hồ) từ fromDeg tới toDeg
    static void AddArc(List<Vector2> c, Vector2 center, float r, float fromDeg, float toDeg, int seg)
    {
        for (int i = 0; i <= seg; i++)
        {
            float a = Mathf.Lerp(fromDeg, toDeg, (float)i / seg) * Mathf.Deg2Rad;
            c.Add(center + new Vector2(Mathf.Sin(a), Mathf.Cos(a)) * r);
        }
    }

    /// Bỏ điểm trùng (cung bán kính 0 sinh ra nhiều điểm giống nhau)
    static List<Vector2> Dedupe(List<Vector2> c)
    {
        var o = new List<Vector2>(c.Count);
        foreach (var p in c)
            if (o.Count == 0 || (p - o[o.Count - 1]).sqrMagnitude > 1e-10f) o.Add(p);
        while (o.Count > 1 && (o[0] - o[o.Count - 1]).sqrMagnitude <= 1e-10f) o.RemoveAt(o.Count - 1);
        return o;
    }

    /// Pháp tuyến hướng ra ngoài tại điểm i (contour chiều kim đồng hồ: bên trái hướng đi là bên ngoài)
    static Vector2 Normal(List<Vector2> c, int i)
    {
        int n = c.Count;
        var a = c[(i - 1 + n) % n];
        var b = c[(i + 1) % n];
        var t = (b - a).normalized;
        var e1 = (c[i] - a).normalized;
        var e2 = (b - c[i]).normalized;
        // Ở góc nhọn, dùng trung bình 2 pháp tuyến cạnh và kéo dài để dải offset đều
        var n1 = new Vector2(-e1.y, e1.x);
        var n2 = new Vector2(-e2.y, e2.x);
        var nm = (n1 + n2).normalized;
        float cos = Mathf.Max(0.35f, Vector2.Dot(nm, n1));
        return nm.sqrMagnitude > 0f ? nm / cos : new Vector2(-t.y, t.x);
    }

    /// Quad phẳng cho ảnh nhân vật, cạnh dài nhất bằng size, giữ tỉ lệ ảnh
    public static Mesh Quad(Texture tex, float size)
    {
        float aspect = tex != null ? (float)tex.width / tex.height : 1f;
        float w = size * Mathf.Min(1f, aspect) * 0.5f, h = size * Mathf.Min(1f, 1f / aspect) * 0.5f;
        var m = new Mesh
        {
            name = "CharacterQuad",
            vertices = new[] { new Vector3(-w, -h, 0f), new Vector3(w, -h, 0f), new Vector3(-w, h, 0f), new Vector3(w, h, 0f) },
            uv = new[] { new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 1f), new Vector2(1f, 1f) },
            triangles = new[] { 0, 2, 1, 1, 2, 3 },
        };
        m.RecalculateBounds();
        return m;
    }
}
