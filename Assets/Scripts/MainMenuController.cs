using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuController : MonoBehaviour
{
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
        // Show code entry UI, then:
        // Realtime.Connect(code);

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
