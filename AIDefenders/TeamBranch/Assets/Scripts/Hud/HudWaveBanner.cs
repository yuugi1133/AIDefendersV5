using UnityEngine;
using UnityEngine.UI;

public class HudWaveBanner
{
    const float FadeIn = 0.35f;
    const float Hold = 1.15f;
    const float FadeOut = 0.7f;

    readonly GameObject root;
    readonly CanvasGroup group;
    readonly Text title;
    readonly Text subtitle;
    float elapsed = -1f;

    public HudWaveBanner(RectTransform canvasRoot)
    {
        Image panel = UiFont.AddImage(canvasRoot, "WaveBanner", new Color(0.06f, 0.08f, 0.1f, 0.55f));
        panel.raycastTarget = false;
        RectTransform rt = panel.rectTransform;
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(720f, 140f);
        rt.anchoredPosition = Vector2.zero;

        root = panel.gameObject;
        group = root.AddComponent<CanvasGroup>();
        group.alpha = 0f;
        group.blocksRaycasts = false;
        group.interactable = false;

        title = UiFont.AddText(rt, "Title", "웨이브 시작", 40, TextAnchor.MiddleCenter, Color.white, UiType.Title);
        title.raycastTarget = false;
        RectTransform titleRt = title.rectTransform;
        titleRt.anchorMin = new Vector2(0f, 0.42f);
        titleRt.anchorMax = new Vector2(1f, 1f);
        titleRt.offsetMin = new Vector2(24f, 0f);
        titleRt.offsetMax = new Vector2(-24f, -12f);

        subtitle = UiFont.AddText(rt, "Subtitle", "", 22, TextAnchor.UpperCenter, new Color(0.85f, 0.88f, 0.9f, 1f), UiType.BodyMedium);
        subtitle.raycastTarget = false;
        RectTransform subRt = subtitle.rectTransform;
        subRt.anchorMin = new Vector2(0f, 0f);
        subRt.anchorMax = new Vector2(1f, 0.48f);
        subRt.offsetMin = new Vector2(24f, 16f);
        subRt.offsetMax = new Vector2(-24f, 0f);

        root.SetActive(false);
    }

    public void Play(string waveTitle, int waveNumber, int waveCount)
    {
        title.text = string.IsNullOrWhiteSpace(waveTitle) ? "웨이브 시작" : waveTitle;
        subtitle.text = waveCount > 0
            ? "웨이브 " + waveNumber + " / " + waveCount + "  시작"
            : "웨이브 " + waveNumber + "  시작";
        elapsed = 0f;
        group.alpha = 0f;
        root.SetActive(true);
        root.transform.SetAsLastSibling();
    }

    public void Tick()
    {
        if (elapsed < 0f)
            return;

        elapsed += Time.unscaledDeltaTime;
        float alpha;
        if (elapsed < FadeIn)
            alpha = elapsed / FadeIn;
        else if (elapsed < FadeIn + Hold)
            alpha = 1f;
        else if (elapsed < FadeIn + Hold + FadeOut)
            alpha = 1f - (elapsed - FadeIn - Hold) / FadeOut;
        else
        {
            elapsed = -1f;
            group.alpha = 0f;
            root.SetActive(false);
            return;
        }

        group.alpha = Mathf.Clamp01(alpha);
    }
}
