using UnityEditor;
using UnityEngine;

/// Vẽ và cho kéo các vùng pop-out (Pop Out Zones) ngay trên ảnh nhân vật trong Scene view:
/// chấm ở tâm để di chuyển, 2 ô vuông để đổi bán kính ngang / dọc,
/// vòng tròn trên elip trong (nét mờ) để đổi độ rộng dải mờ ở mép vùng.
[CustomEditor(typeof(HoloCardView))]
public class HoloCardViewEditor : Editor
{
    static readonly Color ZoneColor = new Color(1f, 0.6f, 0.1f);

    void OnSceneGUI()
    {
        var view = (HoloCardView)target;
        if (!view.popOut || view.characterMesh == null || view.popOutZones == null) return;
        if (view.characterMesh.sharedMesh == null) return;

        // Khung ảnh nhân vật ứng với UV 0..1 của ảnh (mesh Spine chỉ phủ phần có hình nên không dùng bounds của mesh)
        var rect = view.CharacterRect();
        var t = view.characterMesh.transform;
        Vector3 ToWorld(Vector2 uv) => t.TransformPoint(new Vector3(
            Mathf.LerpUnclamped(rect.xMin, rect.xMax, uv.x), Mathf.LerpUnclamped(rect.yMin, rect.yMax, uv.y), 0f));
        Vector2 ToUv(Vector3 world)
        {
            var local = t.InverseTransformPoint(world);
            return new Vector2((local.x - rect.xMin) / rect.width, (local.y - rect.yMin) / rect.height);
        }

        Vector3[] Ellipse(Vector2 center, Vector2 radius)
        {
            var points = new Vector3[49];
            for (int k = 0; k < points.Length; k++)
            {
                float a = 2f * Mathf.PI * k / (points.Length - 1);
                points[k] = ToWorld(center + new Vector2(Mathf.Cos(a) * radius.x, Mathf.Sin(a) * radius.y));
            }
            return points;
        }

        int count = Mathf.Min(view.popOutZones.Count, HoloCardView.MaxPopOutZones);
        for (int i = 0; i < count; i++)
        {
            var zone = view.popOutZones[i];
            float softness = HoloCardView.ZoneSoftness(zone);
            var inner = zone.radius * (1f - softness);

            // Elip ngoài = mép vùng, elip trong (nét mờ) = hết dải mờ, bên trong hiện rõ hoàn toàn
            Handles.color = ZoneColor;
            Handles.DrawAAPolyLine(3f, Ellipse(zone.center, zone.radius));
            Handles.color = new Color(ZoneColor.r, ZoneColor.g, ZoneColor.b, 0.45f);
            Handles.DrawAAPolyLine(2f, Ellipse(zone.center, inner));
            Handles.color = ZoneColor;
            Handles.Label(ToWorld(zone.center + new Vector2(0f, zone.radius.y)), "  Pop-out " + (i + 1) + "  (mờ " + softness.ToString("0.00") + ")");

            Vector3 centerWorld = ToWorld(zone.center);
            float size = HandleUtility.GetHandleSize(centerWorld) * 0.06f;

            EditorGUI.BeginChangeCheck();
            var newCenter = Handles.FreeMoveHandle(centerWorld, size, Vector3.zero, Handles.DotHandleCap);
            var newRadiusX = Handles.FreeMoveHandle(ToWorld(zone.center + new Vector2(zone.radius.x, 0f)), size, Vector3.zero, Handles.RectangleHandleCap);
            var newRadiusY = Handles.FreeMoveHandle(ToWorld(zone.center + new Vector2(0f, zone.radius.y)), size, Vector3.zero, Handles.RectangleHandleCap);
            // Tay nắm tròn trên elip trong (bên trái): kéo vào tâm = dải mờ rộng hơn, kéo ra mép = cắt gọn hơn
            var innerWorld = ToWorld(zone.center - new Vector2(inner.x, 0f));
            var newInner = Handles.FreeMoveHandle(innerWorld, size, Vector3.zero, Handles.CircleHandleCap);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(view, "Edit Pop-Out Zone");
                var center = ToUv(newCenter);
                if ((center - zone.center).sqrMagnitude > 1e-10f)
                    zone.center = center;
                else if ((newInner - innerWorld).sqrMagnitude > 1e-12f)
                    zone.softness = Mathf.Clamp(1f - Mathf.Abs(ToUv(newInner).x - zone.center.x) / zone.radius.x, 0.01f, 1f);
                else
                    zone.radius = new Vector2(   // đổi bán kính giữ nguyên tỉ lệ dải mờ
                        Mathf.Max(0.02f, Mathf.Abs(ToUv(newRadiusX).x - zone.center.x)),
                        Mathf.Max(0.02f, Mathf.Abs(ToUv(newRadiusY).y - zone.center.y)));
                zone.softness = HoloCardView.ZoneSoftness(zone);
                view.popOutZones[i] = zone;
                view.ApplyPopOut();
                PrefabUtility.RecordPrefabInstancePropertyModifications(view);   // thẻ là prefab instance: lưu thành override
                EditorUtility.SetDirty(view);
            }
        }
    }
}
