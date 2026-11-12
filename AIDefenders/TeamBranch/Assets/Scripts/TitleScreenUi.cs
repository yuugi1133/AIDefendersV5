using UnityEngine;
using UnityEngine.UI;

// A separate opening cover, built with the game's own turret renders and supplied logo.
public class TitleScreenUi : MonoBehaviour
{
    System.Action enter;
    bool submitted;
    Button start;

    public static GameObject Create(Transform canvas, System.Action onEnter)
    {
        var root = new GameObject("TitleScreen", typeof(RectTransform), typeof(Image), typeof(TitleScreenUi));
        root.transform.SetParent(canvas, false);
        UiFont.Stretch(root.GetComponent<RectTransform>());
        root.GetComponent<Image>().color = new Color32(8, 37, 78, 255);
        var controller = root.GetComponent<TitleScreenUi>();
        controller.enter = onEnter;

        var rays = new GameObject("RadiantBackdrop", typeof(RectTransform), typeof(CanvasRenderer), typeof(TitleScreenRays));
        rays.transform.SetParent(root.transform, false);
        UiFont.Stretch(rays.GetComponent<RectTransform>());
        rays.GetComponent<TitleScreenRays>().raycastTarget = false;

        // Keep the center clear and bring the larger foreground turrets closer to the viewer.
        Turret(root.transform, "Build_Turret_Test Missile", new Vector2(.23f, .72f), 490, -12, true);
        Turret(root.transform, "Build_Turret_Test Laser", new Vector2(.77f, .72f), 490, 12, false);
        Turret(root.transform, "Build_Turret_MG", new Vector2(.19f, .35f), 600, 8, true);
        Turret(root.transform, "Build_Turret_PrecisionShot", new Vector2(.81f, .35f), 600, -8, false);

        var logo = new GameObject("AI_Defenders_Logo", typeof(RectTransform), typeof(RawImage));
        logo.transform.SetParent(root.transform, false);
        var texture = Resources.Load<Texture2D>("UI/Title/Logo");
        var image = logo.GetComponent<RawImage>();
        image.texture = texture;
        image.raycastTarget = false;
        UiSkin.Place(image.rectTransform, new Vector2(.5f, .55f), Vector2.zero, new Vector2(900, 473));
        // Use the logo's native ratio without stretching its Korean lettering.
        if (texture != null) image.rectTransform.sizeDelta = new Vector2(900, 900f * texture.height / texture.width);
        UiLogoDepth.Apply(image);

        Text subtitle = UiFont.AddText(root.transform, "Tagline", "인식하고, 연구하고, 방어하라", 32, TextAnchor.MiddleCenter, Color.white, UiType.BodyBold);
        UiSkin.Place(subtitle.rectTransform, new Vector2(.5f, .27f), Vector2.zero, new Vector2(900, 65));
        controller.start = UiSkin.Button(root.transform, "Start", "시작", UiSkin.Green,
            new Vector2(.5f, .145f), Vector2.zero, new Vector2(370, 96));
        controller.start.GetComponentInChildren<Text>().fontSize = 44;
        controller.start.onClick.AddListener(controller.Submit);
        Text hint = UiFont.AddText(root.transform, "KeyboardHint", "ENTER 또는 SPACE로 시작", 20, TextAnchor.MiddleCenter, UiSkin.Muted, UiType.Body);
        UiSkin.Place(hint.rectTransform, new Vector2(.5f, .065f), Vector2.zero, new Vector2(600, 40));
        root.transform.SetAsLastSibling();
        return root;
    }

    static void Turret(Transform parent, string resource, Vector2 anchor, float size, float angle, bool mirror)
    {
        var preview = UiSkin.Preview(parent, resource, anchor, new Vector2(size, size * .75f));
        if (preview == null) return;
        preview.rectTransform.localRotation = Quaternion.Euler(0, 0, angle);
        if (mirror) preview.uvRect = new Rect(1, 0, -1, 1);
    }

    void Update()
    {
#if ENABLE_INPUT_SYSTEM
        var keyboard = UnityEngine.InputSystem.Keyboard.current;
        if (keyboard != null && (keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame || keyboard.spaceKey.wasPressedThisFrame)) Submit();
#elif ENABLE_LEGACY_INPUT_MANAGER
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetKeyDown(KeyCode.Space)) Submit();
#endif
    }

    void Submit()
    {
        if (submitted) return;
        submitted = true;
        start.interactable = false;
        enter();
    }
}

// Resolution-independent radial light and quiet rays, with no scenery screenshot behind the cover.
public class TitleScreenRays : MaskableGraphic
{
    protected override void OnPopulateMesh(VertexHelper mesh)
    {
        mesh.Clear();
        Rect r = rectTransform.rect;
        Vector2 center = new Vector2(r.center.x, r.center.y + r.height * .08f);
        const int segments = 96;
        float radius = Mathf.Max(r.width, r.height) * 1.2f;
        mesh.AddVert(center, new Color32(31, 170, 215, 255), Vector2.zero);
        for (int i = 0; i <= segments; i++)
        {
            float angle = i * Mathf.PI * 2 / segments;
            mesh.AddVert(center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius,
                new Color32(7, 29, 70, 255), Vector2.zero);
            if (i > 0) mesh.AddTriangle(0, i, i + 1);
        }
        for (int i = 0; i < 16; i++)
        {
            float angle = i * Mathf.PI * 2 / 16;
            int index = mesh.currentVertCount;
            mesh.AddVert(center, new Color(0.5f, .95f, 1, .07f), Vector2.zero);
            mesh.AddVert(center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius, Color.clear, Vector2.zero);
            mesh.AddVert(center + new Vector2(Mathf.Cos(angle + .1f), Mathf.Sin(angle + .1f)) * radius, Color.clear, Vector2.zero);
            mesh.AddTriangle(index, index + 1, index + 2);
        }
    }
}
