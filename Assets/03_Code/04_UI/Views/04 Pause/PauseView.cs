using UnityEngine;

public class PauseView : UIViewBase
{
    private enum PauseTabType
    {
        Status,
        Skill,
        Option
    }

    [Header("Tab Buttons")]
    [SerializeField] private CommonButtonView statusTabButton;
    [SerializeField] private CommonButtonView skillTabButton;
    [SerializeField] private CommonButtonView optionTabButton;

    [Header("Page Roots")]
    [SerializeField] private GameObject statusPageRoot;
    [SerializeField] private GameObject skillPageRoot;
    [SerializeField] private GameObject optionPageRoot;

    private PauseTabType currentTab = PauseTabType.Status;

    // 2026.08.10_UI 정리: 컴포넌트 초기화와 이벤트 구독을 준비한다.
    protected override void Awake()
    {
        base.Awake();
        SubscribeEvents();
    }

    // 2026.08.10_UI 정리: 파괴 시 등록한 이벤트와 임시 UI 상태를 정리한다.
    private void OnDestroy()
    {
        UnsubscribeEvents();
    }

    // 2026.08.10_UI 정리: 이벤트 이벤트를 구독한다.
    private void SubscribeEvents()
    {
        EventBus<UIPauseTabMoveRequestedEvent>.action += HandlePauseTabMoveRequested;
    }

    // 2026.08.10_UI 정리: 이벤트 이벤트 구독을 해제한다.
    private void UnsubscribeEvents()
    {
        EventBus<UIPauseTabMoveRequestedEvent>.action -= HandlePauseTabMoveRequested;
    }

    // 2026.08.10_UI 정리: 화면 표시 시 필요한 UI 상태를 초기화한다.
    protected override void OnShow()
    {
        SetupButtons();
        ChangeTab(PauseTabType.Status);
    }

    // 2026.08.10_UI 정리: 화면 숨김 시 임시 UI 상태를 정리한다.
    protected override void OnHide()
    {
        ClearButtons();
    }
    
    // 2026.08.10_UI 정리: 전달받은 데이터와 콜백으로 Buttons 상태를 설정한다.
    private void SetupButtons()
    {
        if (statusTabButton != null)
        {
            statusTabButton.Setup(UITextManager.Get("Pause.StatusTab"), HandleStatusTabClicked);
        }

        if (skillTabButton != null)
        {
            skillTabButton.Setup(UITextManager.Get("Pause.SkillTab"), HandleSkillTabClicked);
        }

        if (optionTabButton != null)
        {
            optionTabButton.Setup(UITextManager.Get("Pause.OptionTab"), HandleOptionTabClicked);
        }

    }

    // 2026.08.10_UI 정리: Buttons 상태를 정리한다.
    private void ClearButtons()
    {
        if (statusTabButton != null)
        {
            statusTabButton.Clear();
        }

        if (skillTabButton != null)
        {
            skillTabButton.Clear();
        }

        if (optionTabButton != null)
        {
            optionTabButton.Clear();
        }

    }

    // 2026.08.10_UI 정리: 일시정지 탭 Move Requested 관련 입력 또는 EventBus 요청을 처리한다.
    private void HandlePauseTabMoveRequested(UIPauseTabMoveRequestedEvent eventData)
    {
        if (!IsVisible)
            return;

        MoveTab(eventData.Direction);
    }

    // 2026.08.10_UI 정리: 탭 상태를 이동한다.
    private void MoveTab(int direction)
    {
        int currentIndex = (int)currentTab;
        int nextIndex = Mathf.Clamp(currentIndex + direction, 0, 2);

        if (nextIndex == currentIndex)
            return;

        ChangeTab((PauseTabType)nextIndex);
    }

    // 2026.08.10_UI 정리: 전체현황 탭 클릭 관련 입력 또는 EventBus 요청을 처리한다.
    private void HandleStatusTabClicked()
    {
        ChangeTab(PauseTabType.Status);
    }

    // 2026.08.10_UI 정리: 스킬 탭 클릭 관련 입력 또는 EventBus 요청을 처리한다.
    private void HandleSkillTabClicked()
    {
        ChangeTab(PauseTabType.Skill);
    }

    // 2026.08.10_UI 정리: 옵션 탭 클릭 관련 입력 또는 EventBus 요청을 처리한다.
    private void HandleOptionTabClicked()
    {
        ChangeTab(PauseTabType.Option);
    }

    // 2026.08.10_UI 정리: 탭 상태로 전환한다.
    private void ChangeTab(PauseTabType nextTab)
    {
        currentTab = nextTab;

        SetPageActive(statusPageRoot, currentTab == PauseTabType.Status);
        SetPageActive(skillPageRoot, currentTab == PauseTabType.Skill);
        SetPageActive(optionPageRoot, currentTab == PauseTabType.Option);
    }

    // 2026.08.10_UI 정리: 페이지 액티브 표시 값을 반영한다.
    private void SetPageActive(GameObject pageRoot, bool isActive)
    {
        if (pageRoot == null)
            return;

        pageRoot.SetActive(isActive);
    }
}
