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
    }
}
