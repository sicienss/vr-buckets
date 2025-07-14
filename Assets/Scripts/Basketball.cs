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
using UnityEngine.XR.Interaction.Toolkit.Interactors;


[RequireComponent(typeof(XRGrabInteractable))]
public class Basketball : MonoBehaviour
{
    public PlayerComponent owner;  // This gets set when grabbed

    public Vector3 originalPosition;
    private Quaternion originalRotation;
    public RealtimeView realtimeView;
    private Rigidbody rb;
    private XRGrabInteractable grab;

    [SerializeField] private float respawnDistance = 10f;
    [SerializeField] private float maxTimeBeforeRespawn = 7.5f;

    private float timeSinceRelease;
    public bool isHeld = false;
    public bool isThrown = false;
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
        isThrown = true;
        timeSinceRelease = 0f;

        rb.isKinematic = false;
        rb.useGravity = true;

        StartCoroutine(AdjustThrowDirectionNextFrame(args));
    }

    private IEnumerator AdjustThrowDirectionNextFrame(SelectExitEventArgs args)
    {
        // Wait a frame so XR Toolkit can apply built-in physics
        yield return null;


        //// Adjust velocity by helping on Y-axis slightly
        //rb.linearVelocity = new Vector3(rb.linearVelocity.x, rb.linearVelocity.y * 1.25f, rb.linearVelocity.z);
    }

    private void Update()
    {
        // Skip if ball is held or was just thrown
        if (isHeld || isThrown)
            return;

        // If no one owns this object, allow the host (clientID == 0) to control it
        bool shouldMonitor = realtimeView.isOwnedLocally || (realtimeView.ownerID == -1 && GameManager.instance.realtime.clientID == 0);

        if (!shouldMonitor)
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
        yield return new WaitForSeconds(2.0f);
        enteredTop = false;
    }

    private void OnCollisionEnter(Collision collision)
    {
        // Reset shot streak
        if (collision.gameObject.CompareTag("Ground"))
        {
            if (owner != null && !hasScored)
            {
                // Only the client that owns the player updates their own model
                if (owner.realtimeView.isOwnedLocally)
                {
                    owner.Model.playerShotStreak = 0;
                }
            }

            isThrown = false;
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