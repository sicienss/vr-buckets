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
    [SerializeField] GameObject matchCodeEntryScreen;
    [SerializeField] TMP_Text inputField;
    [SerializeField] List<Button> keyboardButtons = new List<Button>();
    [SerializeField] GameObject uiBlocker;

    private DebugInputActions debugInput;


    private void OnEnable()
    {
        debugInput = new DebugInputActions();
        debugInput.Enable();
        debugInput.DebugControls.CreateRoom.performed += ctx => OnCreateMatchClicked();
    }

    private void OnDisable()
    {
        debugInput.Disable();
    }

    private void Awake()
    {
        // Subscribe to stuff
        foreach (Button button in keyboardButtons)
        {
            button.onClick.AddListener(OnKeyboardButtonClick);
        }
        MatchManager.instance.OnJoinSuccess += HandleJoinSuccess;
        MatchManager.instance.OnJoinFailure += HandleJoinFailure;
    }

    private void Start()
    {
        // FADE IN
        TransitionManager.instance.Fade(0f, 0.5f);
        Debug.Log("Fade!");
    }

    private void OnDestroy()
    {
        // Unsubscribe from stuff
        foreach (Button button in keyboardButtons)
        {
            button.onClick.AddListener(OnKeyboardButtonClick);
        }
        if (MatchManager.instance != null)
        {
            MatchManager.instance.OnJoinSuccess -= HandleJoinSuccess;
            MatchManager.instance.OnJoinFailure -= HandleJoinFailure;
        }
    }

    public void OnCreateMatchClicked()
    {
        uiBlocker.SetActive(true);

        // Create Normcore room w/ random match code
        string code = MatchManager.instance.GenerateMatchCode(3);
        MatchManager.instance.TryJoinRoom(code); // Callbacks below will fire in response to success or failure       
    }

    public void OnJoinMatchClicked()
    {
        // Show match code entry UI
        OpenMatchCodeEntryScreen();
    }


    void OpenMatchCodeEntryScreen()
    {
        // Initialize screen
        inputField.text = "";
        matchCodeEntryScreen.SetActive(true);
    }

    void CloseMatchCodeEntryScreen()
    {
        inputField.text = "";
        matchCodeEntryScreen.SetActive(false);
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

        // Delete, clear, or add letters to inputField
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

    public void SubmitMatchCode()
    {
        uiBlocker.SetActive(true);

        // Check if submitted match code corresponds to Normcore room
        string code = inputField.text.ToUpperInvariant();
        MatchManager.instance.TryJoinRoom(code); // Callbacks below will fire in response to success or failure       
    }

    private void HandleJoinSuccess()
    {
        GameManager.instance.GoToLobby();
    }

    private void HandleJoinFailure()
    {
        // TODO: flow for surfacing the error inUI rather than just going back
        GoBackFromMatchCode();
    }

    public void GoBackFromMatchCode()
    {
        CloseMatchCodeEntryScreen();

        uiBlocker.SetActive(false);
    }
    
    public void OnSettingsClicked()
    {
        // TODO
    }

    public void OnQuitClicked()
    {
        StartCoroutine(QuitRoutine());
    }

    IEnumerator QuitRoutine()
    {
        yield return TransitionManager.instance.Fade(1f, 0.5f);
        Application.Quit();
    }
}