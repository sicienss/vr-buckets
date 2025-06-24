using UnityEngine;
using UnityEngine.SceneManagement;

public class LobbyController : MonoBehaviour
{
    public void OnStartMatchClicked()
    {
        // Load scene
        SceneManager.LoadScene("BasketballCourtScene");
    }
}


