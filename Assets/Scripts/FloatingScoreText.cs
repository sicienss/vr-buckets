using UnityEngine;

public class FloatingScoreText : MonoBehaviour
{
    public float riseSpeed = 1f;
    public float fadeDuration = 1f;

    private TMPro.TMP_Text text;
    private Color originalColor;
    private float timer;

    void Start()
    {
        text = GetComponentInChildren<TMPro.TMP_Text>();
        originalColor = text.color;
        Destroy(gameObject, fadeDuration);
    }

    void Update()
    {
        transform.position += Vector3.up * riseSpeed * Time.deltaTime;
        timer += Time.deltaTime;
        float alpha = Mathf.Lerp(originalColor.a, 0, timer / fadeDuration);
        text.color = new Color(originalColor.r, originalColor.g, originalColor.b, alpha);
    }
}
