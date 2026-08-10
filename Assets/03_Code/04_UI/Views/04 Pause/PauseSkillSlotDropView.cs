using UnityEngine;
using UnityEngine.EventSystems;

public class PauseSkillSlotDropView : MonoBehaviour, IDropHandler, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private int slotIndex;
    [SerializeField] private GameObject highlightObject;

    // 2026.08.10_UI 정리: 전달받은 데이터와 콜백으로 UI 상태 상태를 설정한다.
    public void Setup(int slotIndex)
    {
        this.slotIndex = slotIndex;
        SetHighlight(false);
    }

    // 2026.08.10_UI 정리: 드롭된 UI 항목의 교체 요청을 처리한다.
    public void OnDrop(PointerEventData eventData)
    {
        PauseSkillDragView dragItem = GetDragItem(eventData);

        if (dragItem == null)
            return;

        if (dragItem.SkillId < 0)
            return;

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

    // 2026.08.10_UI 정리: 포인터 상호작용에 맞춰 UI 표시 상태를 갱신한다.
    public void OnPointerEnter(PointerEventData eventData)
    {
        PauseSkillDragView dragItem = GetDragItem(eventData);

        if (dragItem == null || dragItem.SkillId < 0)
            return;

        SetHighlight(true);
    }

    // 2026.08.10_UI 정리: 포인터 상호작용에 맞춰 UI 표시 상태를 갱신한다.
    public void OnPointerExit(PointerEventData eventData)
    {
        SetHighlight(false);
    }

    // 2026.08.10_UI 정리: 현재 드래그 아이템 값을 반환한다.
    private PauseSkillDragView GetDragItem(PointerEventData eventData)
    {
        if (eventData.pointerDrag == null)
            return null;

        return eventData.pointerDrag.GetComponentInParent<PauseSkillDragView>();
    }

    // 2026.08.10_UI 정리: Highlight 표시 값을 반영한다.
    private void SetHighlight(bool isActive)
    {
        if (highlightObject != null)
        {
            highlightObject.SetActive(isActive);
        }
    }
}
