using UnityEngine;
using UnityEngine.UI;

public class UiLogoDepth : MonoBehaviour
{
    RectTransform face;
    readonly RectTransform[] layers = new RectTransform[7];
    Vector2 home;
    public static void Apply(RawImage image)
    {
        var effect = image.gameObject.AddComponent<UiLogoDepth>();
        effect.face = image.rectTransform;
        effect.home = effect.face.anchoredPosition;
        int index = image.transform.GetSiblingIndex();
        for (int i = 0; i < effect.layers.Length; i++)
        {
            var go = new GameObject("LogoDepth_" + i, typeof(RectTransform), typeof(RawImage));
            go.transform.SetParent(image.transform.parent, false);
            go.transform.SetSiblingIndex(index + i);
            var layer = go.GetComponent<RawImage>();
            layer.texture = image.texture;
            layer.raycastTarget = false;
            layer.color = Color.Lerp(new Color32(3, 15, 36, 255), new Color32(19, 63, 106, 255), i / 6f);
            var rect = layer.rectTransform;
            rect.anchorMin = effect.face.anchorMin;
            rect.anchorMax = effect.face.anchorMax;
            rect.pivot = effect.face.pivot;
            rect.sizeDelta = effect.face.sizeDelta;
            effect.layers[i] = rect;
        }
        effect.LateUpdate();
    }

    void LateUpdate()
    {
        float t = Time.unscaledTime;
        var rotation = Quaternion.Euler(Mathf.Sin(t * .55f) * 1.3f, Mathf.Cos(t * .45f) * 2.2f, 0);
        face.anchoredPosition = home + Vector2.up * Mathf.Sin(t * .8f) * 3;
        face.localRotation = rotation;
        for (int i = 0; i < layers.Length; i++)
        {
            float depth = layers.Length - i;
            layers[i].anchoredPosition = face.anchoredPosition + new Vector2(depth * 1.6f, -depth * 2.1f);
            layers[i].localRotation = rotation;
        }
    }

    void OnDestroy()
    {
        foreach (var layer in layers) if (layer != null) Destroy(layer.gameObject);
    }
}
