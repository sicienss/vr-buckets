using Normal.Realtime;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;


[RequireComponent(typeof(XRGrabInteractable))]
public class BallPhysicsControl : MonoBehaviour
{
    private Rigidbody rb;
    private XRGrabInteractable grab;


    private void Awake()
    {
    }
}
