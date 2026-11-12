using UnityEngine;
using UnityEngine.UI;

// Shared, resolution independent artwork for the game's own UI.
public static class UiSkin
{
    public static readonly Color Navy = new Color32(9, 35, 67, 255);
    public static readonly Color Panel = new Color32(12, 61, 112, 255);
    public static readonly Color Blue = new Color32(16, 133, 218, 255);
    public static readonly Color Sky = new Color32(43, 190, 246, 255);
    public static readonly Color Gold = new Color32(255, 201, 38, 255);
    public static readonly Color Green = new Color32(77, 193, 24, 255);
    public static readonly Color Muted = new Color32(166, 213, 242, 255);
    static Sprite rounded;
    public static Sprite Rounded
    {
        get
        {
            if (rounded != null) return rounded;
            const int size = 64;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.name = "UI Rounded Surface";
            texture.wrapMode = TextureWrapMode.Clamp;
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float dx = Mathf.Max(15f - x, x - 48f, 0f);
                    float dy = Mathf.Max(15f - y, y - 48f, 0f);
                    texture.SetPixel(x, y, new Color(1, 1, 1, Mathf.Clamp01(15f - Mathf.Sqrt(dx * dx + dy * dy))));
                }
            texture.Apply(false, true);
            rounded = Sprite.Create(texture, new Rect(0, 0, size, size), Vector2.one * .5f, 100, 0, SpriteMeshType.FullRect, Vector4.one * 17);
            rounded.name = "UI Rounded Nine Slice";
            return rounded;
        }
    }

    public static void Surface(Image image)
    {
        image.sprite = Rounded;
        image.type = Image.Type.Sliced;
        if (image.GetComponent<UiSurfaceGradient>() == null) image.gameObject.AddComponent<UiSurfaceGradient>();
        var outline = image.GetComponent<Outline>() ?? image.gameObject.AddComponent<Outline>();
        outline.effectColor = new Color32(4, 23, 46, 255);
        outline.effectDistance = new Vector2(2, -2);
        var shadow = image.gameObject.AddComponent<Shadow>();
        shadow.effectColor = new Color(0, .025f, .08f, .6f);
        shadow.effectDistance = new Vector2(0, -5);
    }

    public static void Place(RectTransform rt, Vector2 anchor, Vector2 position, Vector2 size)
    {
        rt.anchorMin = rt.anchorMax = anchor;
        rt.pivot = Vector2.one * .5f;
        rt.anchoredPosition = position;
        rt.sizeDelta = size;
    }

    public static RawImage Preview(Transform parent, string resource, Vector2 anchor, Vector2 size)
    {
        Texture2D texture = Resources.Load<Texture2D>("UI/Previews/" + resource);
        if (texture == null) return null;
        var go = new GameObject("Preview_" + resource, typeof(RectTransform), typeof(RawImage));
        go.transform.SetParent(parent, false);
        var image = go.GetComponent<RawImage>();
        image.texture = texture;
        image.raycastTarget = false;
        Place(image.rectTransform, anchor, Vector2.zero, size);
        return image;
    }

    public static Button Button(Transform parent, string name, string label, Color color, Vector2 anchor, Vector2 position, Vector2 size)
    {
        Image image = UiFont.AddImage(parent, name, color);
        Place(image.rectTransform, anchor, position, size);
        var button = image.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        var colors = button.colors;
        colors.highlightedColor = new Color(1.12f, 1.12f, 1.12f);
        colors.pressedColor = new Color(.75f, .85f, 1f);
        button.colors = colors;
        button.onClick.AddListener(GameSfx.PlayUiClick);
        var text = UiFont.AddText(image.transform, "Label", label, 27, TextAnchor.MiddleCenter, Color.white, UiType.Title);
        UiFont.Stretch(text.rectTransform);
        return button;
    }
}
