using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuController : MonoBehaviour
{
    [SerializeField] GameObject enterMatchCodeScreen;
    [SerializeField] TMP_Text inputField;
    [SerializeField] List<Button> keyboardButtons = new List<Button>();

    private void Awake()
    {
        // Subscribe
        foreach (Button button in keyboardButtons)
        {
            button.onClick.AddListener(OnKeyboardButtonClick);
        }
    }

    private void Start()
    {
        // Initialize
        enterMatchCodeScreen.SetActive(false);
    }

    private void OnDisable()
    {
        // Unsubscribe
        foreach (Button button in keyboardButtons)
        {
            button.onClick.AddListener(OnKeyboardButtonClick);
        }
    }

    public void OnSoloPlayClicked()
    {
        // TODO
    }

    public void OnCreateMatchClicked()
    {
        // Optionally: connect to Normcore here first
        // Realtime.Connect("my-room-code");

        // Load scene
        SceneManager.LoadScene("LobbyScene");
    }

    public void OnJoinMatchClicked()
    {
        // Show match code entry UI
        OpenEnterMatchCodeScreen();
    }


    void OpenEnterMatchCodeScreen()
    {
        // Initialize screen
        inputField.text = "";

        enterMatchCodeScreen.SetActive(true);
    }

    void CloseEnterMatchCodeScreen()
    {
        inputField.text = "";
        enterMatchCodeScreen.SetActive(false);
    }

    void OnKeyboardButtonClick()
    {
        // Get the button that was clicked
        Button clickedButton = UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject?.GetComponent<Button>();
        if (clickedButton == null)
            return;

        // Find the TMP_Text child
        TMP_Text label = clickedButton.GetComponentInChildren<TMP_Text>();
        if (label == null)
            return;

        string key = label.text;

        if (key.Equals("DEL", StringComparison.OrdinalIgnoreCase))
        {
            if (inputField.text.Length > 0)
                inputField.text = inputField.text.Substring(0, inputField.text.Length - 1);
        }
        else if (key.Equals("CLR", StringComparison.OrdinalIgnoreCase))
        {
            inputField.text = "";
        }
        else
        {
            inputField.text += key;
        }
    }

    public void GoBackFromMatchCode()
    {
        CloseEnterMatchCodeScreen();   
    }

    public void SubmitMatchCode()
    {
        string input = inputField.text;

        // TODO: logic to check if match code corresponds to Normcore room (or whatever)
        if (input.Equals("test", StringComparison.OrdinalIgnoreCase))
        {
            Debug.Log("Match code success");
            GoToLobby();            
        }
        else
        {
            Debug.Log("Match code fail");
            GoBackFromMatchCode();
        }
    }

    public void GoToLobby()
    {
        // Load scene
        SceneManager.LoadScene("LobbyScene");
    }

    public void OnSettingsClicked()
    {
        // TODO
    }

    public void OnQuitClicked()
    {
        // Exit the application
        Application.Quit();
    }
}