using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class TransitionManager : MonoBehaviour
{
    public static TransitionManager instance;
    private Coroutine fadeRoutine;
    public float alpha;


    private void Awake()
    {
        instance = this;
        alpha = 1.0f; // Start black
    }

    public Coroutine Fade(float targetAlpha, float duration)
    {
        if (fadeRoutine != null)
        {
            StopCoroutine(fadeRoutine);
        }
        fadeRoutine = StartCoroutine(FadeRoutine(targetAlpha, duration));
        return fadeRoutine;
    }

    private IEnumerator FadeRoutine(float targetAlpha, float duration)
    {
        float startAlpha = alpha;
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / duration;
            alpha = Mathf.Lerp(startAlpha, targetAlpha, t);
            yield return null;
        }
    }
}
