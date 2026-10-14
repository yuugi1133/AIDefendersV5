using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HudSpawnerPreview
{
    readonly RectTransform canvasRt;
    readonly WaveDirector waveDirector;
    readonly List<Card> cards = new List<Card>();
    readonly List<SpawnerPreviewLine> lines = new List<SpawnerPreviewLine>();

    class Card
    {
        public GameObject root;
        public RectTransform rt;
        public Text text;
        public EnemySpawner spawner;
    }

    public HudSpawnerPreview(RectTransform canvasRoot, WaveDirector waveDirector)
    {
        canvasRt = canvasRoot;
        this.waveDirector = waveDirector;
    }

    public void Tick()
    {
        if (waveDirector == null || waveDirector.Phase != WavePhase.Prep)
        {
            HideAll();
            return;
        }

        Camera camera = FindHudCamera();
        if (camera == null)
        {
            HideAll();
            return;
        }

        EnemySpawner[] spawners = Object.FindObjectsByType<EnemySpawner>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None
        );

        int used = 0;
        for (int i = 0; i < spawners.Length; i++)
        {
            EnemySpawner spawner = spawners[i];
            if (spawner == null)
                continue;
            if (!waveDirector.TryGetSpawnerPreview(spawner, lines))
                continue;

            Card card = GetCard(used++);
            card.spawner = spawner;
            card.text.text = FormatLines(lines);
            card.root.SetActive(true);

            Vector3 world = spawner.transform.position + Vector3.up * 2.4f;
            Vector3 screen = camera.WorldToScreenPoint(world);
            if (screen.z <= 0.1f)
            {
                card.root.SetActive(false);
                continue;
            }

            Vector2 local;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRt, screen, null, out local))
            {
                card.root.SetActive(false);
                continue;
            }

            card.rt.anchoredPosition = local;
        }

        for (int i = used; i < cards.Count; i++)
            cards[i].root.SetActive(false);
    }

    static string FormatLines(List<SpawnerPreviewLine> preview)
    {
        System.Text.StringBuilder builder = new System.Text.StringBuilder();
        for (int i = 0; i < preview.Count; i++)
        {
            if (i > 0)
                builder.Append('\n');
            builder.Append(preview[i].label);
            builder.Append(": ");
            builder.Append(preview[i].count);
            builder.Append(" 마리");
        }

        return builder.ToString();
    }

    Card GetCard(int index)
    {
        while (cards.Count <= index)
            cards.Add(CreateCard());
        return cards[index];
    }

    Card CreateCard()
    {
        Image panel = UiFont.AddImage(canvasRt, "SpawnerPreview", new Color(0.07f, 0.09f, 0.12f, 0.82f));
        panel.raycastTarget = false;
        RectTransform rt = panel.rectTransform;
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0f);
        rt.sizeDelta = new Vector2(220f, 72f);

        ContentSizeFitter fitter = panel.gameObject.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        VerticalLayoutGroup layout = panel.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(14, 14, 10, 10);
        layout.childAlignment = TextAnchor.UpperLeft;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        Text text = UiFont.AddText(rt, "Lines", "", 20, TextAnchor.UpperLeft, Color.white, UiType.BodyMedium);
        text.raycastTarget = false;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        LayoutElement textLayout = text.gameObject.AddComponent<LayoutElement>();
        textLayout.minWidth = 160f;
        textLayout.preferredWidth = 200f;

        panel.gameObject.SetActive(false);
        return new Card
        {
            root = panel.gameObject,
            rt = rt,
            text = text
        };
    }

    void HideAll()
    {
        for (int i = 0; i < cards.Count; i++)
            cards[i].root.SetActive(false);
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
