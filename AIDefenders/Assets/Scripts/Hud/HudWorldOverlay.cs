using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HudWorldOverlay
{
    const float DamageLifetime = 0.85f;
    const int MarkerPoolStart = 16;
    const int BlipPoolStart = 24;
    const int DamagePoolStart = 16;

    readonly RectTransform canvasRt;
    readonly List<BlipView> blips = new List<BlipView>();
    readonly List<MarkerView> markers = new List<MarkerView>();
    readonly List<DamageView> damages = new List<DamageView>();
    readonly List<DamageView> liveDamages = new List<DamageView>();

    CCTVRadar radar;
    RectTransform radarRt;
    Image radarPanel;
    Image radarBorder;
    Image radarPlayer;
    GameObject radarRoot;
    Transform markerRoot;
    Transform damageRoot;

    struct BlipView
    {
        public Image image;
        public RectTransform rt;
    }

    struct MarkerView
    {
        public GameObject root;
        public RectTransform rt;
        public Image[] edges;
        public Image hpTrack;
        public RectTransform hpFillRt;
        public Image hpFill;
        public Text label;
        public Text arrow;
    }

    class DamageView
    {
        public Text text;
        public RectTransform rt;
        public Vector3 world;
        public float born;
        public float jitter;
        public bool active;
    }

    public HudWorldOverlay(RectTransform canvasRoot)
    {
        canvasRt = canvasRoot;
        BuildRadar();
        markerRoot = CreateLayer("Markers");
        damageRoot = CreateLayer("Damage");
        for (int i = 0; i < MarkerPoolStart; i++)
            markers.Add(CreateMarker());
        for (int i = 0; i < BlipPoolStart; i++)
            blips.Add(CreateBlip());
        for (int i = 0; i < DamagePoolStart; i++)
            damages.Add(CreateDamage());

        HudFx.DamageShown += HandleDamage;
    }

    public void Dispose()
    {
        HudFx.DamageShown -= HandleDamage;
    }

    public void Tick()
    {
        Camera camera = FindHudCamera();
        TickRadar();
        TickMarkers(camera);
        TickDamage(camera);
    }

    void TickRadar()
    {
        if (radar == null)
            radar = Object.FindFirstObjectByType<CCTVRadar>();

        bool visible = radar != null && radar.IsVisible;
        if (radarRoot.activeSelf != visible)
            radarRoot.SetActive(visible);
        if (!visible)
            return;

        radarRt.sizeDelta = new Vector2(radar.size, radar.size);
        radarRt.anchoredPosition = new Vector2(-radar.margin, -radar.margin);
        radarPanel.color = radar.backgroundColor;
        radarBorder.color = radar.borderColor;

        Target_Base[] targets = Object.FindObjectsByType<Target_Base>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None
        );

        Vector3 origin = radar.transform.position;
        Quaternion toRadar = Quaternion.Euler(0f, -radar.transform.eulerAngles.y, 0f);
        float radius = radar.size * 0.5f - 6f;
        int used = 0;

        for (int i = 0; i < targets.Length; i++)
        {
            Target_Base target = targets[i];
            if (target == null || target.isDead || target.isSample)
                continue;

            Vector3 delta = target.transform.position - origin;
            delta.y = 0f;
            if (delta.sqrMagnitude < 0.0001f)
                continue;

            Vector3 local = toRadar * delta;
            Vector2 n = new Vector2(local.x / radar.range, local.z / radar.range);
            if (n.sqrMagnitude > 1f)
                n.Normalize();

            Color color = radar.unknownColor;
            CCTVController cctv = radar.Cctv;
            if (cctv != null && cctv.TryGetIdentification(target, out bool asEnemy))
                color = asEnemy ? radar.enemyColor : radar.friendlyColor;

            BlipView blip = GetBlip(used++);
            blip.rt.anchoredPosition = n * radius;
            blip.rt.sizeDelta = Vector2.one * radar.blipSize;
            blip.image.color = color;
            blip.image.gameObject.SetActive(true);
        }

        for (int i = used; i < blips.Count; i++)
            blips[i].image.gameObject.SetActive(false);

        float playerSize = radar.blipSize + 2f;
        radarPlayer.rectTransform.sizeDelta = Vector2.one * playerSize;
        radarPlayer.color = radar.playerColor;
        radarPlayer.transform.SetAsLastSibling();
    }

    void TickMarkers(Camera camera)
    {
        YoloTargetMarker[] found = Object.FindObjectsByType<YoloTargetMarker>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None
        );

        int used = 0;
        if (camera != null)
        {
            for (int i = 0; i < found.Length; i++)
            {
                YoloTargetMarker marker = found[i];
                if (marker == null || !marker.isActiveAndEnabled)
                    continue;
                if (marker.Target != null && marker.Target.isDead)
                    continue;
                if (!marker.TryGetWorldBounds(out Bounds bounds))
                    continue;
                if (!TryCanvasRect(camera, bounds, out Vector2 center, out Vector2 size))
                    continue;

                MarkerView view = GetMarker(used++);
                Color color = marker.classifiedAsEnemy ? marker.enemyColor : marker.friendlyColor;
                view.root.SetActive(true);
                view.rt.anchoredPosition = center;
                view.rt.sizeDelta = size;
                view.label.color = color;
                view.arrow.color = color;
                for (int e = 0; e < view.edges.Length; e++)
                    view.edges[e].color = color;
                view.label.text = marker.classifiedAsEnemy
                    ? "E " + marker.confidence.ToString("0.00")
                    : "F " + marker.confidence.ToString("0.00");

                Target_Base target = marker.Target;
                bool showHp = target != null && target.maxHP > 0.01f && !target.isSample;
                view.hpTrack.gameObject.SetActive(showHp);
                if (!showHp)
                    continue;

                float ratio = Mathf.Clamp01(target.currentHP / target.maxHP);
                float barWidth = Mathf.Clamp(size.x, 36f, 88f);
                view.hpTrack.rectTransform.sizeDelta = new Vector2(barWidth, 8f);
                view.hpFill.color = Color.Lerp(new Color(0.9f, 0.15f, 0.12f, 1f), color, ratio);
                view.hpFillRt.anchorMin = Vector2.zero;
                view.hpFillRt.anchorMax = new Vector2(ratio, 1f);
                view.hpFillRt.offsetMin = new Vector2(1f, 1f);
                view.hpFillRt.offsetMax = new Vector2(-1f, -1f);
                view.hpFill.enabled = ratio > 0.001f;
            }
        }

        for (int i = used; i < markers.Count; i++)
            markers[i].root.SetActive(false);
    }

    void TickDamage(Camera camera)
    {
        float now = Time.unscaledTime;
        for (int i = liveDamages.Count - 1; i >= 0; i--)
        {
            DamageView view = liveDamages[i];
            float t = (now - view.born) / DamageLifetime;
            if (t >= 1f || camera == null)
            {
                RecycleDamage(view);
                liveDamages.RemoveAt(i);
                continue;
            }

            Vector3 screen = camera.WorldToScreenPoint(view.world);
            if (screen.z <= 0.1f)
            {
                view.text.gameObject.SetActive(false);
                continue;
            }

            Vector2 local;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRt, screen, null, out local))
                continue;

            local.x += view.jitter;
            local.y += t * 42f;
            view.rt.anchoredPosition = local;
            view.text.color = Color.Lerp(
                new Color(1f, 0.92f, 0.35f, 1f),
                new Color(1f, 0.35f, 0.2f, 0f),
                t
            );
            view.text.gameObject.SetActive(true);
        }
    }

    void HandleDamage(Vector3 world, float amount)
    {
        DamageView view = GetDamage();
        view.world = world;
        view.born = Time.unscaledTime;
        view.jitter = Random.Range(-18f, 18f);
        view.active = true;
        view.text.text = amount >= 10f ? amount.ToString("0") : amount.ToString("0.#");
        view.text.gameObject.SetActive(true);
        liveDamages.Add(view);
    }

    void RecycleDamage(DamageView view)
    {
        view.active = false;
        view.text.gameObject.SetActive(false);
    }

    BlipView GetBlip(int index)
    {
        while (blips.Count <= index)
            blips.Add(CreateBlip());
        return blips[index];
    }

    MarkerView GetMarker(int index)
    {
        while (markers.Count <= index)
            markers.Add(CreateMarker());
        return markers[index];
    }

    DamageView GetDamage()
    {
        for (int i = 0; i < damages.Count; i++)
        {
            if (!damages[i].active)
                return damages[i];
        }

        DamageView created = CreateDamage();
        damages.Add(created);
        return created;
    }

    void BuildRadar()
    {
        radarPanel = UiFont.AddImage(canvasRt, "Radar", new Color(0.05f, 0.08f, 0.08f, 0.72f));
        radarPanel.raycastTarget = false;
        radarRoot = radarPanel.gameObject;
        radarRt = radarPanel.rectTransform;
        radarRt.anchorMin = new Vector2(1f, 1f);
        radarRt.anchorMax = new Vector2(1f, 1f);
        radarRt.pivot = new Vector2(1f, 1f);
        radarRt.anchoredPosition = new Vector2(-16f, -16f);
        radarRt.sizeDelta = new Vector2(168f, 168f);

        radarBorder = UiFont.AddImage(radarRt, "Border", new Color(0.55f, 0.85f, 0.7f, 0.9f));
        radarBorder.raycastTarget = false;
        radarBorder.type = Image.Type.Sliced;
        radarBorder.fillCenter = false;
        UiFont.Stretch(radarBorder.rectTransform);

        radarPlayer = UiFont.AddImage(radarRt, "Player", Color.white);
        radarPlayer.raycastTarget = false;
        RectTransform playerRt = radarPlayer.rectTransform;
        playerRt.anchorMin = new Vector2(0.5f, 0.5f);
        playerRt.anchorMax = new Vector2(0.5f, 0.5f);
        playerRt.pivot = new Vector2(0.5f, 0.5f);
        playerRt.anchoredPosition = Vector2.zero;
        playerRt.sizeDelta = new Vector2(9f, 9f);
        radarRoot.SetActive(false);
    }

    BlipView CreateBlip()
    {
        Image image = UiFont.AddImage(radarRt, "Blip", Color.white);
        image.raycastTarget = false;
        image.gameObject.SetActive(false);
        RectTransform rt = image.rectTransform;
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        return new BlipView { image = image, rt = rt };
    }

    MarkerView CreateMarker()
    {
        GameObject root = new GameObject("Marker");
        RectTransform rt = root.AddComponent<RectTransform>();
        rt.SetParent(markerRoot, false);
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        root.SetActive(false);

        const float edge = 2f;
        Image[] edges =
        {
            CreateEdge(rt, "Top", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, edge)),
            CreateEdge(rt, "Bottom", new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, edge)),
            CreateEdge(rt, "Left", new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0.5f), new Vector2(edge, 0f)),
            CreateEdge(rt, "Right", new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(1f, 0.5f), new Vector2(edge, 0f))
        };

        Text arrow = UiFont.AddText(rt, "Arrow", "▼", 18, TextAnchor.LowerCenter, Color.white, UiType.Title);
        arrow.raycastTarget = false;
        RectTransform arrowRt = arrow.rectTransform;
        arrowRt.anchorMin = new Vector2(0f, 1f);
        arrowRt.anchorMax = new Vector2(1f, 1f);
        arrowRt.pivot = new Vector2(0.5f, 0f);
        arrowRt.anchoredPosition = new Vector2(0f, 4f);
        arrowRt.sizeDelta = new Vector2(0f, 20f);

        Text label = UiFont.AddText(rt, "Label", "E", 16, TextAnchor.LowerCenter, Color.white, UiType.BodyBold);
        label.raycastTarget = false;
        RectTransform labelRt = label.rectTransform;
        labelRt.anchorMin = new Vector2(0f, 1f);
        labelRt.anchorMax = new Vector2(1f, 1f);
        labelRt.pivot = new Vector2(0.5f, 0f);
        labelRt.anchoredPosition = new Vector2(0f, 22f);
        labelRt.sizeDelta = new Vector2(0f, 22f);

        Image hpTrack = UiFont.AddImage(rt, "HpTrack", new Color(0.05f, 0.05f, 0.05f, 0.8f));
        hpTrack.raycastTarget = false;
        RectTransform trackRt = hpTrack.rectTransform;
        trackRt.anchorMin = new Vector2(0.5f, 0f);
        trackRt.anchorMax = new Vector2(0.5f, 0f);
        trackRt.pivot = new Vector2(0.5f, 1f);
        trackRt.anchoredPosition = new Vector2(0f, -4f);
        trackRt.sizeDelta = new Vector2(64f, 8f);

        Image hpFill = UiFont.AddImage(trackRt, "Fill", Color.red);
        hpFill.raycastTarget = false;
        RectTransform fillRt = hpFill.rectTransform;
        fillRt.anchorMin = Vector2.zero;
        fillRt.anchorMax = Vector2.one;
        fillRt.offsetMin = new Vector2(1f, 1f);
        fillRt.offsetMax = new Vector2(-1f, -1f);

        return new MarkerView
        {
            root = root,
            rt = rt,
            edges = edges,
            hpTrack = hpTrack,
            hpFillRt = fillRt,
            hpFill = hpFill,
            label = label,
            arrow = arrow
        };
    }

    static Image CreateEdge(RectTransform parent, string name, Vector2 min, Vector2 max, Vector2 pivot, Vector2 size)
    {
        Image image = UiFont.AddImage(parent, name, Color.white);
        image.raycastTarget = false;
        RectTransform rt = image.rectTransform;
        rt.anchorMin = min;
        rt.anchorMax = max;
        rt.pivot = pivot;
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = size;
        return image;
    }

    DamageView CreateDamage()
    {
        Text text = UiFont.AddText(damageRoot, "Damage", "0", 20, TextAnchor.MiddleCenter, Color.white, UiType.BodyBold);
        text.raycastTarget = false;
        text.gameObject.SetActive(false);
        RectTransform rt = text.rectTransform;
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(80f, 32f);
        return new DamageView { text = text, rt = rt };
    }

    Transform CreateLayer(string name)
    {
        GameObject go = new GameObject(name);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.SetParent(canvasRt, false);
        UiFont.Stretch(rt);
        return rt;
    }

    bool TryCanvasRect(Camera camera, Bounds bounds, out Vector2 center, out Vector2 size)
    {
        center = default;
        size = default;
        Vector3 c = bounds.center;
        Vector3 e = bounds.extents;
        Vector3[] corners =
        {
            c + new Vector3(-e.x, -e.y, -e.z),
            c + new Vector3(e.x, -e.y, -e.z),
            c + new Vector3(e.x, -e.y, e.z),
            c + new Vector3(-e.x, -e.y, e.z),
            c + new Vector3(-e.x, e.y, -e.z),
            c + new Vector3(e.x, e.y, -e.z),
            c + new Vector3(e.x, e.y, e.z),
            c + new Vector3(-e.x, e.y, e.z)
        };

        float minX = float.MaxValue;
        float minY = float.MaxValue;
        float maxX = float.MinValue;
        float maxY = float.MinValue;
        bool any = false;

        for (int i = 0; i < corners.Length; i++)
        {
            Vector3 screen = camera.WorldToScreenPoint(corners[i]);
            if (screen.z <= 0.1f)
                continue;

            Vector2 local;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRt, screen, null, out local))
                continue;

            if (local.x < minX) minX = local.x;
            if (local.y < minY) minY = local.y;
            if (local.x > maxX) maxX = local.x;
            if (local.y > maxY) maxY = local.y;
            any = true;
        }

        if (!any)
            return false;

        center = new Vector2((minX + maxX) * 0.5f, (minY + maxY) * 0.5f);
        size = new Vector2(Mathf.Max(24f, maxX - minX), Mathf.Max(24f, maxY - minY));
        return true;
    }

    static Camera FindHudCamera()
    {
        Camera best = null;
        float bestDepth = float.MinValue;
        Camera[] cameras = Object.FindObjectsByType<Camera>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        for (int i = 0; i < cameras.Length; i++)
        {
            Camera camera = cameras[i];
            if (camera == null || !camera.enabled || !camera.gameObject.activeInHierarchy)
                continue;
            if (camera.targetTexture != null)
                continue;
            if (best != null && camera.depth < bestDepth)
                continue;
            best = camera;
            bestDepth = camera.depth;
        }

        return best;
    }
}
