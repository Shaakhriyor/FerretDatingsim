using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class VNPauseMenu : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private CanvasGroup pauseGroup;
    [SerializeField] private RectTransform menuPanel;
    [SerializeField] private UnityEngine.UI.Button menuButton;
    [SerializeField] private UnityEngine.UI.Button resumeButton;

    [Header("Animation")]
    [Min(0f)][SerializeField] private float transitionDuration = 0.22f;
    [Range(0.8f, 1f)][SerializeField] private float closedScale = 0.96f;

    [Header("Pause")]
    [Tooltip("Pause scene audio while this menu is open.")]
    [SerializeField] private bool pauseAudio = true;

    private bool isOpen;
    private bool pauseApplied;
    private float previousTimeScale;
    private bool previousAudioPause;
    private bool previousDialoguePause;
    private VNDialogueManager pausedDialogue;
    private Vector3 openPanelScale;
    private Coroutine transition;

    public bool IsOpen => isOpen;

    private void Awake()
    {
        if (pauseGroup == null || menuPanel == null ||
            pauseGroup.gameObject == gameObject ||
            transform.IsChildOf(pauseGroup.transform))
        {
            Debug.LogError("Assign PauseOverlay and MenuPanel. Put VNPauseMenu on the menu Canvas, outside PauseOverlay.", this);
            enabled = false;
            return;
        }

        openPanelScale = menuPanel.localScale;
        pauseGroup.alpha = 0f;
        pauseGroup.interactable = false;
        pauseGroup.blocksRaycasts = false;
        menuPanel.localScale = openPanelScale * closedScale;
        pauseGroup.gameObject.SetActive(false);

        if (menuButton != null) menuButton.onClick.AddListener(Open);
        if (resumeButton != null) resumeButton.onClick.AddListener(Close);
    }

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            Toggle();
    }

    public void Toggle()
    {
        if (isOpen) Close();
        else Open();
    }

    public void Open()
    {
        if (!isActiveAndEnabled || isOpen || pauseGroup == null)
            return;

        isOpen = true;
        // Keep the original state during a rapid close/reopen.
        if (!pauseApplied)
        {
            previousTimeScale = Time.timeScale;
            previousAudioPause = AudioListener.pause;
            pausedDialogue = VNDialogueManager.Instance;
            previousDialoguePause = pausedDialogue != null && pausedDialogue.IsPaused;
            pauseApplied = true;
        }

        if (pausedDialogue != null) pausedDialogue.SetPaused(true);
        Time.timeScale = 0f;
        if (pauseAudio) AudioListener.pause = true;

        pauseGroup.gameObject.SetActive(true);
        pauseGroup.blocksRaycasts = true;
        StartTransition(true);
    }

    public void Close()
    {
        if (!isOpen) return;
        isOpen = false;
        StartTransition(false);
    }

    private void StartTransition(bool opening)
    {
        if (transition != null) StopCoroutine(transition);
        pauseGroup.interactable = false;
        transition = StartCoroutine(AnimateMenu(opening));
    }

    private IEnumerator AnimateMenu(bool opening)
    {
        float fromAlpha = pauseGroup.alpha;
        float toAlpha = opening ? 1f : 0f;
        Vector3 fromScale = menuPanel.localScale;
        Vector3 toScale = openPanelScale * (opening ? 1f : closedScale);
        float elapsed = 0f;
        float duration = Mathf.Max(0f, transitionDuration);

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            t = t * t * (3f - 2f * t);
            pauseGroup.alpha = Mathf.Lerp(fromAlpha, toAlpha, t);
            menuPanel.localScale = Vector3.Lerp(fromScale, toScale, t);
            yield return null;
        }

        pauseGroup.alpha = toAlpha;
        menuPanel.localScale = toScale;
        pauseGroup.interactable = opening;

        if (opening)
        {
            if (resumeButton != null && UnityEngine.EventSystems.EventSystem.current != null)
                UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(resumeButton.gameObject);
        }
        else
        {
            pauseGroup.blocksRaycasts = false;
            pauseGroup.gameObject.SetActive(false);
            if (UnityEngine.EventSystems.EventSystem.current != null)
                UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(null);
            ReleasePause();
        }

        transition = null;
    }

    private void ReleasePause()
    {
        if (!pauseApplied) return;
        Time.timeScale = previousTimeScale;
        if (pauseAudio) AudioListener.pause = previousAudioPause;
        if (pausedDialogue != null) pausedDialogue.SetPaused(previousDialoguePause);
        pausedDialogue = null;
        pauseApplied = false;
    }

    private void OnDisable()
    {
        if (transition != null) StopCoroutine(transition);
        transition = null;
        isOpen = false;
        if (pauseGroup != null && pauseGroup.gameObject != gameObject &&
            !transform.IsChildOf(pauseGroup.transform))
        {
            pauseGroup.alpha = 0f;
            pauseGroup.interactable = false;
            pauseGroup.blocksRaycasts = false;
            pauseGroup.gameObject.SetActive(false);
        }
        ReleasePause();
    }

    private void OnDestroy()
    {
        if (menuButton != null) menuButton.onClick.RemoveListener(Open);
        if (resumeButton != null) resumeButton.onClick.RemoveListener(Close);
        ReleasePause();
    }
}
