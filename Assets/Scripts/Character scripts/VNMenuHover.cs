using UnityEngine;
using UnityEngine.EventSystems;

public class VNMenuHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
{
    [SerializeField] private float scale = 1.035f;
    private Vector3 rest;
    private bool hovered;
    private bool selected;
    private UnityEngine.UI.Selectable control;
    private void Awake() { rest = transform.localScale; control = GetComponent<UnityEngine.UI.Selectable>(); }
    private void Update()
    {
        bool active = (hovered || selected) && control != null && control.IsInteractable();
        transform.localScale = Vector3.Lerp(transform.localScale, rest * (active ? scale : 1f),
            1f - Mathf.Exp(-18f * Time.unscaledDeltaTime));
    }
    public void OnPointerEnter(PointerEventData e) { hovered = true; }
    public void OnPointerExit(PointerEventData e) { hovered = false; }
    public void OnSelect(BaseEventData e) { selected = true; }
    public void OnDeselect(BaseEventData e) { selected = false; }
    private void OnDisable() { hovered = false; selected = false; if (rest != Vector3.zero) transform.localScale = rest; }
}
