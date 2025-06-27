using Normal.Realtime;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.SocialPlatforms;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using static UnityEngine.UI.GridLayoutGroup;

[RequireComponent(typeof(XRGrabInteractable))]
public class BallPhysicsControl : MonoBehaviour
{
    private Rigidbody rb;
    private XRGrabInteractable grab;


    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        grab = GetComponent<XRGrabInteractable>();

        grab.movementType = XRBaseInteractable.MovementType.Instantaneous;
        grab.smoothPosition = true;
        grab.smoothPositionAmount = 20f; // Higher = faster
        grab.tightenPosition = 0.5f;     // 0�1, higher = less overshoot

        grab.smoothRotation = true;
        grab.smoothRotationAmount = 20f;
        grab.tightenRotation = 0.5f;

        grab.selectEntered.AddListener(OnGrabbed);
        grab.selectExited.AddListener(OnReleased);
    }

    private void OnGrabbed(SelectEnterEventArgs args)
    {
        // Remember owner
        Basketball basketball = GetComponent<Basketball>();
        basketball.owner = args.interactorObject.transform.GetComponentInParent<PlayerComponent>();

        // Take ownership
        GetComponent<RealtimeView>()?.RequestOwnership(); // local client is requesting to take ownership of the networked object that this RealtimeView is attached to
    }

    private void OnReleased(SelectExitEventArgs args)
    {
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
}
