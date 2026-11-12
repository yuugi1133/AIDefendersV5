using UnityEngine;
using UnityEngine.UI;

public class AudioSettingsUi : MonoBehaviour
{
    public static bool IsOpen { get { return UnityEngine.Object.FindFirstObjectByType<AudioSettingsUi>() != null; } }
    static int closedFrame = -1;
    public static bool BlocksPauseInput { get { return IsOpen || closedFrame == Time.frameCount; } }

    public static Slider VolumeSlider(Transform parent, Vector2 anchor, Vector2 position, float width)
    {
        return SettingSlider(parent, "VolumeSlider", anchor, position, width, 0, 1, AudioListener.volume, GameAudioSettings.SetVolume);
    }

    static Slider SettingSlider(Transform parent, string name, Vector2 anchor, Vector2 position, float width, float min, float max, float initial, System.Action<float> change)
    {
        var root = new GameObject(name, typeof(RectTransform), typeof(Slider));
        root.transform.SetParent(parent, false);
        UiSkin.Place(root.GetComponent<RectTransform>(), anchor, position, new Vector2(width, 40));
        var track = UiFont.AddImage(root.transform, "Track", UiSkin.Navy);
        UiFont.Stretch(track.rectTransform);
        track.raycastTarget = false;
        var fillArea = new GameObject("FillArea", typeof(RectTransform));
        fillArea.transform.SetParent(root.transform, false);
        var fillRect = fillArea.GetComponent<RectTransform>();
        UiFont.Stretch(fillRect);
        fillRect.offsetMin = new Vector2(20, 5);
        fillRect.offsetMax = new Vector2(-20, -5);
        var fill = UiFont.AddImage(fillArea.transform, "Fill", UiSkin.Green);
        UiFont.Stretch(fill.rectTransform);
        fill.raycastTarget = false;
        var handleArea = new GameObject("HandleArea", typeof(RectTransform));
        handleArea.transform.SetParent(root.transform, false);
        var handleRect = handleArea.GetComponent<RectTransform>();
        UiFont.Stretch(handleRect);
        handleRect.offsetMin = new Vector2(20, 0);
        handleRect.offsetMax = new Vector2(-20, 0);
        var handle = UiFont.AddImage(handleArea.transform, "Handle", UiSkin.Sky);
        handle.rectTransform.sizeDelta = new Vector2(40, 8);
        var percent = UiFont.AddText(parent, "VolumePercent", "", 26, TextAnchor.MiddleRight, Color.white, UiType.BodyBold);
        UiSkin.Place(percent.rectTransform, anchor, position + new Vector2(width * .5f - 60, 68), new Vector2(120, 40));
        var slider = root.GetComponent<Slider>();
        slider.fillRect = fill.rectTransform;
        slider.handleRect = handle.rectTransform;
        slider.targetGraphic = handle;
        slider.minValue = min; slider.maxValue = max;
        slider.SetValueWithoutNotify(initial);
        percent.text = Mathf.RoundToInt(slider.value * 100) + "%";
        slider.onValueChanged.AddListener(value =>
        {
            change(value);
            percent.text = Mathf.RoundToInt(value * 100) + "%";
        });
        return slider;
    }

    public static AudioSettingsUi Create(Transform canvas)
    {
        AudioSettingsUi controller;
        var card = Card(canvas, "MainSettings", "설정", new Vector2(1000, 860), out controller);
        var audio = new GameObject("AudioTab", typeof(RectTransform));
        audio.transform.SetParent(card, false);
        UiFont.Stretch(audio.GetComponent<RectTransform>());
        var camera = new GameObject("CameraTab", typeof(RectTransform));
        camera.transform.SetParent(card, false);
        UiFont.Stretch(camera.GetComponent<RectTransform>());
        Row(audio.transform, "Master", "전체 음량", .60f, 0, 1, AudioListener.volume, GameAudioSettings.SetVolume);
        Row(audio.transform, "Music", "배경음", .42f, 0, 1, GameAudioSettings.MusicVolume, GameAudioSettings.SetMusicVolume);
        Row(audio.transform, "Effects", "효과음 · 환경음", .24f, 0, 1, GameAudioSettings.EffectsVolume, GameAudioSettings.SetEffectsVolume);
        Row(camera.transform, "Move", "카메라 이동 감도", .56f, .25f, 2, GameAudioSettings.MoveSensitivity, GameAudioSettings.SetMoveSensitivity);
        Row(camera.transform, "Look", "카메라 회전 감도", .34f, .25f, 2, GameAudioSettings.LookSensitivity, GameAudioSettings.SetLookSensitivity);
        var tip = UiFont.AddText(camera.transform, "Hint", "100%가 기본 속도입니다. 변경 즉시 적용됩니다.", 22, TextAnchor.MiddleCenter, UiSkin.Muted, UiType.Body);
        UiSkin.Place(tip.rectTransform, new Vector2(.5f, .18f), Vector2.zero, new Vector2(820, 50));
        camera.SetActive(false);
        var soundTab = UiSkin.Button(card, "SoundTab", "소리", UiSkin.Gold, new Vector2(.30f, .76f), Vector2.zero, new Vector2(340, 70));
        var cameraTab = UiSkin.Button(card, "CameraTabButton", "카메라", UiSkin.Blue, new Vector2(.70f, .76f), Vector2.zero, new Vector2(340, 70));
        soundTab.onClick.AddListener(() => { audio.SetActive(true); camera.SetActive(false); soundTab.image.color = UiSkin.Gold; cameraTab.image.color = UiSkin.Blue; });
        cameraTab.onClick.AddListener(() => { audio.SetActive(false); camera.SetActive(true); soundTab.image.color = UiSkin.Blue; cameraTab.image.color = UiSkin.Gold; });
        UiSkin.Button(card, "Close", "확인", UiSkin.Green, new Vector2(.5f, .075f), Vector2.zero, new Vector2(320, 72)).onClick.AddListener(controller.Close);
        return controller;
    }

    static RectTransform Card(Transform canvas, string name, string heading, Vector2 size, out AudioSettingsUi controller)
    {
        var dim = UiFont.AddImage(canvas, name, new Color(.01f, .035f, .075f, .85f));
        UiFont.Stretch(dim.rectTransform);
        controller = dim.gameObject.AddComponent<AudioSettingsUi>();
        var card = UiFont.AddImage(dim.transform, "SettingsCard", UiSkin.Panel);
        UiSkin.Place(card.rectTransform, Vector2.one * .5f, Vector2.zero, size);
        var title = UiFont.AddText(card.transform, "Title", heading, 44, TextAnchor.MiddleCenter, Color.white, UiType.Title);
        UiSkin.Place(title.rectTransform, new Vector2(.5f, .89f), Vector2.zero, new Vector2(800, 70));
        return card.rectTransform;
    }

    static void Row(Transform parent, string name, string label, float y, float min, float max, float initial, System.Action<float> change)
    {
        var caption = UiFont.AddText(parent, name + "Label", label, 28, TextAnchor.MiddleLeft, Color.white, UiType.BodyBold);
        UiSkin.Place(caption.rectTransform, new Vector2(.5f, y), new Vector2(0, 68), new Vector2(760, 40));
        SettingSlider(parent, name + "Slider", new Vector2(.5f, y), Vector2.zero, 760, min, max, initial, change);
    }

    public static AudioSettingsUi CreateHelp(Transform canvas)
    {
        AudioSettingsUi controller;
        var card = Card(canvas, "ControlsOverlay", "조작키 안내", new Vector2(1120, 900), out controller);
        string[] keys = { "W A S D / 방향키", "SPACE / SHIFT", "마우스 이동 / 휠", "TAB", "왼쪽 클릭 / 오른쪽 클릭", "Q · E · R · F", "ESC" };
        string[] actions = { "카메라 이동", "카메라 상승 / 하강", "시점 회전 / 확대·축소", "건설 메뉴 열기·닫기", "배치 확정 / 배치 취소", "스킬 슬롯 1~4 사용", "일시정지 · 배치 중에는 취소" };
        for (int i = 0; i < keys.Length; i++)
        {
            float y = .75f - i * .085f;
            var key = UiFont.AddText(card, "Key" + i, keys[i], 25, TextAnchor.MiddleLeft, UiSkin.Gold, UiType.BodyBold);
            UiSkin.Place(key.rectTransform, new Vector2(.29f, y), Vector2.zero, new Vector2(470, 55));
            var action = UiFont.AddText(card, "Action" + i, actions[i], 25, TextAnchor.MiddleLeft, Color.white, UiType.Body);
            UiSkin.Place(action.rectTransform, new Vector2(.73f, y), Vector2.zero, new Vector2(500, 55));
        }
        UiSkin.Button(card, "Close", "확인", UiSkin.Green, new Vector2(.5f, .09f), Vector2.zero, new Vector2(320, 72)).onClick.AddListener(controller.Close);
        return controller;
    }

    void Update()
    {
#if ENABLE_INPUT_SYSTEM
        var keyboard = UnityEngine.InputSystem.Keyboard.current;
        if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame) Close();
#endif
    }

    void Close() { closedFrame = Time.frameCount; GameAudioSettings.Save(); Destroy(gameObject); }
}
