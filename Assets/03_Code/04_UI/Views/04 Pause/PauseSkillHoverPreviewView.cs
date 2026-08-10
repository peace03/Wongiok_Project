using UnityEngine;
using UnityEngine.EventSystems;

public class PauseSkillHoverPreviewView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private PauseSkillPageView owner;
    private UIPauseSkillInfoData skillData;
    private bool hasData;

    // 2026.08.10_UI 정리: 전달받은 데이터와 콜백으로 UI 상태 상태를 설정한다.
    public void Setup(PauseSkillPageView owner, UIPauseSkillInfoData skillData, bool canHover)
    {
        this.owner = owner;
        this.skillData = skillData;
        hasData = canHover && skillData.SkillId >= 0;
    }

    // 2026.08.10_UI 정리: UI 상태 상태를 정리한다.
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

    // 2026.08.10_UI 정리: 포인터 상호작용에 맞춰 UI 표시 상태를 갱신한다.
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!hasData || owner == null)
            return;

        owner.ShowHoverPreview(skillData);
    }

    // 2026.08.10_UI 정리: 포인터 상호작용에 맞춰 UI 표시 상태를 갱신한다.
    public void OnPointerExit(PointerEventData eventData)
    {
        if (owner == null)
            return;

        owner.HideHoverPreview();
    }
}
