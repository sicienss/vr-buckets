using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Normal.Realtime;
using System.Linq;

public class PlayerComponent : RealtimeComponent<PlayerModel>
{
    public PlayerModel Model => model; // Getter to access the model from outside this class
    public static event Action<PlayerComponent> OnPlayerSpawned;
    public static event Action<PlayerComponent> OnPlayerDespawned;
    private static readonly string[] namePool = {
    "BigBaller", "SkyJam", "DunkMan", "BigJoe",
    "AirAce", "Hoopster", "Bucketz", "FastBreak",
    "AlleyKing", "JellyRoll", "PosterBoy", "SlamUnit",
    "SpinMove", "BoardLord", "TripleDub", "IsoJoe",
    "Rainmaker", "NoLook", "HangTime", "ThePaint",
    "CashOut", "AndOne", "Crossover", "FullCourt",
    "Baseline", "BouncePass", "Backboard", "ClutchCity",
    "SwishZone", "HeatCheck", "ZoneCrusher", "InTheLab"
};
    // ^ TODO: Move this somewhere better
    [SerializeField] GameObject xrRigRoot;
    [SerializeField] Transform headAnchor;
    [SerializeField] Transform leftControllerAnchor;
    [SerializeField] Transform rightControllerAnchor;
    [SerializeField] Transform headTransform;
    [SerializeField] Transform leftHandTransform;
    [SerializeField] Transform rightHandTransform;


    void Start()
    {
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
        }

        // Rig is only active is realtimeView is owned locally
        if (realtimeView.isOwnedLocally)
        {
            xrRigRoot.SetActive(true);
        }
        else
        {
            xrRigRoot.SetActive(false);
        }

        OnPlayerSpawned?.Invoke(this);
    }

    private void OnPlayerNameChanged(PlayerModel model, string name)
    {
        Debug.Log($"[PlayerComponent] Name changed to: {name}");
    }

    private void OnDestroy()
    {
        OnPlayerDespawned?.Invoke(this);
    }

    bool hasRequestedOwnership = false;
    private void Update()
    {
        if (!realtimeView.isOwnedLocally) return;

        if (!hasRequestedOwnership)
        {
            headTransform.GetComponent<RealtimeTransform>()?.RequestOwnership();
            leftHandTransform.GetComponent<RealtimeTransform>()?.RequestOwnership();
            rightHandTransform.GetComponent<RealtimeTransform>()?.RequestOwnership();

            headTransform.GetComponent<RealtimeView>()?.RequestOwnership();
            leftHandTransform.GetComponent<RealtimeView>()?.RequestOwnership();
            rightHandTransform.GetComponent<RealtimeView>()?.RequestOwnership();
        }

        // Owner updates head and hands, and the transform is synced on network via Normcore's RealtimeTransform
        headTransform.position = headAnchor.position;
        headTransform.rotation = headAnchor.rotation;
        leftHandTransform.position = leftControllerAnchor.position;
        leftHandTransform.rotation = leftControllerAnchor.rotation;
        rightHandTransform.position = rightControllerAnchor.position;
        rightHandTransform.rotation = rightControllerAnchor.rotation;
    }
}