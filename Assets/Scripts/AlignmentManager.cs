using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class alignmanager : MonoBehaviour
{
    // This allows other scripts to easily find the score
    public static alignmanager Instance { get; private set; }

    [Header("Meter Limits")]
    public int minScore = -100;
    public int maxScore = 100;

    [Header("Thresholds")]
    public int goodThreshold = 40;
    public int badThreshold = -40;

    [Header("Current Status")]
    public int currentScore = 0;

    [Header("Optional UI Elements")]
    public Slider alignmentSlider;
    public TextMeshProUGUI statusText;

    private void Awake()
    {
        // Set up the Singleton pattern safely
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
        // Force your starting score to absolute neutral (0)
        currentScore = 0;

        // --- THE CRITICAL FIX: This forces the visual slider bar to accept negative numbers ---
        if (alignmentSlider != null)
        {
            alignmentSlider.minValue = minScore;   // Sets it to -100 instead of 0
            alignmentSlider.maxValue = maxScore;   // Sets it to 100
            alignmentSlider.value = currentScore;  // Centers the handle at 0
        }

        UpdateUI();
    }

    // This is the function your buttons call to change points
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