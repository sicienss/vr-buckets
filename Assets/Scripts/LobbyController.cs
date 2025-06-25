using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Normal.Realtime;
using UnityEngine.SceneManagement;


public class LobbyController : MonoBehaviour
{
    [SerializeField] TMP_Text matchCodeLabel;
    [SerializeField] private Transform playerListUI;
    [SerializeField] private GameObject playerRowPrefab;
    private Dictionary<int, GameObject> playerRowsDict = new(); // Dictionary for easily tracking playerRows by PlayerComponent.realtimeView.ownerID

    void Start()
    {
        // Set match code label
        matchCodeLabel.text = MatchManager.instance?.currentMatchCode ?? "???";

        // Wait until the room is fully connected before spawning
        Realtime realtime = FindObjectOfType<Realtime>();
        if (realtime.connected)
        {
            SpawnPlayer();
        }
        else
        {
            realtime.didConnectToRoom += OnConnected;
        }
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
        GameObject row = Instantiate(playerRowPrefab, playerListUI);
        row.GetComponent<LobbyPlayerRow>().SetPlayerName(pc.Model.playerName);
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
        // Change scenes
        SceneManager.UnloadScene("LobbyScene");
        SceneManager.LoadScene("BasketballCourtScene", LoadSceneMode.Additive);
    }

    public void OnBackClicked()
    {
        // Disconnect from Normcore room
        MatchManager.instance.Disconnect();

        // Change scenes
        SceneManager.UnloadScene("LobbyScene");
        SceneManager.LoadScene("MainMenuScene", LoadSceneMode.Additive);
    }
}


