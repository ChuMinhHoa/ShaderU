using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// Tạo scene demo hiệu ứng nguyên tố từ scene CardDemo: Tools > Card Demo > Build Elemental Scene
/// Scene gốc CardDemo.unity giữ nguyên, chỉ copy sang CardDemoElemental.unity rồi đổi material.
public static class ElementalCardBuilder
{
    const string SrcScene = "Assets/Scenes/CardDemo.unity";
    const string DstScene = "Assets/Scenes/CardDemoElemental.unity";
    const float CardW = 2f, CardH = 3f; // khớp CardDemoBuilder.CreateCard

    // Thứ tự khớp Card_0..2 của CardDemoBuilder (fire, water, forest); Lightning dùng tranh forest
    static readonly (string art, string name, int element, float intensity)[] Cards =
    {
        ("card_front_fire",   "Fire",      0, 1.0f),
        ("card_front_water",  "Water",     2, 1.0f),
        ("card_front_forest", "Lightning", 1, 1.0f),
    };

    [MenuItem("Tools/Card Demo/Build Elemental Scene")]
    public static void Build()
    {
        var shader = Shader.Find("SharderUltimate/CardElemental");
        if (shader == null) { Debug.LogError("[CardDemo] Không thấy shader SharderUltimate/CardElemental"); return; }

        AssetDatabase.DeleteAsset(DstScene);
        if (!AssetDatabase.CopyAsset(SrcScene, DstScene))
        {
            Debug.LogError("[CardDemo] Chưa có " + SrcScene + ", chạy Tools > Card Demo > Build Scene trước.");
            return;
        }

        var scene = EditorSceneManager.OpenScene(DstScene, OpenSceneMode.Single);
        var cardsRoot = GameObject.Find("Cards");
        if (cardsRoot == null) { Debug.LogError("[CardDemo] Scene không có object 'Cards'"); return; }

        var back = CardDemoBuilder.Load(CardDemoBuilder.Tex + "card_back.png");
        var mask = CardDemoBuilder.Load(CardDemoBuilder.Tex + "card_foil_mask.png");

        for (int i = 0; i < Cards.Length && i < cardsRoot.transform.childCount; i++)
        {
            var c = Cards[i];
            var m = new Material(shader);
            m.SetTexture("_FrontTex", CardDemoBuilder.Load(CardDemoBuilder.Tex + c.art + ".png"));
            m.SetTexture("_BackTex", back);
            m.SetTexture("_FoilMask", mask);
            m.SetFloat("_Element", c.element);
            m.SetFloat("_FxIntensity", c.intensity);
            CardDemoBuilder.SaveMaterial(m, "CardElem_" + c.name);

            var face = cardsRoot.transform.GetChild(i).Find("Pivot/Face");
            if (face != null)
            {
                face.GetComponent<Renderer>().sharedMaterial = m;
                // Quad to hơn card _Pad lần để hiệu ứng tràn được ra ngoài viền (collider không đổi)
                float pad = m.GetFloat("_Pad");
                face.localScale = new Vector3(CardW * pad, CardH * pad, 1f);
            }
        }

        EditorSceneManager.SaveScene(scene, DstScene);
        CardDemoBuilder.AddToBuildSettings(DstScene);
        AssetDatabase.SaveAssets();
        Debug.Log("[CardDemo] Built " + DstScene);
    }
}
