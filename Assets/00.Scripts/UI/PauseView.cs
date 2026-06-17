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

    [Header("Command Buttons")]
    [SerializeField] private CommonButtonView continueButton;

    private PauseTabType currentTab = PauseTabType.Status;

    protected override void OnShow()
    {
        SetupButtons();
        ChangeTab(PauseTabType.Status);
    }

    protected override void OnHide()
    {
        ClearButtons();
    }
    
    private void SetupButtons()
    {
        if (statusTabButton != null)
        {
            statusTabButton.Setup("전체 현황", HandleStatusTabClicked);
        }

        if (skillTabButton != null)
        {
            skillTabButton.Setup("스킬", HandleSkillTabClicked);
        }

        if (optionTabButton != null)
        {
            optionTabButton.Setup("옵션", HandleOptionTabClicked);
        }

        if (continueButton != null)
        {
            continueButton.Setup("계속하기", HandleContinueClicked);
        }
    }

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

        if (continueButton != null)
        {
            continueButton.Clear();
        }
    }

    private void HandleStatusTabClicked()
    {
        ChangeTab(PauseTabType.Status);
    }

    private void HandleSkillTabClicked()
    {
        ChangeTab(PauseTabType.Skill);
    }

    private void HandleOptionTabClicked()
    {
        ChangeTab(PauseTabType.Option);
    }

    private void HandleContinueClicked()
    {
        EventBus<UICloseOverlayEvent>.Publish(
            new UICloseOverlayEvent(UIOverlayState.Pause));
    }

    private void ChangeTab(PauseTabType nextTab)
    {
        currentTab = nextTab;

        SetPageActive(statusPageRoot, currentTab == PauseTabType.Status);
        SetPageActive(skillPageRoot, currentTab == PauseTabType.Skill);
        SetPageActive(optionPageRoot, currentTab == PauseTabType.Option);
    }

    private void SetPageActive(GameObject pageRoot, bool isActive)
    {
        if (pageRoot == null)
            return;

        pageRoot.SetActive(isActive);
    }
}