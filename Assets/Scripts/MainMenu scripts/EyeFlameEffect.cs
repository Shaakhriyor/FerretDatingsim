using UnityEngine;
using UnityEngine.UI;

public class EyeFlameEffect : MonoBehaviour
{
    private Image eyeImage;
    private RectTransform rectTransform;
    private Vector3 originalScale;

    [Header("Flame Pulse & Scale Settings")]
    public float scalePulseAmount = 0.06f; // How much the eyes expand like breathing flames
    public float scaleSpeed = 4f;

    [Header("Fiery Glow & Color Flicker")]
    public Color baseColor = new Color(1f, 0.2f, 0.2f, 1f); // Bright fiery red
    public Color peakColor = new Color(1f, 0f, 0f, 1f);   // Deep intense red
    public float colorFlickerSpeed = 10f;

    [Header("Heat Distortion Jitter")]
    public float jitterAmount = 1.5f; // Small pixel heat wave wobble
    public float jitterSpeed = 15f;

    private Vector2 originalAnchoredPos;

    void Awake()
    {
        eyeImage = GetComponent<Image>();
        rectTransform = GetComponent<RectTransform>();

        if (rectTransform != null)
        {
            originalScale = rectTransform.localScale;
            originalAnchoredPos = rectTransform.anchoredPosition;
        }
    }

    void OnEnable()
    {
        if (rectTransform != null)
        {
            rectTransform.localScale = originalScale;
            rectTransform.anchoredPosition = originalAnchoredPos;
        }
    }

    void Update()
    {
        if (rectTransform == null || eyeImage == null) return;

        // 1. Scale pulse (breathing/expanding flame energy)
        float scaleFactor = 1f + Mathf.Sin(Time.time * scaleSpeed) * scalePulseAmount;
        rectTransform.localScale = originalScale * scaleFactor;

        // 2. Color flicker (intensity changes like flickering fire)
        float noise = Mathf.PerlinNoise(Time.time * colorFlickerSpeed, 0f);
        eyeImage.color = Color.Lerp(baseColor, peakColor, noise);

        // 3. Heat wave jitter offset
        float offsetX = (Mathf.PerlinNoise(Time.time * jitterSpeed, 1f) - 0.5f) * 2f * jitterAmount;
        float offsetY = (Mathf.PerlinNoise(1f, Time.time * jitterSpeed) - 0.5f) * 2f * jitterAmount;
        rectTransform.anchoredPosition = originalAnchoredPos + new Vector2(offsetX, offsetY);
    }
}