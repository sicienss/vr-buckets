using Normal.Realtime;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit.Interactors;


public class GameManager : RealtimeComponent<GameManagerModel>
{
    public static GameManager instance;
    public GameManagerModel Model => model; // Getter to access the model from outside this class

    public GameObject scoreRowPrefab;
    public GameObject floatingScoreTextPrefab;

    // AUDIO -- TODO: move this to audio manager
    [SerializeField] AudioSource audioSource;
    [SerializeField] AudioClip whistle;
    [SerializeField] AudioClip buzzer;
    [SerializeField] AudioClip count5;
    [SerializeField] AudioClip count4;
    [SerializeField] AudioClip count3;
    [SerializeField] AudioClip count2;
    [SerializeField] AudioClip count1;
    [SerializeField] AudioClip greatJob;

    private void Awake()
    {
        instance = this;
    }

    protected override void OnRealtimeModelReplaced(GameManagerModel previousModel, GameManagerModel currentModel)
    {
        if (previousModel != null)
        {
            previousModel.gameStateDidChange -= OnGameStateChanged;
        }

        if (currentModel != null)
        {
            currentModel.gameStateDidChange += OnGameStateChanged;
        }
    }

    private void OnGameStateChanged(GameManagerModel model, int newState)
    {
        // LOBBY
        if (newState == 0)
        {
            Debug.Log("GameState changed to: Lobby");
            SceneManager.sceneUnloaded += OnSceneUnloaded;
            SceneManager.UnloadSceneAsync("BasketballCourtScene");
            SceneManager.sceneLoaded += OnSceneLoaded;
            SceneManager.LoadScene("LobbyScene", LoadSceneMode.Additive);
        }
        // LOADING
        else if (newState == 1)
        {
            StartCoroutine(GoToGameplayRoutine());
        }
        // COUNTDOWN
        else if (newState == 2)
        {
            Debug.Log("GameState changed to: Countdown");
            StartCoroutine(CountdownRoutine());
        }
        // GAMEPLAY
        else if (newState == 3)
        {
            Debug.Log("GameState changed to: Gameplay");
            StartCoroutine(GameplayRoutine());
        }
        // RESULTS
        else if (newState == 4)
        {
            Debug.Log("GameState changed to: Results");
            StartCoroutine(ResultsRoutine());
        }
    }

    IEnumerator GoToGameplayRoutine()
    {
        // Fade out
        yield return TransitionManager.instance.Fade(1f, 0.5f);

        Debug.Log("GameState changed to: Loading");
        SceneManager.sceneUnloaded += OnSceneUnloaded;
        SceneManager.UnloadSceneAsync("LobbyScene");
        SceneManager.sceneLoaded += OnSceneLoaded;
        SceneManager.LoadScene("BasketballCourtScene", LoadSceneMode.Additive);
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "LobbyScene")
        {
            // Fade in
            TransitionManager.instance.Fade(0f, 0.5f);
        }
        else if (scene.name == "BasketballCourtScene")
        {
            //// Go to an open spawn point -- TODO: This is a bad algo, do something better...
            //var players = FindObjectsOfType<PlayerComponent>();
            //var playerSpawnPoints = FindObjectsOfType<PlayerSpawnPoint>();
            //foreach (var playerSpawnPoint in playerSpawnPoints)
            //{
            //    if (!players.Any(p => Vector3.Distance(p.transform.position, playerSpawnPoint.transform.position) < 1))
            //    { 
            //        transform.position = playerSpawnPoint.transform.position;
            //        break;
            //    }
            //}

            // Fade in
            TransitionManager.instance.Fade(0f, 0.5f);

            // Host transitions state
            // TODO: We should add _isReady to PlayerModel and set to true only when scenes have loaded, so host can wait for everyone to load before advancing state
            if (realtime.clientID == 0)
            {
                SpawnBalls(); // Host spawns balls
                model.gameState = 2;
            }
        }

        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneUnloaded(Scene scene)
    {
        if (scene.name == "Lobby")
        {
        }
        else if (scene.name == "BasketballCourtScene")
        {
            // Host destroys balls
            if (realtime.clientID == 0)
            {
                DestroyBalls();
            }
        }

        SceneManager.sceneUnloaded -= OnSceneUnloaded;
    }

    private void SpawnBalls()
    {
        foreach (BasketballSpawnPoint spawnPoint in FindObjectsOfType<BasketballSpawnPoint>()) // TODO: Do something more performant than FindObjectsOfType()
        {
            Realtime.Instantiate("Basketball",
                position: spawnPoint.transform.position,
                rotation: spawnPoint.transform.rotation,
                ownedByClient: false,        // Let whoever grabs it request ownership
                preventOwnershipTakeover: false,
                useInstance: realtime        // Attach to current Realtime room
            );
        }
    }

    private void DestroyBalls()
    {
        foreach (var ball in FindObjectsOfType<Basketball>()) // TODO: Do this more performantly than using FindObjectsOfType()
        {
            Realtime.Destroy(ball.gameObject);
        }
    }

    public void GoToLobby()
    {
        StartCoroutine(GoToLobbyRoutine());
    }

    IEnumerator GoToLobbyRoutine()
    {
        // Fade out
        yield return TransitionManager.instance.Fade(1f, 0.5f);

        // Change scenes
        yield return SceneManager.UnloadSceneAsync("MainMenuScene");
        yield return SceneManager.LoadSceneAsync("LobbyScene", LoadSceneMode.Additive);

        // Fade in
        yield return TransitionManager.instance.Fade(0f, 0.5f);
    }

    private IEnumerator CountdownRoutine()
    {
        // Create a score rows for each player
        CreateScoreRows();

        // Do countdown
        TMP_Text label = GameObject.Find("CountdownLabel")?.GetComponent<TMP_Text>();

        var countdownClips = new AudioClip[] { count1, count2, count3, count4, count5 }; // SFX
        for (int i = 5; i >= 1; i--)
        {
            if (label != null) label.text = i.ToString();
            audioSource.PlayOneShot(countdownClips[i - 1], 1f); // SFX
            yield return new WaitForSeconds(1f);
        }

        if (label != null) label.text = "Go!";

        PlayWhistle(); // SFX

        // Host transitions state
        if (realtime.clientID == 0)
        {
            model.gameState = 3;
        }

        yield return new WaitForSeconds(1f);
        if (label != null) label.text = "";
    }

    private IEnumerator GameplayRoutine()
    {
        // Do countdown
        TMP_Text label = GameObject.Find("TimerLabel")?.GetComponent<TMP_Text>();

        int matchDurationSeconds = 60; // TODO: don't hardcode this here
        for (int i = matchDurationSeconds; i >= 1; i--)
        {
            if (label != null) label.text = i.ToString();
            yield return new WaitForSeconds(1f);
        }
        if (label != null) label.text = "0";

        // Host transitions state
        if (realtime.clientID == 0)
        {
            model.gameState = 4;
        }
    }

    IEnumerator ResultsRoutine()
    {
        PlayBuzzer(); // SFX

        TMP_Text label = GameObject.Find("CountdownLabel")?.GetComponent<TMP_Text>();
        label.text = "Time's Up!";
        yield return new WaitForSeconds(3f);
        // Determine winning player
        PlayerComponent winnerPlayerComponent = FindObjectsOfType<PlayerComponent>()
            .Where(pc => pc.Model != null)
            .OrderByDescending(pc => pc.Model.playerScore)
            .FirstOrDefault();
        GameObject.Find("CountdownLabel").GetComponent<TMPro.TMP_Text>().text = $"{winnerPlayerComponent.Model.playerName} Wins";
        audioSource.PlayOneShot(greatJob, 1f); // SFX
        yield return new WaitForSeconds(5f);

        // Fade out
        yield return TransitionManager.instance.Fade(1f, 0.5f);

        // Host transitions state
        if (realtime.clientID == 0)
        {
            model.gameState = 0;
        }
    }

    private void CreateScoreRows()
    {
        foreach (var player in FindObjectsOfType<PlayerComponent>())
        {
            GameObject rowGameObject = Instantiate(scoreRowPrefab, GameObject.Find("ScoreRowContainer").transform);
            rowGameObject.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 0);
            ScoreRow row = rowGameObject.GetComponent<ScoreRow>();
            row.Bind(player.Model, player.GetComponent<RealtimeAvatarVoice>());
        }
    }

    public void PlayWhistle()
    {
        audioSource.PlayOneShot(whistle, 1f);
    }

    public void PlayBuzzer()
    {
        audioSource.PlayOneShot(buzzer, 1f);
    }


    public void RefrestRayInteractors()
    {
        StartCoroutine(RefreshRayInteractorsRoutine());
    }

    IEnumerator RefreshRayInteractorsRoutine()
    {
        // Find all PlayerComponent instances in the scene
        var playerComponents = FindObjectsOfType<PlayerComponent>();

        // Filter to just the locally owned PlayerComponent
        foreach (var player in playerComponents)
        {
            if (!player.realtimeView.isOwnedLocally)
                continue;

            // Get XRRayInteractors of locally owned player
            XRRayInteractor[] rayInteractors = player.GetComponentsInChildren<XRRayInteractor>(true);

            foreach (var rayInteractor in rayInteractors)
                rayInteractor.enabled = false;

            yield return null; // wait one frame

            foreach (var rayInteractor in rayInteractors)
                rayInteractor.enabled = true;

            Debug.Log("Refreshed ray interactors for local player.");
            yield break;
        }
    }
}