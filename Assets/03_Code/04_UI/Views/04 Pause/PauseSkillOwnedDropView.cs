using UnityEngine;
using UnityEngine.EventSystems;

public class PauseSkillOwnedDropView : MonoBehaviour, IDropHandler, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private GameObject highlightObject;

    public void OnDrop(PointerEventData eventData)
    {
        PauseSkillDragView dragItem = GetDragItem(eventData);

        if (dragItem == null)
            return;

        if (dragItem.SkillId < 0)
            return;

        if (dragItem.SourceType != PauseSkillDragSourceType.EquippedSlot)
            return;

        SetHighlight(false);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        PauseSkillDragView dragItem = GetDragItem(eventData);

        if (dragItem == null)
            return;

        if (dragItem.SourceType != PauseSkillDragSourceType.EquippedSlot)
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
