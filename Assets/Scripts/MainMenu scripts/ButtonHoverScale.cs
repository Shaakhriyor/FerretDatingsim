using UnityEngine;
using UnityEngine.EventSystems;

public class ButtonHoverScale : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Hover Settings")]
    public float hoverScaleMultiplier = 1.15f; // Scale multiplier on hover (1.15 = 15% bigger)
    public float transitionSpeed = 12f;       // How fast it scales up/down

    private Vector3 originalScale;
    private Vector3 targetScale;

    void Awake()
    {
        originalScale = transform.localScale;
        targetScale = originalScale;
    }

    void OnEnable()
    {
        // Reset scale if button is re-enabled
        if (originalScale != Vector3.zero)
        {
            transform.localScale = originalScale;
            targetScale = originalScale;
        }
    }

    void Update()
    {
        // Smoothly interpolate scale toward target scale
        transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.deltaTime * transitionSpeed);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        targetScale = originalScale * hoverScaleMultiplier;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        targetScale = originalScale;
    }
}