using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering;
using System.Collections;

public class MenuIntro : MonoBehaviour
{
    [Header("References - Eyes")]
    public Image closedEyes;
    public GameObject openEyes;

    [Header("References - Effects")]
    public Image redVignette;
    public Volume postProcessingVolume;

    [Header("References - UI Buttons")]
    public CanvasGroup[] menuButtons;

    [Header("References - Audio")]
    public AudioSource audioSource;
    public AudioClip shakeRumbleClip; // Plays during closed eyes tremble
    public AudioClip bamImpactClip;   // Plays right when eyes snap open
    public AudioClip buttonFadeClip;  // (Optional) Soft chime/whoosh as buttons appear

    [Header("1. Fade In Closed Eyes")]
    public float initialFadeDuration = 3f;

    [Header("2. Eye Shake Settings")]
    public float rampUpDuration = 2f;
    public float rampDownDuration = 2.5f;
    public float startIntensity = 1f;
    public float peakIntensity = 18f;

    [Header("3. Glow & Blur Effects")]
    public float maxVignetteAlpha = 0.6f;
    public float fxSlowFadeDuration = 3.0f;

    [Header("4. Button Sequential Fade Settings")]
    public float buttonFadeDuration = 0.8f;
    public float delayBetweenButtons = 0.4f;

    private RectTransform openEyesRect;

    void Start()
    {
        if (openEyes != null)
            openEyesRect = openEyes.GetComponent<RectTransform>();

        if (redVignette) SetVignetteAlpha(0);
        if (postProcessingVolume) postProcessingVolume.weight = 0;

        foreach (CanvasGroup btn in menuButtons)
        {
            if (btn != null)
            {
                btn.alpha = 0f;
                btn.interactable = false;
                btn.blocksRaycasts = false;
            }
        }

        StartCoroutine(IntroSequence());
    }

    IEnumerator IntroSequence()
    {
        // 1. Fade in closed eyes
        float elapsedTime = 0f;
        Color eyeColor = closedEyes.color;
        while (elapsedTime < initialFadeDuration)
        {
            elapsedTime += Time.deltaTime;
            eyeColor.a = Mathf.Clamp01(elapsedTime / initialFadeDuration);
            closedEyes.color = eyeColor;
            yield return null;
        }
        eyeColor.a = 1f;
        closedEyes.color = eyeColor;

        // 2. Play Shake Rumble & Ramp-up shake
        if (audioSource && shakeRumbleClip)
        {
            audioSource.PlayOneShot(shakeRumbleClip);
        }

        Vector2 closedOriginalPos = closedEyes.rectTransform.anchoredPosition;
        elapsedTime = 0f;
        while (elapsedTime < rampUpDuration)
        {
            elapsedTime += Time.deltaTime;
            float currentIntensity = Mathf.Lerp(startIntensity, peakIntensity, elapsedTime / rampUpDuration);
            Vector2 randomOffset = Random.insideUnitCircle * currentIntensity;
            closedEyes.rectTransform.anchoredPosition = closedOriginalPos + randomOffset;
            yield return null;
        }
        closedEyes.rectTransform.anchoredPosition = closedOriginalPos;

        // 3. BAM! Play Heavy Impact Audio + Swap eyes + Burst glow/blur
        if (audioSource && bamImpactClip)
        {
            audioSource.PlayOneShot(bamImpactClip);
        }

        closedEyes.gameObject.SetActive(false);
        openEyes.SetActive(true);
        StartCoroutine(BurstEffects());

        // 4. Ramp-down shake
        if (openEyesRect != null)
        {
            Vector2 openOriginalPos = openEyesRect.anchoredPosition;
            elapsedTime = 0f;
            while (elapsedTime < rampDownDuration)
            {
                elapsedTime += Time.deltaTime;
                float currentIntensity = Mathf.Lerp(peakIntensity, 0f, elapsedTime / rampDownDuration);
                Vector2 randomOffset = Random.insideUnitCircle * currentIntensity;
                openEyesRect.anchoredPosition = openOriginalPos + randomOffset;
                yield return null;
            }
            openEyesRect.anchoredPosition = openOriginalPos;
        }

        // 5. Fade vignette & blur away
        StartCoroutine(FadeEffectsAway());

        // 6. Fade in buttons ONE BY ONE
        yield return StartCoroutine(FadeInButtonsSequentially());
    }

    IEnumerator FadeInButtonsSequentially()
    {
        foreach (CanvasGroup btn in menuButtons)
        {
            if (btn == null) continue;

            if (audioSource && buttonFadeClip)
            {
                audioSource.PlayOneShot(buttonFadeClip);
            }

            float elapsedTime = 0f;
            while (elapsedTime < buttonFadeDuration)
            {
                elapsedTime += Time.deltaTime;
                btn.alpha = Mathf.Clamp01(elapsedTime / buttonFadeDuration);
                yield return null;
            }

            btn.alpha = 1f;
            btn.interactable = true;
            btn.blocksRaycasts = true;

            yield return new WaitForSeconds(delayBetweenButtons);
        }
    }

    IEnumerator BurstEffects()
    {
        float duration = 0.1f;
        float elapsedTime = 0f;
        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.Clamp01(elapsedTime / duration);
            if (redVignette) SetVignetteAlpha(Mathf.Lerp(0, maxVignetteAlpha, t));
            if (postProcessingVolume) postProcessingVolume.weight = Mathf.Lerp(0, 1, t);
            yield return null;
        }
        if (redVignette) SetVignetteAlpha(maxVignetteAlpha);
        if (postProcessingVolume) postProcessingVolume.weight = 1;
    }

    IEnumerator FadeEffectsAway()
    {
        float elapsedTime = 0f;
        while (elapsedTime < fxSlowFadeDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = 1.0f - Mathf.Clamp01(elapsedTime / fxSlowFadeDuration);
            if (redVignette) SetVignetteAlpha(Mathf.Lerp(0, maxVignetteAlpha, t));
            if (postProcessingVolume) postProcessingVolume.weight = Mathf.Lerp(0, 1, t);
            yield return null;
        }
        if (redVignette) SetVignetteAlpha(0);
        if (postProcessingVolume) postProcessingVolume.weight = 0;
    }

    void SetVignetteAlpha(float alpha)
    {
        Color vColor = redVignette.color;
        vColor.a = alpha;
        redVignette.color = vColor;
    }
}