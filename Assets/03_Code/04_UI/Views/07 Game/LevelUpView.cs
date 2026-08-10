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

    // 2026.08.10_UI 정리: 화면 표시 시 필요한 UI 상태를 초기화한다.
    protected override void OnShow()
    {
        isSelectionLocked = false;
        RefreshOptions();
    }

    // 2026.08.10_UI 정리: 화면 숨김 시 임시 UI 상태를 정리한다.
    protected override void OnHide()
    {
        ClearCards();
        isSelectionLocked = false;

        if (emptyOptionButton != null)
            emptyOptionButton.Clear();
    }

    // 2026.08.10_UI 정리: 이벤트 이벤트를 구독한다.
    private void SubscribeEvents()
    {
        EventBus<UISetLevelUpOptionsEvent>.action += HandleSetLevelUpOptions;
        EventBus<UIResetEvent>.action += HandleReset;
    }

    // 2026.08.10_UI 정리: 이벤트 이벤트 구독을 해제한다.
    private void UnsubscribeEvents()
    {
        EventBus<UISetLevelUpOptionsEvent>.action -= HandleSetLevelUpOptions;
        EventBus<UIResetEvent>.action -= HandleReset;
    }

    // 2026.08.10_UI 정리: Set 레벨 Up 옵션 관련 입력 또는 EventBus 요청을 처리한다.
    private void HandleSetLevelUpOptions(UISetLevelUpOptionsEvent eventData)
    {
        currentOptions = eventData.Options;
        isSelectionLocked = false;
        RefreshOptions();
    }

    // 2026.08.10_UI 정리: 초기화 관련 입력 또는 EventBus 요청을 처리한다.
    private void HandleReset(UIResetEvent eventData)
    {
        currentOptions = null;
        isSelectionLocked = false;
        ClearCards();

        if (emptyOptionButton != null)
            emptyOptionButton.Clear();
    }

    // 2026.08.10_UI 정리: Empty 옵션 클릭 관련 입력 또는 EventBus 요청을 처리한다.
    private void HandleEmptyOptionClicked()
    {
        EventBus<UICloseOverlayEvent>.Publish(
            new UICloseOverlayEvent(UIOverlayState.LevelUp));
    }

    // 2026.08.10_UI 정리: 현재 데이터로 옵션 표시를 갱신한다.
    private void RefreshOptions()
    {
        SetText(titleText, UITextManager.Get("LevelUp.Title"));
        SetText(guideText, UITextManager.Get("LevelUp.Guide"));

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
                emptyOptionButton.Setup(UITextManager.Get("LevelUp.Continue"), HandleEmptyOptionClicked);
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

    // 2026.08.10_UI 정리: Cards 상태를 정리한다.
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


    // 2026.08.10_UI 정리: 스킬 선택 관련 입력 또는 EventBus 요청을 처리한다.
    private void HandleSkillSelected(int optionIndex, int skillId)
    {
        if (isSelectionLocked)
            return;

        isSelectionLocked = true;
        SetCardsInteractable(false);

        EventBus<UILevelUpSkillSelectedEvent>.Publish(
            new UILevelUpSkillSelectedEvent(optionIndex, skillId));
    }

    // 2026.08.10_UI 정리: Cards Interactable 표시 값을 반영한다.
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

    // 2026.08.10_UI 정리: 텍스트 표시 값을 반영한다.
    private void SetText(Text targetText, string value)
    {
        if (targetText == null)
            return;

        targetText.text = value;
    }
}
