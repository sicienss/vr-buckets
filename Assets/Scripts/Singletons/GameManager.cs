using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Normal.Realtime;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;


public class GameManager : RealtimeComponent<GameManagerModel>
{
    public static GameManager instance;
    public GameManagerModel Model => model; // Getter to access the model from outside this class

    [SerializeField] GameObject scoreRowPrefab;
    [SerializeField] GameObject playerPrefab;
    [SerializeField] GameObject basketballPrefab;


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
            Debug.Log("GameState changed to: Loading");
            SceneManager.sceneUnloaded += OnSceneUnloaded;
            SceneManager.UnloadSceneAsync("LobbyScene");
            SceneManager.sceneLoaded += OnSceneLoaded;
            SceneManager.LoadScene("BasketballCourtScene", LoadSceneMode.Additive);
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

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "Lobby")
        {
        }
        else if (scene.name == "BasketballCourtScene")
        {
            // Host transitions state
            // TODO: We should add _isReady to PlayerModel and set to true only when scenes have loaded,
            // so host can wait for everyone to load before advancing state
            if (realtime.clientID == 0)
            {
                SpawnBalls(); // Only host spawns balls to avoid duplicates
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
            // Destroy non-player networked game objects
            foreach (var ball in FindObjectsOfType<Basketball>()) // TODO: Do this more performantly than using FindObjectsOfType()
            {
                Realtime.Destroy(ball.gameObject);
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

    public void GoToLobby()
    {
        StartCoroutine(GoToLobbyRoutine());
    }

    IEnumerator GoToLobbyRoutine()
    {
        // Change scenes
        yield return SceneManager.UnloadSceneAsync("MainMenuScene");
        yield return SceneManager.LoadSceneAsync("LobbyScene", LoadSceneMode.Additive);
    }

    private IEnumerator CountdownRoutine()
    {
        // Create a score rows for each player
        CreateScoreRows();

        // Do countdown
        TMP_Text label = GameObject.Find("CountdownLabel")?.GetComponent<TMP_Text>();

        for (int i = 5; i >= 1; i--)
        {
            if (label != null) label.text = i.ToString();
            yield return new WaitForSeconds(1f);
        }

        if (label != null) label.text = "Go!";

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
        TMP_Text label = GameObject.Find("CountdownLabel")?.GetComponent<TMP_Text>();
        label.text = "Time's Up!";
        yield return new WaitForSeconds(3f);
        // Determine winning player
        PlayerComponent winnerPlayerComponent = FindObjectsOfType<PlayerComponent>()
            .Where(pc => pc.Model != null)
            .OrderByDescending(pc => pc.Model.playerScore)
            .FirstOrDefault();
        GameObject.Find("CountdownLabel").GetComponent<TMPro.TMP_Text>().text = $"{winnerPlayerComponent.Model.playerName} Wins";
        yield return new WaitForSeconds(5f);

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

    //private void Update()
    //{
    //    switch (model.gameState)
    //    {
    //        case 0:
    //            break;
    //        case 1:
    //            break;
    //        case 2:
    //            break;
    //        case 3:
    //            break;
    //        case 4:
    //            break;
    //    }
    //}
}