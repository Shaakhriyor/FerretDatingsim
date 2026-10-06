using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class KarmaManager : MonoBehaviour
{
    public static KarmaManager Instance { get; private set; }

    [Header("Karma")]
    [SerializeField] private int minimumKarma = 0;
    [SerializeField] private int maximumKarma = 100;
    [SerializeField] private int startingKarma = 50;

    [Header("UI")]
    [Tooltip("Assign this if you're using an Image with Image Type = Filled.")]
    [SerializeField] private Image karmaFill;

    [Tooltip("Optional number such as 50.")]
    [SerializeField] private TMP_Text karmaText;

    private int currentKarma;

    public int CurrentKarma => currentKarma;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        currentKarma =
            Mathf.Clamp(
                startingKarma,
                minimumKarma,
                maximumKarma);

        RefreshUI();
    }

    public void AdjustKarma(int amount)
    {
        currentKarma =
            Mathf.Clamp(
                currentKarma + amount,
                minimumKarma,
                maximumKarma);

        RefreshUI();

        Debug.Log(
            $"Karma changed by {amount}. Current karma = {currentKarma}");
    }

    public void SetKarma(int value)
    {
        currentKarma =
            Mathf.Clamp(
                value,
                minimumKarma,
                maximumKarma);

        RefreshUI();
    }

    private void RefreshUI()
    {
        float normalized =
            Mathf.InverseLerp(
                minimumKarma,
                maximumKarma,
                currentKarma);

        if (karmaFill != null)
        {
            karmaFill.fillAmount = normalized;
        }

        if (karmaText != null)
        {
            karmaText.text = currentKarma.ToString();
        }
    }
}