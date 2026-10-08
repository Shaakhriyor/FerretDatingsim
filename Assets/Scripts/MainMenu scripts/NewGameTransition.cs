using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public class NewGameTransition : MonoBehaviour
{
    [Header("Target Scene")]
    public string targetSceneName = "GameScene";

    [Header("Camera & Eye Target References")]
    public Camera mainCamera;
    public Transform targetEyeTransform; // The eye transform/object to zoom into
    public float targetZoomSize = 0.5f;   // How small the camera orthographic size zooms to

    [Header("UI References")]
    public CanvasGroup mainButtonsCanvasGroup;
    public Image blackFadeOverlay;

    [Header("Audio References")]
    public AudioSource audioSource;
    public AudioClip buttonClickClip;
    public AudioClip flyingWhooshClip;

    [Header("Transition Timings")]
    public float buttonFadeDuration = 0.5f;
    public float cameraZoomDuration = 1.8f;
    public float finalBlackFadeDuration = 0.8f;

    private bool isStarting = false;

    public void PlayNewGame()
    {
        if (isStarting) return;
        StartCoroutine(NewGameSequenceRoutine());
    }

    IEnumerator NewGameSequenceRoutine()
    {
        isStarting = true;

        // 1. Lock menu buttons
        if (mainButtonsCanvasGroup != null)
        {
            mainButtonsCanvasGroup.interactable = false;
            mainButtonsCanvasGroup.blocksRaycasts = false;
        }

        // 2. Play button click/fade sound
        if (audioSource && buttonClickClip)
        {
            audioSource.PlayOneShot(buttonClickClip);
        }

        // 3. Fade out menu buttons
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

        // 5. Zoom Camera towards target eye
        if (mainCamera && targetEyeTransform)
        {
            Vector3 startCamPos = mainCamera.transform.position;
            float startOrthoSize = mainCamera.orthographicSize;
            Vector3 targetCamPos = new Vector3(targetEyeTransform.position.x, targetEyeTransform.position.y, startCamPos.z);

            elapsedTime = 0f;
            while (elapsedTime < cameraZoomDuration)
            {
                elapsedTime += Time.deltaTime;
                float t = elapsedTime / cameraZoomDuration;

                // Smooth cinematic acceleration curve
                float smoothT = Mathf.SmoothStep(0f, 1f, t);

                mainCamera.transform.position = Vector3.Lerp(startCamPos, targetCamPos, smoothT);
                mainCamera.orthographicSize = Mathf.Lerp(startOrthoSize, targetZoomSize, smoothT);

                // Fade screen to black as camera approaches the eye pupil
                if (t >= 0.4f && blackFadeOverlay != null)
                {
                    float fadeProgress = (t - 0.4f) / 0.6f;
                    Color overlayColor = blackFadeOverlay.color;
                    overlayColor.a = Mathf.Clamp01(fadeProgress);
                    blackFadeOverlay.color = overlayColor;
                }

                yield return null;
            }
        }

        // Ensure pitch black before scene swap
        if (blackFadeOverlay)
        {
            Color overlayColor = blackFadeOverlay.color;
            overlayColor.a = 1f;
            blackFadeOverlay.color = overlayColor;
        }

        yield return new WaitForSeconds(0.2f);

        // 6. Load Game Scene
        if (!string.IsNullOrEmpty(targetSceneName))
        {
            SceneManager.LoadScene(targetSceneName);
        }
        else
        {
            Debug.LogWarning("Target Scene Name is empty on NewGameTransition!");
        }
    }
}