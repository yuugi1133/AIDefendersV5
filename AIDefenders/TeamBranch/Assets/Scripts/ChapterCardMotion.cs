using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class ChapterCardMotion : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    bool selected;
    bool hovered;
    Outline outline;
    void Awake() { outline = GetComponent<Outline>(); transform.localScale = Vector3.one * .94f; }
    public void Select()
    {
        foreach (var card in transform.parent.GetComponentsInChildren<ChapterCardMotion>())
        {
            card.selected = card == this;
            if (card.outline != null) card.outline.effectColor = card.selected ? UiSkin.Gold : UiSkin.Sky;
        }
    }
    public void OnPointerEnter(PointerEventData data) { hovered = true; }
    public void OnPointerExit(PointerEventData data) { hovered = false; }
    void Update()
    {
        float size = selected ? 1.04f : hovered ? .98f : .94f;
        transform.localScale = Vector3.Lerp(transform.localScale, Vector3.one * size, 1 - Mathf.Exp(-14 * Time.unscaledDeltaTime));
    }
}
