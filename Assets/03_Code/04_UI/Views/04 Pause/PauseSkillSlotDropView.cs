using UnityEngine;
using UnityEngine.EventSystems;

public class PauseSkillSlotDropView : MonoBehaviour, IDropHandler, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private int slotIndex;
    [SerializeField] private GameObject highlightObject;

    public void Setup(int slotIndex)
    {
        this.slotIndex = slotIndex;
        SetHighlight(false);
    }

    public void OnDrop(PointerEventData eventData)
    {
        PauseSkillDragView dragItem = GetDragItem(eventData);

        if (dragItem == null)
            return;

        if (dragItem.SkillId < 0)
            return;

        dragItem.MarkDroppedOnSlot();
        SetHighlight(false);

        if (dragItem.SourceType == PauseSkillDragSourceType.OwnedSkill)
        {
            EventBus<UIPauseSkillEquipRequestedEvent>.Publish(
                new UIPauseSkillEquipRequestedEvent(
                    dragItem.SkillId,
                    slotIndex,
                    dragItem.SourceOwnedSlotIndex));

            return;
        }

        if (dragItem.SourceType == PauseSkillDragSourceType.EquippedSlot)
        {
            if (dragItem.SourceSlotIndex == slotIndex)
                return;

            EventBus<UIPauseSkillSwapRequestedEvent>.Publish(
                new UIPauseSkillSwapRequestedEvent(
                    dragItem.SourceSlotIndex,
                    slotIndex));
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        PauseSkillDragView dragItem = GetDragItem(eventData);

        if (dragItem == null || dragItem.SkillId < 0)
            return;

        SetHighlight(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        SetHighlight(false);
    }

    private PauseSkillDragView GetDragItem(PointerEventData eventData)
    {
        if (eventData.pointerDrag == null)
            return null;

        return eventData.pointerDrag.GetComponentInParent<PauseSkillDragView>();
    }

    private void SetHighlight(bool isActive)
    {
        if (highlightObject != null)
        {
            highlightObject.SetActive(isActive);
        }
    }
}
