using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuManager_MainMenu : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    // 씬 이름을 전달받아 해당 화면으로 이동
    public void LoadScene(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }

    // 게임 종료 버튼
    public void QuitGame()
    {
        Application.Quit();
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false; // 에디터 테스트용
        #endif
    }
}
