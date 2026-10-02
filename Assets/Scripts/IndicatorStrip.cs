using UnityEngine;

public class MoveIndicator : MonoBehaviour
{
    [Header("The White Bar Object")]
    public RectTransform whiteBar;

    [Header("Screen Boundaries (Inspector Adjustable)")]
    public float maxDishonorXPosition = -250f;
    public float maxHonorXPosition = 250f;

    [Header("Movement Settings")]
    public float slideSpeed = 8f;

    void Update()
    {
        // Safety check to make sure the manager and bar exist in your scene
        if (alignmanager.Instance == null || whiteBar == null) return;

        // 1. Grab your current live score and limits from the alignmanager
        float score = alignmanager.Instance.currentScore;
        float min = alignmanager.Instance.minScore;
        float max = alignmanager.Instance.maxScore;

        // 2. Turn your current score into a percentage (0% to 100%)
        float scorePercentage = Mathf.InverseLerp(min, max, score);

        // 3. Find the exact target X coordinate based on your Inspector boundaries
        float targetX = Mathf.Lerp(maxDishonorXPosition, maxHonorXPosition, scorePercentage);

        // 4. Slide the white bar smoothly toward that target spot
        Vector2 currentPos = whiteBar.anchoredPosition;
        currentPos.x = Mathf.Lerp(currentPos.x, targetX, Time.deltaTime * slideSpeed);
        whiteBar.anchoredPosition = currentPos;
    }
}