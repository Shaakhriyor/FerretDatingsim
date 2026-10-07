using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class RedFlameAura : MonoBehaviour
{
    private Volume volume;
    private Vignette vignette;

    [Header("Flame Pulse Settings")]
    public float baseIntensity = 0.5f;
    public float flickerAmount = 0.15f;
    public float flickerSpeed = 8f;

    void Awake()
    {
        volume = GetComponent<Volume>();
        if (volume != null && volume.profile != null)
        {
            volume.profile.TryGet(out vignette);
        }
    }

    void Update()
    {
        if (vignette == null) return;

        // Uses noise to create a burning flame flicker on the red screen edges
        float noise = Mathf.PerlinNoise(Time.time * flickerSpeed, 0f);
        vignette.intensity.value = baseIntensity + (noise * flickerAmount);
    }
}