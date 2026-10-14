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

            uiSprite = Resources.GetBuiltinResource<Sprite>("UI/Skin/UISprite.psd");
            if (uiSprite == null)
                uiSprite = Resources.GetBuiltinResource<Sprite>("UI/Skin/Background.psd");
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
