using UnityEngine;
using System.Collections;

public class SceneLoader : MonoBehaviour
{
    [Header("Connect our name and load menu")]
    public VNNewGameMenu newGameMenu;

    // Retained for compatibility with existing scene data. Destinations are now
    // configured on VNNewGameMenu; Load opens slots instead of loading a UI scene.
    [HideInInspector] public string newGameSceneName = "GameScene";
    [HideInInspector] public string loadGameSceneName = "LoadGameScene";

    [Header("New Game Eye Rush References")]
    public RectTransform openEyesRect;
    public CanvasGroup mainButtonsCanvasGroup;
    public UnityEngine.UI.Image blackFadeOverlay;

    [Header("Audio SFX")]
    public AudioSource audioSource;
    public AudioClip buttonClickClip;
    public AudioClip flyingWhooshClip;

    [Header("Rush Settings")]
    [Min(0f)] public float buttonFadeDuration = 0.4f;
    [Min(0f)] public float eyeRushDuration = 1.5f;
    public Vector3 giantTargetScale = new Vector3(12f, 12f, 1f);

    private bool isTransitioning;
    private bool snapshotCaptured;
    private Vector3 originalEyeScale;
    private Vector2 originalEyePosition;
    private float originalButtonsAlpha;
    private bool originalButtonsInteractable, originalButtonsBlockRaycasts;
    private Color originalOverlayColor;
    private bool originalOverlayRaycast, originalOverlayEnabled, originalOverlayActive;

    public void PlayNewGame()
    {
        if (isTransitioning || VNSceneTransition.IsBusy) return;
        if (newGameMenu == null)
        {
            Debug.LogError("Assign MainMenuConnections (VNNewGameMenu) to SceneLoader's New Game Menu field.", this);
            return;
        }

        // Capture before VNNewGameMenu locks the same button group.
        CaptureMenuState();
        if (!newGameMenu.TryBeginAnimatedIntro(RestoreMenu))
        {
            snapshotCaptured = false;
            return;
        }
        isTransitioning = true;
        StartCoroutine(NewGameTransitionRoutine());
    }

    private IEnumerator NewGameTransitionRoutine()
    {
        if (mainButtonsCanvasGroup != null)
        {
            mainButtonsCanvasGroup.interactable = false;
            mainButtonsCanvasGroup.blocksRaycasts = false;
        }
        if (blackFadeOverlay != null)
        {
            blackFadeOverlay.gameObject.SetActive(true);
            blackFadeOverlay.enabled = true;
            blackFadeOverlay.raycastTarget = true;
        }
        if (audioSource != null && buttonClickClip != null)
            audioSource.PlayOneShot(buttonClickClip);

        float duration = Mathf.Max(0f, buttonFadeDuration);
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed = Mathf.Min(duration, elapsed + Time.unscaledDeltaTime);
            if (mainButtonsCanvasGroup != null)
                mainButtonsCanvasGroup.alpha = Mathf.Lerp(originalButtonsAlpha, 0f, elapsed / duration);
            yield return null;
        }
        if (mainButtonsCanvasGroup != null) mainButtonsCanvasGroup.alpha = 0f;

        if (audioSource != null && flyingWhooshClip != null)
            audioSource.PlayOneShot(flyingWhooshClip);

        duration = Mathf.Max(0f, eyeRushDuration);
        elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed = Mathf.Min(duration, elapsed + Time.unscaledDeltaTime);
            float t = elapsed / duration;
            float smoothT = t * t * t;
            if (openEyesRect != null)
            {
                openEyesRect.localScale = Vector3.Lerp(originalEyeScale, giantTargetScale, smoothT);
                openEyesRect.anchoredPosition = Vector2.Lerp(originalEyePosition, Vector2.zero, smoothT);
            }
            if (blackFadeOverlay != null && t >= 0.3f)
                SetBlackAlpha(Mathf.Lerp(originalOverlayColor.a, 1f, (t - 0.3f) / 0.7f));
            yield return null;
        }
        if (openEyesRect != null)
        {
            openEyesRect.localScale = giantTargetScale;
            openEyesRect.anchoredPosition = Vector2.zero;
        }
        SetBlackAlpha(1f);
        yield return new WaitForSecondsRealtime(0.2f);

        // Stay in MAINMENU. The name panel's Start button owns the new-game load.
        if (newGameMenu == null || !newGameMenu.CompleteAnimatedIntro())
        {
            Debug.LogError("The name panel could not be opened. Check VNNewGameMenu is enabled and configured.", this);
            if (newGameMenu != null) newGameMenu.Cancel();
            RestoreMenu();
            yield break;
        }
        if (blackFadeOverlay != null) blackFadeOverlay.raycastTarget = false;
        // Keep the black background until Start or Cancel. Cancel invokes RestoreMenu.
    }

    public void LoadSavedGame()
    {
        if (isTransitioning || VNSceneTransition.IsBusy) return;
        if (newGameMenu != null) newGameMenu.OpenLoadGame();
        else Debug.LogError("Assign VNNewGameMenu to SceneLoader's New Game Menu field.", this);
    }

    public void QuitGame()
    {
        if (isTransitioning || VNSceneTransition.IsBusy) return;
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    private void CaptureMenuState()
    {
        if (openEyesRect != null)
        {
            originalEyeScale = openEyesRect.localScale;
            originalEyePosition = openEyesRect.anchoredPosition;
        }
        if (mainButtonsCanvasGroup != null)
        {
            originalButtonsAlpha = mainButtonsCanvasGroup.alpha;
            originalButtonsInteractable = mainButtonsCanvasGroup.interactable;
            originalButtonsBlockRaycasts = mainButtonsCanvasGroup.blocksRaycasts;
        }
        if (blackFadeOverlay != null)
        {
            originalOverlayColor = blackFadeOverlay.color;
            originalOverlayRaycast = blackFadeOverlay.raycastTarget;
            originalOverlayEnabled = blackFadeOverlay.enabled;
            originalOverlayActive = blackFadeOverlay.gameObject.activeSelf;
        }
        snapshotCaptured = true;
    }

    private void SetBlackAlpha(float alpha)
    {
        if (blackFadeOverlay == null) return;
        Color color = blackFadeOverlay.color;
        color.a = Mathf.Clamp01(alpha);
        blackFadeOverlay.color = color;
    }

    private void RestoreMenu()
    {
        if (this == null) return;
        StopAllCoroutines();
        isTransitioning = false;
        if (!snapshotCaptured) return;
        snapshotCaptured = false;
        if (openEyesRect != null)
        {
            openEyesRect.localScale = originalEyeScale;
            openEyesRect.anchoredPosition = originalEyePosition;
        }
        if (mainButtonsCanvasGroup != null)
        {
            mainButtonsCanvasGroup.alpha = originalButtonsAlpha;
            mainButtonsCanvasGroup.interactable = originalButtonsInteractable;
            mainButtonsCanvasGroup.blocksRaycasts = originalButtonsBlockRaycasts;
        }
        if (blackFadeOverlay != null)
        {
            blackFadeOverlay.color = originalOverlayColor;
            blackFadeOverlay.raycastTarget = originalOverlayRaycast;
            blackFadeOverlay.enabled = originalOverlayEnabled;
            blackFadeOverlay.gameObject.SetActive(originalOverlayActive);
        }
    }

    private void OnDisable()
    {
        if (!snapshotCaptured) return;
        if (newGameMenu != null && newGameMenu.isActiveAndEnabled && !VNSceneTransition.IsBusy)
            newGameMenu.Cancel();
        RestoreMenu();
    }
}
