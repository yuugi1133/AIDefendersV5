using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuManager_TurretLab : MonoBehaviour
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
}
