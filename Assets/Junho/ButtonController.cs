using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ButtonController : MonoBehaviour
{
    public GameObject helpCanvas;
    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }

    public void GameStart()
    {
        SceneManager.LoadScene("Game"); // Game 씬으로 전환
    }

    public void Help()
    {
        helpCanvas.SetActive(true);
    }

    public void GameQuit()
    {
        Application.Quit(); // 애플리케이션 종료
    }
    public void HelpQuit()
    {
        helpCanvas.SetActive(false);
    }
}
