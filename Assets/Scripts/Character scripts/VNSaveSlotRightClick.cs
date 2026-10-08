using UnityEngine;
using UnityEngine.EventSystems;

[DisallowMultipleComponent]
public sealed class VNSaveSlotRightClick : MonoBehaviour, IPointerClickHandler
{
    private VNHeartMenu menu;
    private int visibleIndex;

    public void Configure(VNHeartMenu owner, int index)
    {
        menu = owner;
        visibleIndex = index;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (isActiveAndEnabled && menu != null &&
            eventData.button == PointerEventData.InputButton.Right)
            menu.RequestDeleteSlot(visibleIndex);
    }
}
