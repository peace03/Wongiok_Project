using UnityEngine;
using UnityEngine.UI;

// 레벨업 스킬 선택 오버레이 전체를 담당하는 View
public class LevelUpView : UIViewBase
{
    [Header("Text")]
    [SerializeField] private Text titleText;
    [SerializeField] private Text guideText;

    [Header("Cards")]
    [SerializeField] private SkillSelectCardView[] optionCards;
    [SerializeField] private GameObject emptyOptionObject;

    [SerializeField] private CommonButtonView emptyOptionButton;

    private UILevelUpSkillOptionData[] currentOptions;
    private bool isSelectionLocked;

    protected override void Awake()
    {
        base.Awake();
        SubscribeEvents();
    }

    private void OnDestroy()
    {
        UnsubscribeEvents();
    }

    protected override void OnShow()
    {
        isSelectionLocked = false;
        RefreshOptions();
    }

    protected override void OnHide()
    {
        ClearCards();
        isSelectionLocked = false;

        if (emptyOptionButton != null)
            emptyOptionButton.Clear();
    }

    private void SubscribeEvents()
    {
        EventBus<UISetLevelUpOptionsEvent>.action += HandleSetLevelUpOptions;
        EventBus<UIResetEvent>.action += HandleReset;
    }

    private void UnsubscribeEvents()
    {
        EventBus<UISetLevelUpOptionsEvent>.action -= HandleSetLevelUpOptions;
        EventBus<UIResetEvent>.action -= HandleReset;
    }

    private void HandleSetLevelUpOptions(UISetLevelUpOptionsEvent eventData)
    {
        currentOptions = eventData.Options;
        isSelectionLocked = false;
        RefreshOptions();
    }

    private void HandleReset(UIResetEvent eventData)
    {
        currentOptions = null;
        isSelectionLocked = false;
        ClearCards();

        if (emptyOptionButton != null)
            emptyOptionButton.Clear();
    }

    private void HandleEmptyOptionClicked()
    {
        EventBus<UICloseOverlayEvent>.Publish(
            new UICloseOverlayEvent(UIOverlayState.LevelUp));
    }

    private void RefreshOptions()
    {
        SetText(titleText, "레벨업");
        SetText(guideText, "강화할 스킬 선택");

        bool hasOptions = currentOptions != null && currentOptions.Length > 0;

        if (emptyOptionObject != null)
        {
            emptyOptionObject.SetActive(!hasOptions);
        }

        if (emptyOptionButton != null)
        {
            if (hasOptions)
            {
                emptyOptionButton.Clear();
            }
            else
            {
                emptyOptionButton.Setup("계속", HandleEmptyOptionClicked);
            }
        }

        if (optionCards == null)
            return;

        for (int i = 0; i < optionCards.Length; i++)
        {
            if (optionCards[i] == null)
                continue;

            bool hasData = hasOptions && i < currentOptions.Length;

            if (hasData)
            {
                optionCards[i].Setup(currentOptions[i], i, HandleSkillSelected);
            }
            else
            {
                optionCards[i].Clear();
            }
        }
    }

    private void ClearCards()
    {
        if (optionCards == null)
            return;

        for (int i = 0; i < optionCards.Length; i++)
        {
            if (optionCards[i] == null)
                continue;

            optionCards[i].Clear();
        }
    }


    private void HandleSkillSelected(int optionIndex, int skillId)
    {
        if (isSelectionLocked)
            return;

        isSelectionLocked = true;
        SetCardsInteractable(false);

        EventBus<UILevelUpSkillSelectedEvent>.Publish(
            new UILevelUpSkillSelectedEvent(optionIndex, skillId));
    }

    private void SetCardsInteractable(bool isInteractable)
    {
        if (optionCards == null)
            return;

        for (int i = 0; i < optionCards.Length; i++)
        {
            if (optionCards[i] == null)
                continue;

            optionCards[i].SetInteractable(isInteractable);
        }
    }

    private void SetText(Text targetText, string value)
    {
        if (targetText == null)
            return;

        targetText.text = value;
    }
}
