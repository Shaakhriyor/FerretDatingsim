using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class KarmaMeter : MonoBehaviour
{
    // Now matches your filename perfectly!
    public static KarmaMeter Instance { get; private set; }

    [Header("Meter Limits")]
    public int minScore = -100;
    public int maxScore = 100;

    [Header("Thresholds")]
    public int goodThreshold = 40;
    public int badThreshold = -40;

    [Header("Current Status")]
    public int currentScore = 0;

    [Header("UI Elements (Optional)")]
    public Slider alignmentSlider;
    public TextMeshProUGUI statusText;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        UpdateUI();
    }

    public void AdjustAlignment(int amount)
    {
        currentScore = Mathf.Clamp(currentScore + amount, minScore, maxScore);
        Debug.Log($"Alignment updated by {amount}. Current Score: {currentScore}");
        UpdateUI();
    }

    private void UpdateUI()
    {
        if (alignmentSlider != null)
        {
            alignmentSlider.minValue = minScore;
            alignmentSlider.maxValue = maxScore;
            alignmentSlider.value = currentScore;
        }

        if (statusText != null)
        {
            if (currentScore >= goodThreshold)
            {
                statusText.text = "Status: Virtuous";
                statusText.color = Color.green;
            }
            else if (currentScore <= badThreshold)
            {
                statusText.text = "Status: Malevolent";
                statusText.color = Color.red;
            }
            else
            {
                statusText.text = "Status: Neutral";
                statusText.color = Color.white;
            }
        }
    }
}