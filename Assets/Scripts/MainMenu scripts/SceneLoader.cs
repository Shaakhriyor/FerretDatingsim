using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public class SceneLoader : MonoBehaviour
{
    [Header("Target Scene Names")]
    [Tooltip("Exact name of your new game scene")]
    public string newGameSceneName = "GameScene";

    [Tooltip("Exact name of your saved game / level select scene")]
    public string loadGameSceneName = "LoadGameScene";

    [Header("New Game Eye Zoom Transition References")]
    public Camera mainCamera;
    public Transform targetEyeTransform;
    public float targetZoomSize = 0.5f;
    public CanvasGroup mainButtonsCanvasGroup;
    public Image blackFadeOverlay;

    [Header("Audio SFX")]
    public AudioSource audioSource;
    public AudioClip buttonClickClip;
    public AudioClip flyingWhooshClip;

    [Header("Transition Timings")]
    public float buttonFadeDuration = 0.5f;
    public float cameraZoomDuration = 1.8f;

    private bool isTransitioning = false;

    // -------------------------------------------------------------
    // 1. NEW GAME (Triggers cinematic zoom & UI fade)
    // -------------------------------------------------------------
    public void PlayNewGame()
    {
        if (isTransitioning) return;
        StartCoroutine(NewGameTransitionRoutine());
    }

    private IEnumerator NewGameTransitionRoutine()
    {
        isTransitioning = true;

        // Lock menu interaction
        if (mainButtonsCanvasGroup != null)
        {
            mainButtonsCanvasGroup.interactable = false;
            mainButtonsCanvasGroup.blocksRaycasts = false;
        }

        // Button click SFX
        if (audioSource && buttonClickClip)
        {
            audioSource.PlayOneShot(buttonClickClip);
        }

        // Fade buttons out
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

        // Flying whoosh SFX
        if (audioSource && flyingWhooshClip)
        {
            audioSource.PlayOneShot(flyingWhooshClip);
        }

        // Zoom camera to target eye & fade to black
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
                float smoothT = Mathf.SmoothStep(0f, 1f, t);

                mainCamera.transform.position = Vector3.Lerp(startCamPos, targetCamPos, smoothT);
                mainCamera.orthographicSize = Mathf.Lerp(startOrthoSize, targetZoomSize, smoothT);

                if (t >= 0.4f && blackFadeOverlay != null)
                {
                    float fadeProgress = (t - 0.4f) / 0.6f;
                    Color c = blackFadeOverlay.color;
                    c.a = Mathf.Clamp01(fadeProgress);
                    blackFadeOverlay.color = c;
                }

                yield return null;
            }
        }

        if (blackFadeOverlay)
        {
            Color finalColor = blackFadeOverlay.color;
            finalColor.a = 1f;
            blackFadeOverlay.color = finalColor;
        }

        yield return new WaitForSeconds(0.2f);

        // Load New Game Scene
        if (!string.IsNullOrEmpty(newGameSceneName))
        {
            SceneManager.LoadScene(newGameSceneName);
        }
        else
        {
            Debug.LogWarning("New Game Scene Name is empty on SceneLoader!");
        }
    }

    // -------------------------------------------------------------
    // 2. LOAD GAME
    // -------------------------------------------------------------
    public void LoadSavedGame()
    {
        if (!string.IsNullOrEmpty(loadGameSceneName))
        {
            SceneManager.LoadScene(loadGameSceneName);
        }
        else
        {
            Debug.LogWarning("Load Game Scene Name is empty on SceneLoader!");
        }
    }

    // -------------------------------------------------------------
    // 3. GENERIC SCENE LOAD (Pass scene name directly)
    // -------------------------------------------------------------
    public void LoadSceneByName(string sceneName)
    {
        if (!string.IsNullOrEmpty(sceneName))
        {
            SceneManager.LoadScene(sceneName);
        }
    }

    // -------------------------------------------------------------
    // 4. QUIT GAME
    // -------------------------------------------------------------
    public void QuitGame()
    {
        Debug.Log("Quitting game...");
        Application.Quit();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}