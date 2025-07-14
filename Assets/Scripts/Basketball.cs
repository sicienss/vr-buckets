using Normal.Realtime;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Rendering;
using UnityEngine.XR;
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

        //StartCoroutine(AdjustThrowDirectionNextFrame(args));

        // Identify which hand released the object
        var interactor = args.interactorObject.transform;
        if (interactor == null) return;

        XRNode releasingNode;

        if (interactor.name.ToLower().Contains("left"))
            releasingNode = XRNode.LeftHand;
        else if (interactor.name.ToLower().Contains("right"))
            releasingNode = XRNode.RightHand;
        else
            return; // Unknown hand

        // Get the releasing device
        var device = InputDevices.GetDeviceAtXRNode(releasingNode);

        // Get velocity from the releasing hand
        Vector3 releaseVelocity = Vector3.zero;
        Vector3 releaseAngularVelocity = Vector3.zero;
        device.TryGetFeatureValue(CommonUsages.deviceVelocity, out releaseVelocity);
        device.TryGetFeatureValue(CommonUsages.deviceAngularVelocity, out releaseAngularVelocity);

        // Adjust velocity for gamefeel -- translate some of the XZ force to Y axis, and boost XZ
        Vector3 adjustedVelocity = releaseVelocity;
        float xzMagnitude = new Vector3(releaseVelocity.x, 0f, releaseVelocity.z).magnitude;
        if (releaseVelocity.y > 1f)
        {
            float yBoost = xzMagnitude * 0.66f;
            adjustedVelocity.y += yBoost;
        }
        adjustedVelocity.x *= 1.5f;
        adjustedVelocity.z *= 1.5f;

        // Apply velocity to rb
        rb.linearVelocity = adjustedVelocity;
        rb.angularVelocity = releaseAngularVelocity;
    }

    private IEnumerator AdjustThrowDirectionNextFrame(SelectExitEventArgs args)
    {
        yield return null; // Let XR Toolkit apply its velocity

        Vector3 velocity = rb.linearVelocity;

        // Take some of the horizontal force (XZ plane)
        Vector3 horizontal = new Vector3(velocity.x, 0f, velocity.z);
        float horizontalMagnitude = horizontal.magnitude;

        // Convert a fraction of horizontal force into vertical boost
        float arcFactor = 0.5f; // tune this!
        float extraY = horizontalMagnitude * arcFactor;

        // Apply it
        velocity.y += extraY;

        //// Also boost XZ
        //velocity.x *= 1.25f;
        //velocity.z *= 1.25f;

        rb.linearVelocity = velocity;
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
        {
            PlayBackboardHitSound();

            // Very subtle redirect toward hoop
            GameObject hoopCenter = GameObject.Find("Rim");
            Vector3 toHoop = (hoopCenter.transform.position - transform.position).normalized;
            Vector3 currentVelocity = rb.linearVelocity;

            // Blend current velocity with hoop direction
            float assistStrength = 0.2f; // Tune this! 0 = no help, 1 = full redirect
            Vector3 assistedVelocity = Vector3.Lerp(currentVelocity, toHoop * currentVelocity.magnitude, assistStrength);

            rb.linearVelocity = assistedVelocity;
        }
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