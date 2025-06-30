using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Normal.Realtime;
using System.Linq;
using UnityEngine.InputSystem;

public class DebugMultiplayerInput : MonoBehaviour
{
    private DebugInputActions debugInput;

    private void OnEnable()
    {
        debugInput = new DebugInputActions();
        debugInput.Enable();

        debugInput.DebugControls.StartMatch.performed += ctx => FindObjectOfType<LobbyController>().OnStartMatchClicked();
        debugInput.DebugControls.Score.performed += ctx => Score();
    }

    private void OnDisable()
    {
        debugInput.Disable();
    }

    private void Score()
    {
        if (GetComponent<PlayerComponent>().realtimeView.ownerIDSelf == 0)
        {
            // Teleport ball above hoop
            Basketball basketball = FindObjectsOfType<Basketball>().First(b => !b.isHeld && b.GetComponent<Rigidbody>().linearVelocity.y == 0);
            basketball.owner = GetComponent<PlayerComponent>();
            basketball.GetComponent<RealtimeView>()?.RequestOwnership(); // local client is requesting to take ownership of the networked object that this RealtimeView is attached to
            basketball.GetComponent<RealtimeTransform>()?.RequestOwnership(); // also need to request ownership of the transform for pos and rot to update
            basketball.transform.position = GameObject.Find("HoopAimPoint").transform.position + Vector3.up * 4f;
        }
    }
}