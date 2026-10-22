// Hình dạng cửa sổ portal của thẻ holo, dùng chung cho mặt thẻ, mask và bóng mép.
// Phải khớp với PortalMeshes.*Contour (C#) để lỗ trên mặt thẻ và mesh portal trùng nhau.
// Mọi khoảng cách tính theo đơn vị thẻ (thẻ rộng 2.5), âm = bên trong.
#ifndef SHARDER_WINDOW_SHAPE_INCLUDED
#define SHARDER_WINDOW_SHAPE_INCLUDED

// Đa giác đều bán kính ngoại tiếp r, đỉnh đầu hướng +Y (Inigo Quilez)
float SdRegularPolygon(float2 p, float r, float n)
{
    float an = PI / n;
    float2 acs = float2(cos(an), sin(an));
    float bn = atan2(p.x, p.y);
    bn = bn - 2.0 * an * floor(bn / (2.0 * an)) - an;
    p = length(p) * float2(cos(bn), abs(sin(bn)));
    p -= r * acs;
    p.y += clamp(-p.y, 0.0, r * acs.y);
    return length(p) * sign(p.x);
}

float SdRoundedBox(float2 p, float2 halfSize, float corner)
{
    float r = clamp(corner, 0.0, min(halfSize.x, halfSize.y));
    float2 q = abs(p) - halfSize + r;
    return length(max(q, 0.0)) + min(max(q.x, q.y), 0.0) - r;
}

// p tính từ tâm cửa sổ. shape = (halfX, halfY, corner, type 0 elip / 1 chữ nhật / 2 đa giác),
// poly = (số cạnh, góc xoay radian)
float WindowDistance(float2 p, float4 shape, float4 poly)
{
    float2 h = shape.xy;
    float minH = min(h.x, h.y);
    if (shape.w < 0.5)
        return (length(p / h) - 1.0) * minH;
    if (shape.w < 1.5)
        return SdRoundedBox(p, h, shape.z);
    // Đa giác: chuẩn hoá về bán kính 1, xoay ngược, bo góc theo tỉ lệ
    float2 qn = p / h;
    float s = sin(poly.y), c = cos(poly.y);
    qn = float2(c * qn.x + s * qn.y, -s * qn.x + c * qn.y);
    float roundness = clamp(shape.z / minH, 0.0, 0.9);
    return (SdRegularPolygon(qn, 1.0 - roundness, max(poly.x, 3.0)) - roundness) * minH;
}

// Vùng nhìn thấy thật sự của cửa sổ = giao của cửa sổ và mặt thẻ (bo góc).
// card = (tâm thẻ tính từ tâm cửa sổ xy, corner, 0), cardHalf = (halfW, halfH)
float VisibleWindowDistance(float2 p, float4 shape, float4 poly, float4 card, float2 cardHalf)
{
    float dWindow = WindowDistance(p, shape, poly);
    if (cardHalf.x <= 0.0) return dWindow;
    float dCard = SdRoundedBox(p - card.xy, cardHalf, card.z);
    return max(dWindow, dCard);
}

#endif
