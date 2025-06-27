using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

[RequireComponent(typeof(XRRayInteractor))]
public class RayHighlightOnHover : MonoBehaviour
{
    [SerializeField] private LineRenderer lineRenderer;
    [SerializeField] private Color defaultColor = Color.white;
    [SerializeField] private Color highlightColor = Color.green;

    private XRRayInteractor interactor;

    private void Awake()
    {
        interactor = GetComponent<XRRayInteractor>();
        if (lineRenderer == null)
            lineRenderer = GetComponent<LineRenderer>();
    }

    private void Update()
    {
        //bool hovering3D = interactor.hasHover;
        //bool hoveringUI = interactor.TryGetCurrentUIRaycastResult(out var uiHit) && uiHit.gameObject != null;

        //if (hovering3D || hoveringUI)
        //    SetColor(highlightColor);
        //else
        //    SetColor(defaultColor);
    }

    private void SetColor(Color color)
    {
        lineRenderer.startColor = color;
        lineRenderer.endColor = color;
    }
}