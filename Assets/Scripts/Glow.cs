using UnityEngine;

public class GlowController : MonoBehaviour
{
    private SpriteRenderer spriteRenderer;
    private Material glowMaterial;

    [Header("Glow Asetukset")]
    [ColorUsage(true, true)] // T�m� mahdollistaa HDR-v�rivalinnan suoraan Inspectorissa
    public Color glowColor = Color.cyan;
    public float pulseSpeed = 2f;
    public float minIntensity = 1f;
    public float maxIntensity = 5f;
    private static readonly int ColorProperty = Shader.PropertyToID("_Color");

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        glowMaterial = spriteRenderer.material;
    }

    void Update()
    {
        float pulse = (Mathf.Sin(Time.time * pulseSpeed) + 1f) / 2f;
        float currentIntensity = Mathf.Lerp(minIntensity, maxIntensity, pulse);
        Color finalColor = glowColor * currentIntensity;
        glowMaterial.SetColor(ColorProperty, finalColor);
    }
}