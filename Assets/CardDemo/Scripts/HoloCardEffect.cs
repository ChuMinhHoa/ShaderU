/// Giá trị _Effect của shader SharderUltimate/HoloCard. Drawer [Enum(a, 0, b, 1, ...)] trong shader chỉ nhận
/// tối đa 7 cặp tên / giá trị, nên từ hiệu ứng thứ 8 shader dùng [Enum(HoloCardEffect)] trỏ tới enum này.
/// Thêm hiệu ứng mới: thêm vào cuối, giữ nguyên số của các hiệu ứng cũ (material đã lưu theo số)
public enum HoloCardEffect
{
    Rainbow = 0,
    Crystal = 1,
    Metal = 2,
    Ice = 3,
    ThinFilm = 4,
    Glitter = 5,
    Hologram = 6,
    Lenticular = 7,
}
