using Normal.Realtime;
using System;
using UnityEngine;

public class PlayerComponent : RealtimeComponent<PlayerModel>
{
    public PlayerModel Model => model; // Getter to access the model from outside this class
    public static event Action<PlayerComponent> OnPlayerSpawned;
    public static event Action<PlayerComponent> OnPlayerDespawned;
    private static readonly string[] namePool = { "BigBaller", "SkyJam", "DunkMan", "BigJoe", "AirAce", "Hoopster", "Bucketz", "FastBreak" };
    // ^ TODO: Move this somewhere better
    [SerializeField] GameObject xrRigRoot;

    [SerializeField] Transform leftControllerAnchor;
    [SerializeField] Transform rightControllerAnchor;
    [SerializeField] Transform leftHandTransform;
    [SerializeField] Transform rightHandTransform;

    void Start()
    {
        if (realtimeView.isOwnedLocally)
        {
            // Enable XR rig for local input
            xrRigRoot.SetActive(true);
            leftControllerAnchor.gameObject.SetActive(true);
            rightControllerAnchor.gameObject.SetActive(true);

            // Ensure hands are visible to self
            leftHandTransform.gameObject.SetActive(true);
            rightHandTransform.gameObject.SetActive(true);
        }
        else
        {
            // Disable camera and XR input for remote players
            xrRigRoot.SetActive(false);
            leftControllerAnchor.gameObject.SetActive(false);
            rightControllerAnchor.gameObject.SetActive(false);

            // Ensure hands are visible to others
            leftHandTransform.gameObject.SetActive(true);
            rightHandTransform.gameObject.SetActive(true);
        }
    }

    protected override void OnRealtimeModelReplaced(PlayerModel previousModel, PlayerModel currentModel)
    {
        if (previousModel != null)
        {
            previousModel.playerNameDidChange -= OnPlayerNameChanged;
        }

        if (currentModel != null)
        {
            currentModel.playerNameDidChange += OnPlayerNameChanged;

            // If this is the local player and no name is set yet, assign a random one
            if (realtimeView.isOwnedLocally && string.IsNullOrEmpty(model.playerName))
            {
                string randomName = namePool[UnityEngine.Random.Range(0, namePool.Length)];
                model.playerName = randomName;
            }

            OnPlayerNameChanged(model, model.playerName);
        }

        OnPlayerSpawned?.Invoke(this);
    }

    private void OnPlayerNameChanged(PlayerModel model, string name)
    {
        Debug.Log($"[PlayerComponent] Name changed to: {name}");
    }

    private void OnDestroy()
    {
        if (realtimeView.isOwnedLocally)
        {
        }

        OnPlayerDespawned?.Invoke(this);
    }

    private void Update()
    {
        if (!realtimeView.isOwnedLocally) return;

        // Owner update hand position
        leftHandTransform.position = leftControllerAnchor.position;
        leftHandTransform.rotation = leftControllerAnchor.rotation;

        rightHandTransform.position = rightControllerAnchor.position;
        rightHandTransform.rotation = rightControllerAnchor.rotation;
    }
}