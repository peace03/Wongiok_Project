using UnityEngine;
using UnityEngine.EventSystems;

public class PauseSkillHoverPreviewView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private PauseSkillPageView owner;
    private UIPauseSkillInfoData skillData;
    private bool hasData;

    public void Setup(PauseSkillPageView owner, UIPauseSkillInfoData skillData, bool canHover)
    {
        this.owner = owner;
        this.skillData = skillData;
        hasData = canHover && skillData.SkillId >= 0;
    }

    public void Clear()
    {
        if (owner != null)
        {
            owner.HideHoverPreview();
        }

        owner = null;
        skillData = default;
        hasData = false;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!hasData || owner == null)
            return;

        owner.ShowHoverPreview(skillData);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (owner == null)
            return;

        owner.HideHoverPreview();
    }
}
