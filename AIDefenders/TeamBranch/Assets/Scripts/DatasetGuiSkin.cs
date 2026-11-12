using System.Collections.Generic;
using UnityEngine;

// IMGUI counterpart of UiSkin for the dataset editor's existing controls.
public sealed class DatasetGuiSkin : System.IDisposable
{
    public GUISkin Skin { get; private set; }
    public GUIStyle Title { get; private set; }
    public GUIStyle Muted { get; private set; }
    public GUIStyle GoldButton { get; private set; }
    public GUIStyle GreenButton { get; private set; }
    readonly List<Texture2D> textures = new List<Texture2D>();

    public DatasetGuiSkin(GUISkin source)
    {
        Skin = Object.Instantiate(source);
        Skin.font = UiFont.Get(UiType.BodyMedium);
        Skin.label.fontSize = 16;
        Skin.label.normal.textColor = Color.white;
        Skin.label.wordWrap = true;
        Skin.label.padding = new RectOffset(4, 4, 3, 3);
        ConfigureButton(Skin.button, UiSkin.Blue);
        ConfigureButton(Skin.toggle, UiSkin.Blue);
        ConfigureField(Skin.textField);
        ConfigureField(Skin.textArea);
        Skin.box.normal.background = Surface(UiSkin.Panel);
        Skin.box.border = new RectOffset(15, 15, 15, 15);
        Skin.box.normal.textColor = Color.white;
        Skin.box.font = UiFont.Get(UiType.BodyBold);
        Skin.box.fontSize = 18;
        Skin.box.alignment = TextAnchor.UpperCenter;
        Skin.box.padding = new RectOffset(12, 12, 8, 8);
        Skin.scrollView.normal.background = null;
        GoldButton = new GUIStyle(Skin.button);
        ConfigureButton(GoldButton, UiSkin.Gold);
        GoldButton.normal.textColor = UiSkin.Navy;
        GoldButton.hover.textColor = UiSkin.Navy;
        GoldButton.active.textColor = UiSkin.Navy;
        GreenButton = new GUIStyle(Skin.button);
        ConfigureButton(GreenButton, UiSkin.Green);
        Title = new GUIStyle(Skin.label) { font = UiFont.Get(UiType.Title), fontSize = 30, wordWrap = false };
        Muted = new GUIStyle(Skin.label);
        Muted.normal.textColor = UiSkin.Muted;
    }

    void ConfigureButton(GUIStyle style, Color color)
    {
        style.font = UiFont.Get(UiType.BodyBold);
        style.fontSize = 16;
        style.alignment = TextAnchor.MiddleCenter;
        style.padding = new RectOffset(6, 6, 2, 2);
        style.margin = new RectOffset(3, 3, 3, 3);
        style.border = new RectOffset(15, 15, 15, 15);
        style.normal.background = Surface(color);
        style.hover.background = Surface(Color.Lerp(color, Color.white, .16f));
        style.active.background = Surface(Color.Lerp(color, UiSkin.Navy, .3f));
        style.onNormal.background = Surface(UiSkin.Sky);
        style.onHover.background = style.hover.background;
        style.onActive.background = style.active.background;
        style.normal.textColor = style.hover.textColor = style.active.textColor = Color.white;
        style.onNormal.textColor = style.onHover.textColor = style.onActive.textColor = Color.white;
    }

    void ConfigureField(GUIStyle style)
    {
        style.fontSize = 16;
        style.padding = new RectOffset(8, 8, 4, 4);
        style.border = new RectOffset(8, 8, 8, 8);
        Texture2D normal = Surface(UiSkin.Navy);
        Texture2D focused = Surface(new Color(.08f, .27f, .46f));
        foreach (GUIStyleState state in new[] { style.normal, style.hover, style.active, style.focused, style.onNormal, style.onHover, style.onActive, style.onFocused })
        {
            state.background = normal;
            state.textColor = Color.white;
        }
        style.focused.background = style.onFocused.background = focused;
    }

    Texture2D Surface(Color color)
    {
        const int size = 48;
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.name = "Dataset UI Surface";
        texture.wrapMode = TextureWrapMode.Clamp;
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dx = Mathf.Max(11f - x, x - 36f, 0f);
                float dy = Mathf.Max(11f - y, y - 36f, 0f);
                float alpha = Mathf.Clamp01(11f - Mathf.Sqrt(dx * dx + dy * dy));
                bool edge = x < 2 || x > 45 || y < 4 || y > 45;
                Color pixel = edge ? UiSkin.Navy : color * (y > 24 ? 1.06f : .87f);
                pixel.a = alpha;
                texture.SetPixel(x, y, pixel);
            }
        texture.Apply(false, true);
        textures.Add(texture);
        return texture;
    }

    public void Dispose()
    {
        foreach (Texture2D texture in textures) Object.Destroy(texture);
        textures.Clear();
        if (Skin != null) Object.Destroy(Skin);
    }
}
