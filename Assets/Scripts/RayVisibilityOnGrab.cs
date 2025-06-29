using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Interactors.Visuals;

[RequireComponent(typeof(XRRayInteractor), typeof(XRInteractorLineVisual))]
public class RayVisibilityOnGrab : MonoBehaviour
{
    private XRRayInteractor rayInteractor;
    private XRInteractorLineVisual lineVisual;
    private LineRenderer lineRenderer;

    private void Awake()
    {
        rayInteractor = GetComponent<XRRayInteractor>();
        lineVisual = GetComponent<XRInteractorLineVisual>();
        lineRenderer = GetComponent<LineRenderer>();
    }

    private void OnEnable()
    {
        rayInteractor.selectEntered.AddListener(OnGrab);
        rayInteractor.selectExited.AddListener(OnRelease);

        rayInteractor.hoverEntered.AddListener(OnHoverEnter);
        rayInteractor.hoverExited.AddListener(OnHoverExit);
    }

    private void OnDisable()
    {
        rayInteractor.selectEntered.RemoveListener(OnGrab);
        rayInteractor.selectExited.RemoveListener(OnRelease);

        rayInteractor.hoverEntered.RemoveListener(OnHoverEnter);
        rayInteractor.hoverExited.RemoveListener(OnHoverExit);
    }

    private void OnGrab(SelectEnterEventArgs args)
    {
        lineVisual.enabled = false;
    }

    private void OnRelease(SelectExitEventArgs args)
    {
        lineVisual.enabled = true;
    }

    private void OnHoverEnter(HoverEnterEventArgs args)
    {
    }

    private void OnHoverExit(HoverExitEventArgs args)
    {
    }

    private void SetRayColor(Color color)
    {
        if (lineRenderer != null)
        {
            lineRenderer.startColor = color;
            lineRenderer.endColor = color;
        }
    }
}
