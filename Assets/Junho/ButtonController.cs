using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor.Rendering.LookDev;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ButtonController : MonoBehaviour
{
    public GameObject helpCanvas;
    public DefaultInputActions uiInput;
    public Button[] buttons;
    private int selectButtonIndex = 0; 
    private float lastInputTime;
    private float inputDelay = 0.25f;
    private ColorBlock[] originalColors;
    // Start is called before the first frame update
    void Awake()
    {
        uiInput = new DefaultInputActions();
        // uiInput.performed += OnInputPerformed;
        originalColors = new ColorBlock[buttons.Length];

        // 버튼의 원래 색상을 저장
        for (int i = 0; i < buttons.Length; i++)
        {
            originalColors[i] = buttons[i].colors;
        }
    }

    void OnEnable()
    {
        uiInput.UI.Navigate.performed += OnInputPerformed;
        uiInput.UI.Submit.performed += OnSubmit;
        uiInput.UI.Cancel.performed += OnCancel;
        uiInput.UI.Enable();
    }
    void OnDisable()
    {
        uiInput.UI.Navigate.performed -= OnInputPerformed;
        uiInput.UI.Submit.performed -= OnSubmit;
        uiInput.UI.Cancel.performed -= OnCancel;
        uiInput.UI.Disable();
    }

    void OnInputPerformed(InputAction.CallbackContext context)
    {
        if(Time.time - lastInputTime < inputDelay) return;

        Vector2 navigation = context.ReadValue<Vector2>();
        // Debug.Log(navigation.y);
        // Debug.Log("select" + selectButtonIndex);

        if(navigation.y < 0)
        {
            selectButtonIndex = (selectButtonIndex + 1) % buttons.Length;
            lastInputTime = Time.time;
        }

        else if(navigation.y > 0)
        {
            selectButtonIndex = (selectButtonIndex - 1) % buttons.Length;
            lastInputTime = Time.time;
        }

        SelectButton(selectButtonIndex);
    }

    public void OnSubmit(InputAction.CallbackContext context)
    {
        //if(context.control.name == "buttonSouth")
        //{
        buttons[selectButtonIndex].onClick.Invoke();
        //}
    }

    public void OnCancel(InputAction.CallbackContext context)
    {
        if (helpCanvas.activeSelf)
        {
            HelpQuit(); // 도움말 UI가 켜져 있을 때만 끄기
        }
    }

    void SelectButton(int index)
    {
        for(int i = 0; i < buttons.Length; i++)
        {
            var color = buttons[i].colors;
            // color.normalColor = i == index ? Color.yellow : Color.white;
            // buttons[i].colors = color;

            if(i == index)
            {
                color.normalColor = new Color(color.pressedColor.r, color.pressedColor.g, color.pressedColor.b, 1f);
            }

            else
            {
                color.normalColor = originalColors[i].normalColor;
            }

            buttons[i].colors = color;
        }
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
