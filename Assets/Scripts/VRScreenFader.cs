using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class VRScreenFader : MonoBehaviour
{
    public Renderer fadeQuadRenderer;
    private Material fadeMaterial;

    private void Start()
    {
        fadeMaterial = fadeQuadRenderer.material;
        SetAlpha(TransitionManager.instance.alpha);
    }

    void Update()
    {
        SetAlpha(TransitionManager.instance.alpha);
    }

    private void SetAlpha(float alpha)
    {
        fadeMaterial.SetColor("_BaseColor", new Color(0, 0, 0, alpha));
    }
}