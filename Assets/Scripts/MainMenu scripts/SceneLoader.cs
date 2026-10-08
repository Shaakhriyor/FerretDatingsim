using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public class SceneLoader : MonoBehaviour
{
    [Header("Target Scene Names")]
    public string newGameSceneName = "GameScene";
    public string loadGameSceneName = "LoadGameScene";

    [Header("New Game Eye Rush References")]
    public RectTransform openEyesRect;        // Drag OpenEyes UI object here
    public CanvasGroup mainButtonsCanvasGroup; // Drag MainMenuButtons container
    public Image blackFadeOverlay;             // Drag BlackFadeOverlay image

    [Header("Audio SFX")]
    public AudioSource audioSource;
    public AudioClip buttonClickClip;
    public AudioClip flyingWhooshClip;

    [Header("Rush Settings")]
    public float buttonFadeDuration = 0.4f;
    public float eyeRushDuration = 1.5f;
    public Vector3 giantTargetScale = new Vector3(12f, 12f, 1f); // How giant eyes grow

    private bool isTransitioning = false;

    public void PlayNewGame()
    {
        if (isTransitioning) return;
        StartCoroutine(NewGameTransitionRoutine());
    }

    private IEnumerator NewGameTransitionRoutine()
    {
        isTransitioning = true;

        // 1. Lock menu interaction
        if (mainButtonsCanvasGroup != null)
        {
            mainButtonsCanvasGroup.interactable = false;
            mainButtonsCanvasGroup.blocksRaycasts = false;
        }

        // 2. Play button click sound
        if (audioSource && buttonClickClip)
        {
            audioSource.PlayOneShot(buttonClickClip);
        }

        // 3. Fade buttons out
        float elapsedTime = 0f;
        while (elapsedTime < buttonFadeDuration)
        {
            elapsedTime += Time.deltaTime;
            if (mainButtonsCanvasGroup)
            {
                mainButtonsCanvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsedTime / buttonFadeDuration);
            }
            yield return null;
        }
        if (mainButtonsCanvasGroup) mainButtonsCanvasGroup.alpha = 0f;

        // 4. Play flying whoosh sound
        if (audioSource && flyingWhooshClip)
        {
            audioSource.PlayOneShot(flyingWhooshClip);
        }

        // 5. Scale OpenEyes up giant and center them on screen
        if (openEyesRect != null)
        {
            Vector3 startScale = openEyesRect.localScale;
            Vector2 startPos = openEyesRect.anchoredPosition;
            Vector2 targetPos = Vector2.zero; // Centers eyes on screen

            elapsedTime = 0f;
            while (elapsedTime < eyeRushDuration)
            {
                elapsedTime += Time.deltaTime;
                float t = elapsedTime / eyeRushDuration;

                // Exponential curve so eyes accelerate as they rush into screen
                float smoothT = t * t * t;

                openEyesRect.localScale = Vector3.Lerp(startScale, giantTargetScale, smoothT);
                openEyesRect.anchoredPosition = Vector2.Lerp(startPos, targetPos, smoothT);

                // Fade screen to black as eyes engulf the camera
                if (t >= 0.3f && blackFadeOverlay != null)
                {
                    float fadeProgress = (t - 0.3f) / 0.7f;
                    Color c = blackFadeOverlay.color;
                    c.a = Mathf.Clamp01(fadeProgress);
                    blackFadeOverlay.color = c;
                }

                yield return null;
            }
        }

        // Ensure pitch black before loading
        if (blackFadeOverlay)
        {
            Color finalColor = blackFadeOverlay.color;
            finalColor.a = 1f;
            blackFadeOverlay.color = finalColor;
        }

        yield return new WaitForSeconds(0.2f);

        // 6. Load scene
        if (!string.IsNullOrEmpty(newGameSceneName))
        {
            SceneManager.LoadScene(newGameSceneName);
        }
        else
        {
            Debug.LogWarning("New Game Scene Name is empty on SceneLoader!");
        }
    }

    public void LoadSavedGame()
    {
        if (!string.IsNullOrEmpty(loadGameSceneName))
        {
            SceneManager.LoadScene(loadGameSceneName);
        }
    }

    public void QuitGame()
    {
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}