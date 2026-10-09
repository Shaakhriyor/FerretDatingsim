using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using System.Collections;

public class CreditsManager : MonoBehaviour
{
    [Header("UI Panel References")]
    public GameObject creditsPanel;            // Drag CreditsPanel here
    public CanvasGroup mainButtonsCanvasGroup; // Drag MainMenuButtons container
    public ScrollRect creditsScrollRect;       // Drag CreditsScrollView

    void Awake()
    {
        if (creditsPanel != null)
        {
            creditsPanel.SetActive(false);
        }
    }

    public void OpenCredits()
    {
        StartCoroutine(OpenCreditsRoutine());
    }

    private IEnumerator OpenCreditsRoutine()
    {
        if (creditsPanel == null) yield break;

        // 1. Turn on panel
        creditsPanel.SetActive(true);

        // 2. Wait 1 frame so child elements wake up
        yield return null;

        // 3. Play video players
        VideoPlayer[] videoPlayers = creditsPanel.GetComponentsInChildren<VideoPlayer>(true);
        foreach (VideoPlayer vp in videoPlayers)
        {
            if (vp != null && vp.gameObject.activeInHierarchy)
            {
                vp.Play();
            }
        }

        // 4. FORCE Canvas to calculate full height of Content immediately
        Canvas.ForceUpdateCanvases();

        // 5. Reset scroll position to absolute top
        if (creditsScrollRect != null)
        {
            creditsScrollRect.verticalNormalizedPosition = 1f;
        }

        // 6. Lock main menu buttons
        if (mainButtonsCanvasGroup != null)
        {
            mainButtonsCanvasGroup.interactable = false;
            mainButtonsCanvasGroup.blocksRaycasts = false;
        }
    }

    public void CloseCredits()
    {
        if (creditsPanel == null) return;

        VideoPlayer[] videoPlayers = creditsPanel.GetComponentsInChildren<VideoPlayer>(true);
        foreach (VideoPlayer vp in videoPlayers)
        {
            if (vp != null && vp.isPlaying)
            {
                vp.Stop();
            }
        }

        creditsPanel.SetActive(false);

        if (mainButtonsCanvasGroup != null)
        {
            mainButtonsCanvasGroup.interactable = true;
            mainButtonsCanvasGroup.blocksRaycasts = true;
        }
    }
}