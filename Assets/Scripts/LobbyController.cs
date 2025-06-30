using Normal.Realtime;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;


public class LobbyController : MonoBehaviour
{
    [SerializeField] TMP_Text matchCodeLabel;
    [SerializeField] private Transform playerListUI;
    [SerializeField] private GameObject playerRowPrefab;
    private Dictionary<int, GameObject> playerRowsDict = new(); // Dictionary for easily tracking playerRows by PlayerComponent.realtimeView.ownerID
    [SerializeField] GameObject uiBlocker;

    void Start()
    {
        // Set match code label
        matchCodeLabel.text = MatchManager.instance?.currentMatchCode ?? "???";

        // Handle already-spawned players
        bool foundSelf = false;
        foreach (var pc in FindObjectsOfType<PlayerComponent>())
        {
            HandlePlayerSpawned(pc);

            if (pc.realtimeView.isOwnedLocally)
            {
                foundSelf = true;

                // Reset model attributes
                pc.Model.playerScore = 0;
                pc.Model.playerShotStreak = 0;
            }
        }

        // Only spawn if not already spawned
        Realtime realtime = FindObjectOfType<Realtime>();
        if (!foundSelf)
        {
            if (realtime.connected)
            {
                SpawnPlayer();
            }
            else
            {
                realtime.didConnectToRoom += OnConnected;
            }
        }

        GameManager.instance.RefrestRayInteractors(); // Bandaid to fix issue w/ UI not interactable on returning to Lobby from Gameplay
    }

    void OnConnected(Realtime realtime)
    {
        realtime.didConnectToRoom -= OnConnected;
        SpawnPlayer();
    }

    void SpawnPlayer()
    {
        Realtime.Instantiate("Player", ownedByClient: true);
    }

    private void OnEnable()
    {
        PlayerComponent.OnPlayerSpawned += HandlePlayerSpawned;
        PlayerComponent.OnPlayerDespawned += HandlePlayerDespawned;
    }

    private void OnDisable()
    {
        PlayerComponent.OnPlayerSpawned -= HandlePlayerSpawned;
        PlayerComponent.OnPlayerDespawned -= HandlePlayerDespawned;
    }

    void HandlePlayerSpawned(PlayerComponent pc)
    {
        // Create row for player in UI
        var row = Instantiate(playerRowPrefab, playerListUI);

        var voice = pc.GetComponent<RealtimeAvatarVoice>();
        var rowScript = row.GetComponent<LobbyPlayerRow>();

        rowScript.SetPlayer(pc.Model.playerName, voice);

        // store ownerID and row in dict for easy reference
        playerRowsDict[pc.realtimeView.ownerID] = row;
    }

    void HandlePlayerDespawned(PlayerComponent pc)
    {
        // Destroy row for player in UI
        if (playerRowsDict.TryGetValue(pc.realtimeView.ownerID, out var row))
        {
            Destroy(row);
            playerRowsDict.Remove(pc.realtimeView.ownerID);
        }
    }

    public void OnStartMatchClicked()
    {
        // Only host can start game
        if (GameManager.instance.realtime.clientID == 0)
        {
            GameManager.instance.realtimeView.RequestOwnership();
            GameManager.instance.Model.gameState = 1; // Model will send event on value change causing all clients to load Basketball court scene
            Debug.Log("Host clicked on start match button");
        }
    }

    public void OnBackClicked()
    {
        StartCoroutine(BackRoutine());
    }

    IEnumerator BackRoutine()
    {
        uiBlocker.SetActive(true);

        // Fade out
        yield return TransitionManager.instance.Fade(1f, 0.5f);

        // Disconnect from Normcore room
        MatchManager.instance.Disconnect();

        // Change scenes
        SceneManager.UnloadScene("LobbyScene");
        SceneManager.LoadScene("MainMenuScene", LoadSceneMode.Additive);

        // Fade in
        yield return TransitionManager.instance.Fade(0f, 0.5f);
    }
}


