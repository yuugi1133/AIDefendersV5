using UnityEngine;
using UnityEngine.UI;

public class HudStageResult
{
    readonly Color panelColor = new Color(0.12f, 0.14f, 0.18f, 0.97f);
    readonly Color textColor = Color.white;
    readonly Color mutedColor = new Color(0.78f, 0.8f, 0.84f, 1f);

    readonly GameObject root;
    readonly CanvasGroup group;
    readonly Text titleText;
    readonly Text gradeText;
    readonly Text[] valueTexts;
    readonly Text accuracyDetail;
    readonly Text finalScore;
    readonly Text researchText;
    float fade;

    static readonly string[] Labels =
    {
        "획득 점수",
        "처치한 적",
        "통과한 아군",
        "통과한 적",
        "처치된 아군",
        "피아식별 정확도"
    };

    public HudStageResult(RectTransform canvasRoot)
    {
        Image dim = UiFont.AddImage(canvasRoot, "StageResult", new Color(0.04f, 0.05f, 0.07f, 0.82f));
        UiFont.Stretch(dim.rectTransform);
        root = dim.gameObject;
        group = root.AddComponent<CanvasGroup>();
        group.alpha = 0f;
        group.blocksRaycasts = true;
        group.interactable = true;

        Image card = UiFont.AddImage(dim.rectTransform, "Card", panelColor);
        RectTransform cardRt = card.rectTransform;
        cardRt.anchorMin = new Vector2(0.5f, 0.5f);
        cardRt.anchorMax = new Vector2(0.5f, 0.5f);
        cardRt.pivot = new Vector2(0.5f, 0.5f);
        cardRt.sizeDelta = new Vector2(680f, 760f);

        titleText = UiFont.AddText(cardRt, "Title", "스테이지 클리어", 36, TextAnchor.MiddleLeft, textColor, UiType.Title);
        RectTransform titleRt = titleText.rectTransform;
        titleRt.anchorMin = new Vector2(0f, 1f);
        titleRt.anchorMax = new Vector2(0.72f, 1f);
        titleRt.pivot = new Vector2(0f, 1f);
        titleRt.anchoredPosition = new Vector2(28f, -22f);
        titleRt.sizeDelta = new Vector2(0f, 48f);

        gradeText = UiFont.AddText(cardRt, "Grade", "S", 48, TextAnchor.MiddleRight, new Color(1f, 0.86f, 0.42f, 1f), UiType.Title);
        RectTransform gradeRt = gradeText.rectTransform;
        gradeRt.anchorMin = new Vector2(0.72f, 1f);
        gradeRt.anchorMax = new Vector2(1f, 1f);
        gradeRt.pivot = new Vector2(1f, 1f);
        gradeRt.anchoredPosition = new Vector2(-28f, -18f);
        gradeRt.sizeDelta = new Vector2(0f, 56f);

        valueTexts = new Text[Labels.Length];
        for (int i = 0; i < Labels.Length; i++)
        {
            float top = -88f - i * 52f;
            Text label = UiFont.AddText(cardRt, "Label_" + i, Labels[i], 20, TextAnchor.MiddleLeft, mutedColor, UiType.Body);
            PlaceRow(label.rectTransform, 0f, 0.55f, top, 36f);

            valueTexts[i] = UiFont.AddText(cardRt, "Value_" + i, "0", 22, TextAnchor.MiddleRight, textColor, UiType.BodyMedium);
            PlaceRow(valueTexts[i].rectTransform, 0.5f, 1f, top, 36f);
        }

        accuracyDetail = UiFont.AddText(cardRt, "AccuracyDetail", "", 16, TextAnchor.MiddleRight, mutedColor, UiType.Body);
        PlaceRow(accuracyDetail.rectTransform, 0.2f, 1f, -88f - Labels.Length * 52f + 14f, 24f);

        Text finalLabel = UiFont.AddText(cardRt, "FinalLabel", "최종 점수", 18, TextAnchor.MiddleLeft, mutedColor, UiType.BodyMedium);
        PlaceRow(finalLabel.rectTransform, 0f, 0.45f, -470f, 28f);

        finalScore = UiFont.AddText(cardRt, "FinalScore", "0", 44, TextAnchor.MiddleRight, textColor, UiType.Title);
        PlaceRow(finalScore.rectTransform, 0.4f, 1f, -458f, 48f);

        researchText = UiFont.AddText(cardRt, "Research", "연구 포인트  +0", 20, TextAnchor.MiddleRight, mutedColor, UiType.BodyMedium);
        PlaceRow(researchText.rectTransform, 0f, 1f, -514f, 28f);

        Image button = UiFont.AddImage(cardRt, "Menu", Color.white);
        RectTransform buttonRt = button.rectTransform;
        buttonRt.anchorMin = new Vector2(0.5f, 0f);
        buttonRt.anchorMax = new Vector2(0.5f, 0f);
        buttonRt.pivot = new Vector2(0.5f, 0f);
        buttonRt.anchoredPosition = new Vector2(0f, 28f);
        buttonRt.sizeDelta = new Vector2(220f, 48f);
        Button click = button.gameObject.AddComponent<Button>();
        click.targetGraphic = button;
        click.onClick.AddListener(() =>
        {
            GameSfx.PlayUiClick();
            SceneLoader.Load("MainMenu");
        });
        Text buttonLabel = UiFont.AddText(buttonRt, "Label", "메인 메뉴", 20, TextAnchor.MiddleCenter, new Color(0.12f, 0.13f, 0.15f, 1f), UiType.BodyMedium);
        UiFont.Stretch(buttonLabel.rectTransform);

        root.SetActive(false);
    }

    public void Show(StageReport report)
    {
        if (report == null)
            report = new StageReport { grade = "실패" };

        bool cleared = report.cleared;
        titleText.text = cleared ? "스테이지 클리어" : "기지 파괴";
        titleText.color = cleared ? textColor : new Color(1f, 0.48f, 0.42f, 1f);
        gradeText.text = string.IsNullOrEmpty(report.grade) ? StageReport.ComputeGrade(report) : report.grade;
        gradeText.color = GradeColor(gradeText.text);

        valueTexts[0].text = report.score.ToString();
        valueTexts[1].text = report.enemiesKilled.ToString();
        valueTexts[2].text = report.friendliesPassed.ToString();
        valueTexts[3].text = report.enemiesPassed.ToString();
        valueTexts[4].text = report.friendliesKilled.ToString();
        valueTexts[5].text = report.IdentifyAccuracyLabel;
        accuracyDetail.text = report.IdentifyAccuracyDetail;
        finalScore.text = report.score.ToString();
        researchText.text = "연구 포인트  +" + report.researchPointsGained
            + "   →   보유 " + report.researchPointsTotal;

        fade = 0f;
        group.alpha = 0f;
        root.SetActive(true);
        root.transform.SetAsLastSibling();
    }

    public void Tick()
    {
        if (root == null || !root.activeSelf)
            return;

        fade = Mathf.Min(1f, fade + Time.unscaledDeltaTime / 0.35f);
        group.alpha = fade;
    }

    static void PlaceRow(RectTransform rt, float minX, float maxX, float top, float height)
    {
        rt.anchorMin = new Vector2(minX, 1f);
        rt.anchorMax = new Vector2(maxX, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = new Vector2(0f, top);
        rt.sizeDelta = new Vector2(-56f, height);
    }

    static Color GradeColor(string grade)
    {
        switch (grade)
        {
            case "S":
                return new Color(1f, 0.86f, 0.42f, 1f);
            case "A":
                return new Color(0.55f, 0.86f, 1f, 1f);
            case "B":
                return new Color(0.55f, 0.82f, 0.58f, 1f);
            case "C":
                return new Color(0.86f, 0.86f, 0.88f, 1f);
            case "D":
                return new Color(0.92f, 0.7f, 0.42f, 1f);
            default:
                return new Color(1f, 0.48f, 0.42f, 1f);
        }
    }
}
