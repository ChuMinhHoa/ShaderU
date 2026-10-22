using System.IO;
using UnityEditor;
using UnityEngine;

/// Tạo ảnh mask mẫu cho pop-out từ ảnh nhân vật, cùng kích thước và khung với ảnh gốc.
/// Kênh R = 0 (chưa phần nào được tràn ra), bóng nhân vật in mờ ở kênh B làm hướng dẫn.
/// Mở ảnh bằng phần mềm vẽ, tô trắng phần muốn tràn ra ngoài cửa sổ rồi lưu lại.
public static class PopOutMaskTool
{
    [MenuItem("Tools/Card Demo/Create Pop-Out Mask Template")]
    static void CreateTemplate()
    {
        var go = Selection.activeGameObject;
        var view = go != null ? go.GetComponentInParent<HoloCardView>() : null;
        var character = view != null ? view.character : Selection.activeObject as Texture2D;
        if (character == null)
        {
            EditorUtility.DisplayDialog("Pop-Out Mask", "Chọn một thẻ có HoloCardView (đã gán Character) hoặc chọn ảnh nhân vật trong Project.", "OK");
            return;
        }

        int w = character.width, h = character.height;
        var pixels = ReadPixels(character, w, h);
        for (int i = 0; i < pixels.Length; i++)
            pixels[i] = new Color32(0, 0, (byte)(pixels[i].a * 0.55f), 255);

        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false, true);
        tex.SetPixels32(pixels);
        string src = AssetDatabase.GetAssetPath(character);
        string path = AssetDatabase.GenerateUniqueAssetPath(
            Path.Combine(Path.GetDirectoryName(src), Path.GetFileNameWithoutExtension(src) + "_popout_mask.png").Replace('\\', '/'));
        File.WriteAllBytes(path, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);

        AssetDatabase.ImportAsset(path);
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.sRGBTexture = false;   // mask là dữ liệu, không phải màu
        importer.alphaSource = TextureImporterAlphaSource.None;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.SaveAndReimport();

        var mask = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        if (view != null)
        {
            Undo.RecordObject(view, "Assign Pop-Out Mask");
            view.popOutMask = mask;
            view.popOut = true;
            EditorUtility.SetDirty(view);
            view.Rebuild();
        }
        EditorGUIUtility.PingObject(mask);
        Debug.Log("[CardDemo] Mask mẫu: " + path + " — tô trắng phần muốn tràn ra ngoài cửa sổ rồi lưu lại");
    }

    /// Đọc pixel qua RenderTexture nên ảnh không cần bật Read/Write
    static Color32[] ReadPixels(Texture source, int w, int h)
    {
        var rt = RenderTexture.GetTemporary(w, h, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
        Graphics.Blit(source, rt);
        var previous = RenderTexture.active;
        RenderTexture.active = rt;
        var read = new Texture2D(w, h, TextureFormat.RGBA32, false, true);
        read.ReadPixels(new Rect(0, 0, w, h), 0, 0);
        read.Apply(false);
        RenderTexture.active = previous;
        RenderTexture.ReleaseTemporary(rt);
        var pixels = read.GetPixels32();
        Object.DestroyImmediate(read);
        return pixels;
    }
}
