using Normal.Realtime;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.SocialPlatforms;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

[RequireComponent(typeof(XRGrabInteractable))]
public class Basketball : MonoBehaviour
{
    public PlayerComponent owner;  // This gets set when grabbed

    public Vector3 originalPosition;
    private Quaternion originalRotation;
    private RealtimeView realtimeView;
    private Rigidbody rb;
    private XRGrabInteractable grab;

    [SerializeField] private float respawnDistance = 10f;
    [SerializeField] private float maxTimeBeforeRespawn = 10f;

    private float timeSinceRelease;
    private bool isHeld = false;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        grab = GetComponent<XRGrabInteractable>();
        realtimeView = GetComponent<RealtimeView>();

        grab.movementType = XRBaseInteractable.MovementType.Instantaneous;
        grab.smoothPosition = true;
        grab.smoothPositionAmount = 20f; // Higher = faster
        grab.tightenPosition = 0.5f;     // 0�1, higher = less overshoot

        grab.smoothRotation = true;
        grab.smoothRotationAmount = 20f;
        grab.tightenRotation = 0.5f;

        grab.selectEntered.AddListener(OnGrab);
        grab.selectExited.AddListener(OnRelease);
    }

    private void Start()
    {
        originalPosition = transform.position;
        originalRotation = transform.rotation;
    }

    private void OnDestroy()
    {
        grab.selectEntered.RemoveListener(OnGrab);
        grab.selectExited.RemoveListener(OnRelease);
    }

    private void OnGrab(SelectEnterEventArgs args)
    {
        // Remember owner
        owner = args.interactorObject.transform.GetComponentInParent<PlayerComponent>();

        // Take ownership
        GetComponent<RealtimeView>()?.RequestOwnership(); // local client is requesting to take ownership of the networked object that this RealtimeView is attached to
        GetComponent<RealtimeTransform>()?.RequestOwnership(); // also need to request ownership of the transform for pos and rot to update

        isHeld = true;
        timeSinceRelease = 0f;

        rb.isKinematic = true;
        rb.useGravity = false;
    }

    private void OnRelease(SelectExitEventArgs args)
    {
        isHeld = false;
        timeSinceRelease = 0f;

        rb.isKinematic = false;
        rb.useGravity = true;

        StartCoroutine(AdjustThrowDirectionNextFrame());
    }

    private IEnumerator AdjustThrowDirectionNextFrame()
    {
        // Wait until next frame so XR can apply velocity
        yield return null;

        // ADJUST BALL TRAJECTORY -- NAIVE APPROACH
        //Vector3 intendedDir = rb.linearVelocity.normalized;
        //Vector3 hoopDir = (GameObject.Find("HoopAimPoint").transform.position - transform.position).normalized;
        //Vector3 correctedDir = Vector3.Lerp(intendedDir, hoopDir, 0.50f); // 50% correction

        //rb.linearVelocity = correctedDir * rb.linearVelocity.magnitude;


        // ADJUST BALL TRAJECTORY -- SOPHISTICATED APPROACH
        Vector3 start = transform.position;
        Vector3 hoop = GameObject.Find("HoopAimPoint").transform.position;

        // Choose a target arc point a bit above the hoop
        Vector3 target = hoop + Vector3.up * 1.5f;

        // Get original throw direction and direction to hoop
        Vector3 originalDir = rb.linearVelocity.normalized;
        Vector3 hoopDir = (target - start).normalized;

        // Only apply arc correction if within angle threshold
        float angleToHoop = Vector3.Angle(originalDir, hoopDir);
        if (angleToHoop > 45f)
        {
            yield break; // Too far off — don't assist
        }

        // Physics constants
        float gravity = Mathf.Abs(Physics.gravity.y);

        Vector3 toTarget = target - start;

        // Set time to target based on distance
        Vector3 horizontal = new Vector3(toTarget.x, 0f, toTarget.z);
        float horizontalDistance = horizontal.magnitude;
        float baseTime = 0.35f; // minimum duration for close throws
        float timePerMeter = 0.15f;
        float timeToTarget = baseTime + horizontalDistance * timePerMeter;

        // Solve for initial velocity needed to reach target under gravity
        float verticalDist = toTarget.y;

        float vx = horizontalDistance / timeToTarget;
        float vy = (verticalDist + 0.5f * gravity * timeToTarget * timeToTarget) / timeToTarget;

        Vector3 throwDirection = horizontal.normalized * vx + Vector3.up * vy;

        // Combine: 70% from calculated arc, 30% from original intent
        Vector3 original = rb.linearVelocity;
        Vector3 finalVelocity = Vector3.Lerp(original, throwDirection, 0.7f);

        rb.linearVelocity = finalVelocity;
    }

    private void Update()
    {
        if (!realtimeView.isOwnedLocally || isHeld)
            return;

        timeSinceRelease += Time.deltaTime;

        float distance = Vector3.Distance(transform.position, originalPosition);
        if (distance > respawnDistance)
        {
            Debug.Log("Respawn because distance from original position too great");
            Respawn();
        }
        else if (timeSinceRelease > maxTimeBeforeRespawn)
        {
            Debug.Log("Respawn because time since release is too large");
            Respawn();
        }
    }

    private void Respawn()
    {
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.isKinematic = true; // Reset before move to avoid physics issues

        transform.position = originalPosition;
        transform.rotation = originalRotation;

        rb.isKinematic = false;
        rb.useGravity = true;

        timeSinceRelease = 0f;
    }

    private void OnCollisionEnter(Collision collision)
    {
        // Reset shot streak
        if (collision.gameObject.CompareTag("Ground"))
        {
            if (owner != null)
            {
                var playerComponent = owner.GetComponent<PlayerComponent>();

                // Only the client that owns the player updates their own model
                if (playerComponent != null && playerComponent.realtimeView.isOwnedLocally)
                {
                    playerComponent.Model.playerShotStreak = 0;
                }
            }
        }
    }
}