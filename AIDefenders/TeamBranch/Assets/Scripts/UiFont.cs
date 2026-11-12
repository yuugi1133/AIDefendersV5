using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.UI;

public enum UiType
{
    Body,
    BodyMedium,
    BodyBold,
    Title,
    TitleMedium,
    TitleLight
}

public static class UiFont
{
    static readonly Dictionary<UiType, Font> fonts = new Dictionary<UiType, Font>();
    static readonly Dictionary<UiType, TMP_FontAsset> tmpFonts = new Dictionary<UiType, TMP_FontAsset>();
    static Sprite uiSprite;

    public static Canvas FindSceneCanvas(Component owner)
    {
        if (owner == null)
            return null;

        var scene = owner.gameObject.scene;
        Canvas parent = owner.GetComponentInParent<Canvas>();
        if (parent != null && parent.gameObject.scene == scene)
            return parent;

        Canvas fallback = null;
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (Canvas canvas in root.GetComponentsInChildren<Canvas>(true))
            {
                if (!canvas.isRootCanvas)
                    continue;
                if (canvas.isActiveAndEnabled)
                    return canvas;
                if (fallback == null)
                    fallback = canvas;
            }
        }
        return fallback;
    }

    public static Font Get(UiType type)
    {
        Font font;
        if (fonts.TryGetValue(type, out font) && font != null)
            return font;

        font = Resources.Load<Font>(ResourcePath(type));
        if (font != null)
            fonts[type] = font;
        return font;
    }

    public static TMP_FontAsset GetTmp(UiType type)
    {
        TMP_FontAsset tmp;
        if (tmpFonts.TryGetValue(type, out tmp) && tmp != null)
            return tmp;

        Font source = Get(type);
        if (source == null)
            return TMP_Settings.defaultFontAsset;

        try
        {
            tmp = TMP_FontAsset.CreateFontAsset(
                source,
                90,
                9,
                GlyphRenderMode.SDFAA,
                1024,
                1024,
                AtlasPopulationMode.Dynamic
            );
        }
        catch (System.Exception exception)
        {
            Debug.LogWarning("TMP 폰트 생성 실패: " + type + " / " + exception.Message);
            return TMP_Settings.defaultFontAsset;
        }

        tmp.name = source.name + " SDF";
        tmpFonts[type] = tmp;
        return tmp;
    }

    public static void Apply(Text text, UiType type)
    {
        if (text == null)
            return;
        Font font = Get(type);
        if (font != null)
            text.font = font;
        text.fontStyle = FontStyle.Normal;
    }

    public static void Apply(TMP_Text text, UiType type)
    {
        if (text == null)
            return;
        TMP_FontAsset font = GetTmp(type);
        if (font != null)
            text.font = font;
    }

    public static Text AddText(
        Transform parent,
        string name,
        string content,
        int size,
        TextAnchor align,
        Color color,
        UiType type)
    {
        GameObject go = new GameObject(name);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.SetParent(parent, false);

        Text text = go.AddComponent<Text>();
        text.text = content;
        text.fontSize = size;
        text.alignment = align;
        text.color = color;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.raycastTarget = false;
        Apply(text, type);
        if (type == UiType.Title || type == UiType.BodyBold || type == UiType.TitleMedium)
        {
            var outline = go.AddComponent<Outline>();
            outline.effectColor = new Color(0.015f, 0.055f, 0.12f, .9f);
            outline.effectDistance = new Vector2(1.2f, -1.2f);
        }
        return text;
    }

    public static Image AddImage(Transform parent, string name, Color color)
    {
        GameObject go = new GameObject(name);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.SetParent(parent, false);
        Image image = go.AddComponent<Image>();
        image.sprite = Sprite;
        image.type = Image.Type.Sliced;
        image.color = color;
        if (color.a >= .9f) UiSkin.Surface(image);
        return image;
    }

    public static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    static Sprite Sprite
    {
        get
        {
            if (uiSprite != null)
                return uiSprite;

            uiSprite = UiSkin.Rounded;
            return uiSprite;
        }
    }

    static string ResourcePath(UiType type)
    {
        switch (type)
        {
            case UiType.Title:
                return "Fonts/GmarketSansTTFBold";
            case UiType.TitleMedium:
                return "Fonts/GmarketSansTTFMedium";
            case UiType.TitleLight:
                return "Fonts/GmarketSansTTFLight";
            case UiType.BodyBold:
                return "Fonts/Pretendard-Bold";
            case UiType.BodyMedium:
                return "Fonts/Pretendard-Medium";
            default:
                return "Fonts/Pretendard-Regular";
        }
    }
}
