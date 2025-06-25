using UnityEngine;
using UnityEngine.SceneManagement;

public class Loader : MonoBehaviour
{
    [SerializeField] private string initialScene = "MainMenuScene";

    private void Start()
    {
        // Load the main menu additively so LoaderScene stays loaded
        SceneManager.LoadSceneAsync(initialScene, LoadSceneMode.Additive);
    }
}