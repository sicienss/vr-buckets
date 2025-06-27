using Normal.Realtime;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Rendering;
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
    public bool hasScored = false; // bool for tracking whether ball scored, used for (1) preventing multiple scoring when bounding around rim, (2) blocking streak reset on collision w/ ground
    public bool enteredTop = false; // bool for tracking whether ball went through top trigger, uses for (1) preventing false positive scores, (2) disabling HoopAssistZone
    public float shotDistance; // distance the ball was shot from, used to determine score

    // AUDIO -- TODO: move this to an audio manager 
    [SerializeField] AudioSource audioSource;
    [SerializeField] AudioClip ball_bounce_soft;
    [SerializeField] AudioClip ball_bounce_hard;
    [SerializeField] AudioClip rim_clang;
    [SerializeField] AudioClip rim_ding;
    [SerializeField] AudioClip backboard_thud;
    [SerializeField] AudioClip net_swish;
    [SerializeField] AudioClip grab_whoosh;
    [SerializeField] AudioClip hand_snap;
    float volume = 1f;


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
        hasScored = false; // reset

        // Remember owner
        owner = args.interactorObject.transform.GetComponentInParent<PlayerComponent>();

        // Take ownership
        GetComponent<RealtimeView>()?.RequestOwnership(); // local client is requesting to take ownership of the networked object that this RealtimeView is attached to
        GetComponent<RealtimeTransform>()?.RequestOwnership(); // also need to request ownership of the transform for pos and rot to update

        isHeld = true;
        timeSinceRelease = 0f;

        rb.isKinematic = true;
        rb.useGravity = false;

        // SFX
        PlayGrab();
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
            yield break; // Aim is too far off — don't assist
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
        shotDistance = horizontalDistance;

        // Determine parameter t to help the player more the farther they are away
        float minDistance = 1.5f;
        float maxDistance = 6f;
        float t = Mathf.InverseLerp(minDistance, maxDistance, horizontalDistance);
        float correctionFactor = Mathf.Lerp(0.5f, 0.9f, t); // from 50% help to 90% depending on distance

        // Solve for initial velocity needed to reach target under gravity, assuming some clearance height
        float verticalDist = toTarget.y;

        float vx = horizontalDistance / timeToTarget;
        float arcClearance = Mathf.Lerp(0.5f, 1.5f, t); // how high the ball should peak above the hoop; lower arc for close shots, higher for far ones
        float adjustedVerticalDist = verticalDist + arcClearance;
        float vy = (adjustedVerticalDist + 0.5f * gravity * timeToTarget * timeToTarget) / timeToTarget;

        Vector3 throwDirection = horizontal.normalized * vx + Vector3.up * vy;

        // Recalculate velocities with new timeToTarget
        vx = horizontalDistance / timeToTarget;
        vy = (verticalDist + 0.5f * gravity * timeToTarget * timeToTarget) / timeToTarget;
        throwDirection = horizontal.normalized * vx + Vector3.up * vy;

        Vector3 original = rb.linearVelocity;
        Vector3 finalVelocity = Vector3.Lerp(original, throwDirection, correctionFactor);
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
            Respawn();
        }
        else if (timeSinceRelease > maxTimeBeforeRespawn)
        {
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

    public void ResetTopAfterDelay()
    {
        StartCoroutine(ResetTopAfterDelayRoutine());
    }

    IEnumerator ResetTopAfterDelayRoutine()
    {
        yield return new WaitForSeconds(1.0f);
        enteredTop = false;
    }

    private void OnCollisionEnter(Collision collision)
    {
        // Reset shot streak
        if (collision.gameObject.CompareTag("Ground"))
        {
            if (owner != null && !hasScored)
            {
                var playerComponent = owner.GetComponent<PlayerComponent>();

                // Only the client that owns the player updates their own model
                if (playerComponent != null && playerComponent.realtimeView.isOwnedLocally)
                {
                    playerComponent.Model.playerShotStreak = 0;
                }
            }
        }

        // SFX
        float impact = collision.relativeVelocity.magnitude;

        if (collision.gameObject.CompareTag("Ground"))
            PlayBounceSound(impact);
        else if (collision.gameObject.CompareTag("Rim"))
            PlayRimHitSound(impact);
        else if (collision.gameObject.CompareTag("Backboard"))
            PlayBackboardHitSound();
    }

    public  void PlayBounceSound(float impact)
    {
        if (impact > 1f)
        {
            audioSource.PlayOneShot(ball_bounce_hard, volume);
        }
        else
        {
            audioSource.PlayOneShot(ball_bounce_soft, volume);
        }
    }

    public void PlayRimHitSound(float impact)
    {
        if (impact > 1f)
        {
            audioSource.PlayOneShot(rim_ding, volume);
        }
        else
        {
            audioSource.PlayOneShot(rim_clang, volume);
        }
    }

    public void PlayBackboardHitSound()
    {
        audioSource.PlayOneShot(backboard_thud, volume);
    }

    public void PlaySwish()
    {
        audioSource.PlayOneShot(net_swish, volume);
    }

    public void PlayGrab()
    {
        audioSource.PlayOneShot(grab_whoosh, volume);
    }
}