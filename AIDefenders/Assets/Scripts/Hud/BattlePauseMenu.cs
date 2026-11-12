using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class BattlePauseMenu : MonoBehaviour
{
    public static bool IsPaused { get; private set; }
    GameObject overlay;
    float previousSpeed = 1f;
    CursorLockMode previousCursorLock;
    bool previousCursorVisible;
    WaveDirector director;
    Slider volumeSlider;

    public void Build(RectTransform canvas, WaveDirector waves)
    {
        director = waves;
        UiSkin.Button(canvas, "PauseButton", "Ⅱ", UiSkin.Sky, new Vector2(1, 1), new Vector2(-60, -66), new Vector2(72, 72)).onClick.AddListener(TogglePause);
        Image dim = UiFont.AddImage(canvas, "PauseOverlay", new Color(.015f, .04f, .07f, .78f));
        UiFont.Stretch(dim.rectTransform);
        overlay = dim.gameObject;
        Image panel = UiFont.AddImage(dim.transform, "PauseCard", new Color32(153, 113, 65, 255));
        UiSkin.Place(panel.rectTransform, Vector2.one * .5f, Vector2.zero, new Vector2(900, 620));
        Text title = UiFont.AddText(panel.transform, "Title", "일시 정지", 44, TextAnchor.MiddleCenter, Color.white, UiType.Title);
        UiSkin.Place(title.rectTransform, new Vector2(.5f, 1), new Vector2(0, -66), new Vector2(800, 70));
        Text caption = UiFont.AddText(panel.transform, "Caption", "방어선을 점검하고 전투를 이어가세요.", 23, TextAnchor.MiddleCenter, Color.white, UiType.BodyMedium);
        UiSkin.Place(caption.rectTransform, new Vector2(.5f, 1), new Vector2(0, -138), new Vector2(800, 50));
        Text volume = UiFont.AddText(panel.transform, "VolumeLabel", "전체 음량", 25, TextAnchor.MiddleLeft, Color.white, UiType.BodyBold);
        UiSkin.Place(volume.rectTransform, new Vector2(.5f, .58f), Vector2.zero, new Vector2(700, 45));
        volumeSlider = AudioSettingsUi.VolumeSlider(panel.transform, new Vector2(.5f, .48f), Vector2.zero, 700);
        UiSkin.Button(panel.transform, "MoreSettings", "소리 · 카메라 설정", UiSkin.Blue, new Vector2(.5f, .70f), Vector2.zero, new Vector2(420, 56)).onClick.AddListener(() =>
        {
            if (!AudioSettingsUi.IsOpen) AudioSettingsUi.Create(canvas);
        });
        UiSkin.Button(panel.transform, "Continue", "▶  계속하기", UiSkin.Green, new Vector2(.5f, .30f), Vector2.zero, new Vector2(700, 84)).onClick.AddListener(Resume);
        UiSkin.Button(panel.transform, "Restart", "다시 시작", UiSkin.Blue, new Vector2(.29f, .12f), Vector2.zero, new Vector2(320, 72)).onClick.AddListener(() => Leave(SceneManager.GetActiveScene().name));
        UiSkin.Button(panel.transform, "Home", "메인 메뉴", UiSkin.Blue, new Vector2(.71f, .12f), Vector2.zero, new Vector2(320, 72)).onClick.AddListener(() => Leave("MainMenu"));
        overlay.SetActive(false);
    }

    void Update()
    {
        if (AudioSettingsUi.BlocksPauseInput) return;
        if (IsPaused && volumeSlider != null && !Mathf.Approximately(volumeSlider.value, AudioListener.volume))
            volumeSlider.value = AudioListener.volume;
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) TogglePause();
    }

    public void TogglePause()
    {
        if (overlay == null || (director != null && director.Phase == WavePhase.Complete)) return;
        if (IsPaused) { Resume(); return; }
        previousSpeed = Time.timeScale;
        previousCursorLock = Cursor.lockState;
        previousCursorVisible = Cursor.visible;
        IsPaused = true;
        if (volumeSlider != null) volumeSlider.value = AudioListener.volume;
        Time.timeScale = 0;
        overlay.transform.SetAsLastSibling();
        overlay.SetActive(true);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void Resume()
    {
        if (!IsPaused) return;
        Time.timeScale = previousSpeed;
        Cursor.lockState = previousCursorLock;
        Cursor.visible = previousCursorVisible;
        IsPaused = false;
        GameAudioSettings.Save();
        if (overlay != null) overlay.SetActive(false);
    }

    void Leave(string scene)
    {
        Resume();
        SceneLoader.Load(scene);
    }

    void OnDestroy() { Resume(); }
}
