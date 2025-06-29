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
        // Defer activation until ownership is known
        StartCoroutine(WaitForOwnershipThenSetup());
    }

    private IEnumerator WaitForOwnershipThenSetup()
    {
        // Wait until the RealtimeView is owned locally or definitely not
        while (!realtimeView.isOwnedLocally && !realtimeView.isOwnedRemotely)
            yield return null;

        if (realtimeView.isOwnedLocally)
        {
            xrRigRoot.SetActive(true);

            headTransform.GetComponent<RealtimeTransform>()?.RequestOwnership();
            leftHandTransform.GetComponent<RealtimeTransform>()?.RequestOwnership();
            rightHandTransform.GetComponent<RealtimeTransform>()?.RequestOwnership();
        }
        else
        {
            xrRigRoot.SetActive(false);
        }

        // Anchors should still be visible for all players
        headAnchor.gameObject.SetActive(true);
        leftControllerAnchor.gameObject.SetActive(true);
        rightControllerAnchor.gameObject.SetActive(true);
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
        OnPlayerDespawned?.Invoke(this);
    }

    private void Update()
    {
        if (!realtimeView.isOwnedLocally) return;

        // Owner updates head and hands, and the transform is synced on network via Normcore's RealtimeTransform
        headTransform.position = headAnchor.position;
        headTransform.rotation = headAnchor.rotation;
        leftHandTransform.position = leftControllerAnchor.position;
        leftHandTransform.rotation = leftControllerAnchor.rotation;
        rightHandTransform.position = rightControllerAnchor.position;
        rightHandTransform.rotation = rightControllerAnchor.rotation;

        // DEBUG -- Input handling
        if (Input.GetKeyDown(KeyCode.B))
        {
            // Teleport ball above hoop
            Basketball basketball = FindObjectsOfType<Basketball>().First(b => !b.isHeld && b.GetComponent<Rigidbody>().linearVelocity.y == 0);
            basketball.owner = this;
            basketball.GetComponent<RealtimeView>()?.RequestOwnership(); // local client is requesting to take ownership of the networked object that this RealtimeView is attached to
            basketball.GetComponent<RealtimeTransform>()?.RequestOwnership(); // also need to request ownership of the transform for pos and rot to update
            basketball.transform.position = GameObject.Find("HoopAimPoint").transform.position + Vector3.up * 0.5f;
        }
    }
}