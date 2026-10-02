using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic; // Required to use Dictionaries for tracking characters

public class AlignmentManager : MonoBehaviour
{
    // This allows ChoiceController to see this script anywhere in your game
    public static AlignmentManager Instance { get; private set; }

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

    // --- NEW: Dating Sim Character Affection Storage ---
    private Dictionary<string, int> affectionScores = new Dictionary<string, int>();

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
        UpdateUI();
    }

    // This is the function ChoiceController calls to change karma/morality points
    public void AdjustAlignment(int amount)
    {
        currentScore = Mathf.Clamp(currentScore + amount, minScore, maxScore);
        Debug.Log($"Alignment updated by {amount}. Current Score: {currentScore}");
        UpdateUI();
    }

    // --- NEW: This is the function ChoiceController calls to change relationship scores ---
    public void AdjustAffection(string characterName, int amount)
    {
        if (string.IsNullOrEmpty(characterName)) return;

        // If we haven't met this character yet, initialize their score at 0
        if (!affectionScores.ContainsKey(characterName))
        {
            affectionScores[characterName] = 0;
        }

        affectionScores[characterName] += amount;
        Debug.Log($"[Dating Sim] {characterName}'s Affection changed by {amount}. Current: {affectionScores[characterName]}");
    }

    // --- NEW: Helper function in case other scripts need to check an affection value ---
    public int GetAffectionScore(string characterName)
    {
        if (affectionScores.ContainsKey(characterName))
        {
            return affectionScores[characterName];
        }
        return 0; // Return 0 if the character hasn't been met or registered yet
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