using UnityEngine;

// 일시정지 메뉴의 스킬 페이지 담당
// 스킬 데이터를 화면에 표시
public class PauseSkillPageView : MonoBehaviour
{
    // 현재 장착 중인 액티브 스킬 슬롯 View 배열
    [Header("Equipped Active Skills")]
    [SerializeField] private PauseSkillInfoView[] equippedActiveSkillViews;

    [Header("Owned Skills")]
    // 보유 중인 교체 가능 스킬 목록 View 배열
    [SerializeField] private PauseSkillInfoView[] ownedSkillViews;
    // 보유 중인 스킬 없을 때 표시할 빈 상태 안내 옵젝
    [SerializeField] private GameObject emptyOwnedSkillObject;

    [Header("Selected Skill Detail")]
    // 스킬 상세 정보 표시
    [SerializeField] private PauseSkillInfoView selectedSkillDetailView;

    // 액티브 스킬 데이터
    private UIPauseSkillInfoData[] currentEquippedActiveSkills;
    // 보유 스킬 데이터
    private UIPauseSkillInfoData[] currentOwnedSkills;

    // 선택된 스킬 정보가 있는지 확인
    private bool hasSelectedSkill;

    // 선택 스킬 상세 데이터
    private UIPauseSkillInfoData currentSelectedSkill;

    private void Awake()
    {
        SubscribeEvents();
    }

    private void OnEnable()
    {
        RefreshAll();
    }

    private void OnDestroy()
    {
        UnsubscribeEvents();
    }

    private void SubscribeEvents()
    {
        EventBus<UISetPauseSkillPageEvent>.action += HandleSetPauseSkillPage;
        EventBus<UIResetEvent>.action += HandleReset;
    }

    private void UnsubscribeEvents()
    {
        EventBus<UISetPauseSkillPageEvent>.action -= HandleSetPauseSkillPage;
        EventBus<UIResetEvent>.action -= HandleReset;
    }

    // 외부 스킬 시스템에서 전달한 스킬 페이지 표시 데이터를 캐싱하고 화면 갱신
    private void HandleSetPauseSkillPage(UISetPauseSkillPageEvent eventData)
    {
        currentEquippedActiveSkills = eventData.EquippedActiveSkills;
        currentOwnedSkills = eventData.OwnedSkills;
        hasSelectedSkill = eventData.HasSelectedSkills;
        currentSelectedSkill = eventData.SelectedSkill;

        RefreshAll();
    }

    private void HandleReset(UIResetEvent eventData)
    {
        ResetSkillPage();
    }

    private void ResetSkillPage()
    {
        currentEquippedActiveSkills = null;
        currentOwnedSkills = null;
        hasSelectedSkill = false;
        currentSelectedSkill = default;

        RefreshAll();
    }

    private void RefreshAll()
    {
        RefreshEquippedActiveSkills();
        RefreshOwnedSkills();
        RefreshSelectedSkillDetail();
    }

    // 장착 중인 액티브 스킬 슬롯 표시 갱신
    private void RefreshEquippedActiveSkills()
    {
        RefreshSkillViewArray(equippedActiveSkillViews, currentEquippedActiveSkills);
    }

    // 보유 중인 교체 가능 스킬 목록 표시 갱신
    private void RefreshOwnedSkills()
    {
        bool hasOwnedSkills = currentOwnedSkills != null && currentOwnedSkills.Length > 0;

        if (emptyOwnedSkillObject != null)
        {
            emptyOwnedSkillObject.SetActive(!hasOwnedSkills);
        }

        RefreshSkillViewArray(ownedSkillViews, currentOwnedSkills);
    }

    // 선택된 스킬 상세 정보 영역 갱신
    private void RefreshSelectedSkillDetail()
    {
        if (selectedSkillDetailView == null)
            return;

        if (hasSelectedSkill)
        {
            selectedSkillDetailView.gameObject.SetActive(true);
            selectedSkillDetailView.Setup(currentSelectedSkill);
            return;
        }

        selectedSkillDetailView.Clear();
        selectedSkillDetailView.gameObject.SetActive(false);
    }

    // 미리 배치된 View 배열에 스킬 데이터를 순서대로 주입
    // 데이터가 없는 남는 View는 Clear 후 비활성 처리
    private void RefreshSkillViewArray(PauseSkillInfoView[] targetViews, UIPauseSkillInfoData[] skillDataArray)
    {
        if (targetViews == null)
            return;

        for (int i = 0; i < targetViews.Length; i++)
        {
            if (targetViews[i] == null)
                continue;

            bool hasData = skillDataArray != null && i < skillDataArray.Length;

            if (hasData)
            {
                targetViews[i].gameObject.SetActive(true);
                targetViews[i].Setup(skillDataArray[i]);
            }
            else
            {
                targetViews[i].Clear();
                targetViews[i].gameObject.SetActive(false);
            }
        }
    }
}
