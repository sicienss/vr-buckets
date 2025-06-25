using System;
using System.Linq;
using UnityEngine;
using Normal.Realtime;

public class MatchManager : MonoBehaviour
{
    public static MatchManager instance;
    [SerializeField] public Realtime realtime;
    public string currentMatchCode { get; private set; }

    // Events to notify UI (for example, MainMenuController)
    public event Action OnJoinSuccess;
    public event Action OnJoinFailure;


    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
    }

    public void TryJoinRoom(string code)
    {
        currentMatchCode = code;

        realtime.didConnectToRoom += OnJoinRoomSuccess;
        realtime.didDisconnectFromRoom += OnJoinRoomFailed;

        realtime.Connect(code);
    }

    private void OnJoinRoomSuccess(Realtime realtime)
    {
        realtime.didDisconnectFromRoom -= OnJoinRoomFailed;
        realtime.didConnectToRoom -= OnJoinRoomSuccess;

        Debug.Log("Joined match: " + currentMatchCode);
        OnJoinSuccess?.Invoke();
    }

    private void OnJoinRoomFailed(Realtime realtime)
    {
        realtime.didConnectToRoom -= OnJoinRoomSuccess;
        realtime.didDisconnectFromRoom -= OnJoinRoomFailed;

        Debug.LogWarning("Failed to join match: " + currentMatchCode);
        OnJoinFailure?.Invoke();
    }

    public void Disconnect()
    {
        if (realtime != null && realtime.connected)
        {
            realtime.Disconnect(); // Leave the room -- NOTE: This also automatically destroyed Realtime.Instantiated objects
        }
    }

    public string GenerateMatchCode(int length)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        System.Random random = new System.Random();
        return new string(Enumerable.Repeat(chars, length)
            .Select(s => s[random.Next(s.Length)]).ToArray());
    }
}