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
        // Get current match code from MatchManager
        if (MatchManager.instance != null)
        {
            matchCodeLabel.text = $"{MatchManager.instance.currentMatchCode}";
        }
        else
        {
            matchCodeLabel.text = "???";
            Debug.LogWarning("MatchManager instance is missing!");
        }

        // Subscribe
        MatchManager.instance.realtime.didConnectToRoom += OnConnected;
    }

    void OnConnected(Realtime realtime)
    {
        // Unsubscribe
        MatchManager.instance.realtime.didConnectToRoom -= OnConnected;

        // Instantiate networked Player prefab -- NOTE: Down stream event handlers will spawn rows in UI to show players in room
        Realtime.Instantiate("Player", ownedByClient: true, preventOwnershipTakeover: true);
    }

    private void OnEnable()
    {
        PlayerComponent.OnPlayerSpawned += HandlePlayerSpawned;
        PlayerComponent.OnPlayerDespawned += HandlePlayerDespawned;
    }

    private void OnDisable()
    {
        PlayerComponent.OnPlayerSpawned -= HandlePlayerSpawned;
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


