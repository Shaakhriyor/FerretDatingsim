using TMPro;
using UnityEngine;

public class KarmaManager : MonoBehaviour
{
    public static KarmaManager Instance { get; private set; }

    [Header("Karma")]
    [SerializeField] private int minimumKarma = 0;
    [SerializeField] private int maximumKarma = 100;
    [SerializeField] private int startingKarma = 50;

    [Header("UI")]
    [Tooltip("The existing Image with Image Type = Filled.")]
    [SerializeField] private UnityEngine.UI.Image karmaFill;
    [Tooltip("Optional number such as 50. Keep your existing reference.")]
    [SerializeField] private TMP_Text karmaText;

    [Header("Change Notification")]
    [Tooltip("Canvas Group on KarmaBar, containing the whole meter and change text.")]
    [SerializeField] private CanvasGroup karmaGroup;
    [Tooltip("A separate TMP text under KarmaBar for +Karma or -Karma.")]
    [SerializeField] private TMP_Text karmaChangeText;
    [SerializeField] private Color positiveColor = new Color(0.2f, 1f, 0.35f, 1f);
    [SerializeField] private Color negativeColor = new Color(1f, 0.25f, 0.25f, 1f);

    [Header("Animation Timing")]
    [Min(0f)][SerializeField] private float fadeInDuration = 0.25f;
    [Min(0f)][SerializeField] private float fillAnimationDuration = 0.6f;
    [Min(0f)][SerializeField] private float holdDuration = 1.2f;
    [Min(0f)][SerializeField] private float fadeOutDuration = 0.4f;

    private enum NotificationPhase { Hidden, FadeIn, AnimateFill, Hold, FadeOut }

    private int currentKarma;
    private float displayedKarma;
    private float fillStartValue;
    private float fadeStartAlpha;
    private float phaseElapsed;
    private NotificationPhase phase;

    public int CurrentKarma => currentKarma;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        maximumKarma = Mathf.Max(minimumKarma, maximumKarma);
        currentKarma = Mathf.Clamp(startingKarma, minimumKarma, maximumKarma);

        if (karmaGroup != null)
        {
            karmaGroup.interactable = false;
            karmaGroup.blocksRaycasts = false;
        }
        else
        {
            Debug.LogWarning("Assign KarmaBar's Canvas Group to Karma Group to enable fading.", this);
        }

        if (karmaChangeText != null)
            karmaChangeText.raycastTarget = false;

        HideAndSync();
    }

    public void AdjustKarma(int amount)
    {
        int previousKarma = currentKarma;
        // Use a wider intermediate so a large adjustment cannot overflow an int.
        long requested = (long)currentKarma + amount;
        currentKarma = (int)System.Math.Max(minimumKarma,
            System.Math.Min(maximumKarma, requested));

        if (currentKarma == previousKarma)
            return;

        Debug.Log($"Karma changed by {(long)currentKarma - previousKarma}. Current karma = {currentKarma}");

        if (!isActiveAndEnabled || karmaGroup == null ||
            !karmaGroup.gameObject.activeInHierarchy)
        {
            HideAndSync();
            return;
        }

        if (karmaChangeText != null)
        {
            bool positive = currentKarma > previousKarma;
            karmaChangeText.text = positive ? "+Karma" : "-Karma";
            karmaChangeText.color = positive ? positiveColor : negativeColor;
        }

        // If another change arrives, continue from the current visual values.
        // The actual karma value is already updated for gameplay and saving.
        fillStartValue = displayedKarma;
        fadeStartAlpha = karmaGroup.alpha;
        BeginPhase(NotificationPhase.FadeIn);
    }

    // Restore a save or reset karma quietly, without showing a reward/loss popup.
    public void SetKarma(int value)
    {
        currentKarma = Mathf.Clamp(value, minimumKarma, maximumKarma);
        HideAndSync();
    }

    private void Update()
    {
        if (phase == NotificationPhase.Hidden)
            return;

        if (karmaGroup == null || !karmaGroup.gameObject.activeInHierarchy)
        {
            HideAndSync();
            return;
        }

        phaseElapsed += Time.unscaledDeltaTime;

        switch (phase)
        {
            case NotificationPhase.FadeIn:
                float fadeIn = Progress(fadeInDuration);
                karmaGroup.alpha = Mathf.Lerp(fadeStartAlpha, 1f, Smooth(fadeIn));
                if (fadeIn >= 1f)
                    BeginPhase(NotificationPhase.AnimateFill);
                break;

            case NotificationPhase.AnimateFill:
                float fill = Progress(fillAnimationDuration);
                RefreshUI(Mathf.Lerp(fillStartValue, currentKarma, Smooth(fill)));
                if (fill >= 1f)
                    BeginPhase(NotificationPhase.Hold);
                break;

            case NotificationPhase.Hold:
                if (phaseElapsed >= Mathf.Max(0f, holdDuration))
                    BeginPhase(NotificationPhase.FadeOut);
                break;

            case NotificationPhase.FadeOut:
                float fadeOut = Progress(fadeOutDuration);
                karmaGroup.alpha = 1f - Smooth(fadeOut);
                if (fadeOut >= 1f)
                    HideAndSync();
                break;
        }
    }

    private void BeginPhase(NotificationPhase next)
    {
        phase = next;
        phaseElapsed = 0f;
    }

    private float Progress(float duration)
    {
        return duration <= 0f ? 1f : Mathf.Clamp01(phaseElapsed / duration);
    }

    private static float Smooth(float value)
    {
        return value * value * (3f - 2f * value);
    }

    private void RefreshUI(float value)
    {
        displayedKarma = value;
        if (karmaFill != null)
            karmaFill.fillAmount = Mathf.InverseLerp(minimumKarma, maximumKarma, value);
        if (karmaText != null)
            karmaText.text = Mathf.RoundToInt(value).ToString();
    }

    private void HideAndSync()
    {
        BeginPhase(NotificationPhase.Hidden);
        RefreshUI(currentKarma);
        if (karmaGroup != null)
            karmaGroup.alpha = 0f;
        if (karmaChangeText != null)
            karmaChangeText.text = "";
    }

    private void OnDisable()
    {
        if (Instance == this)
            HideAndSync();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    [ContextMenu("TEST - Gain 10 Karma (Play Mode)")]
    private void TestGainKarma()
    {
        if (Application.isPlaying)
            AdjustKarma(10);
    }

    [ContextMenu("TEST - Lose 10 Karma (Play Mode)")]
    private void TestLoseKarma()
    {
        if (Application.isPlaying)
            AdjustKarma(-10);
    }
}
