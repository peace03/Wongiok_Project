using UnityEngine;

public class TitleView : UIViewBase
{
    [Header("Buttons")]
    [SerializeField] private CommonButtonView newGameButton;
    [SerializeField] private CommonButtonView continueButton;
    [SerializeField] private CommonButtonView exitButton;

    // 저장 파일이 존재하는지 여부
    // 실제 파일 검사는 다른 쪽에서 처리하고 전달 받은 상태만 적용
    private bool hasSaveFile;

    // 구독 및 구독 해제
    protected override void Awake()
    {
        base.Awake();
        SubscribeEvents();
    }

    private void OnDestroy()
    {
        UnsubscribeEvents();
    }

    // 상황에 맞는 버튼 기능 동작
    protected override void OnShow()
    {
        RefreshButtons();
    }

    // 숨기면서 초기화
    protected override void OnHide()
    {
        ClearButtons();
    }

    // 저장 파일이 존재하는지 외부에서 확인해 받음
    public void SetSaveFileAvailable(bool hasSaveFile)
    {
        this.hasSaveFile = hasSaveFile;
        RefreshButtons();
    }

    // if문을 사용해 상황별 기능 세팅
    public void RefreshButtons()
    {
        if (newGameButton != null)
        {
            newGameButton.Setup("New Story", HandleNewGameClicked);
        }

        if (continueButton != null)
        {
            continueButton.Setup("Continue", HandleContinueClicked, hasSaveFile);
        }

        if (exitButton != null)
        {
            exitButton.Setup("Quit", HandleExitClicked);
        }
    }

    private void SubscribeEvents()
    {
        EventBus<UISetTitleSaveStateEvent>.action += HandleSetTitleSaveState;
    }

    private void UnsubscribeEvents()
    {
        EventBus<UISetTitleSaveStateEvent>.action -= HandleSetTitleSaveState;
    }

    private void HandleSetTitleSaveState(UISetTitleSaveStateEvent eventData)
    {
        SetSaveFileAvailable(eventData.HasSaveFile);
    }

    private void HandleNewGameClicked()
    {
        if (hasSaveFile)
        {
            EventBus<UIShowConfirmPopupEvent>.Publish(
                new UIShowConfirmPopupEvent(
                    "새 게임 시작",
                    "이전 데이터를 파기하고 새로 시작하시겠습니까?",
                    PublishNewGameRequested));

            return;
        }

        PublishNewGameRequested();
    }

    private void HandleContinueClicked()
    {
        if (!hasSaveFile)
            return;

        EventBus<UITitleContinueRequestedEvent>.Publish(default);
    }

    private void HandleExitClicked()
    {
        EventBus<UIShowConfirmPopupEvent>.Publish(
            new UIShowConfirmPopupEvent(
                "게임 종료",
                "정말 게임 종료?",
                PublishExitRequested));
    }

    private void PublishNewGameRequested()
    {
        EventBus<UITitleNewGameRequestedEvent>.Publish(default);
    }

    private void PublishExitRequested()
    {
        EventBus<UITitleExitRequestedEvent>.Publish(default);
    }

    private void ClearButtons()
    {
        if (newGameButton != null)
        {
            newGameButton.Clear();
        }

        if (continueButton != null)
        {
            continueButton.Clear();
        }

        if (exitButton != null)
        {
            exitButton.Clear(); 
        }
    }
}
