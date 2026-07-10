using UnityEngine;
using UnityEngine.UI;

public class GameOverView : UIViewBase
{
    [Header("Text")]
    [SerializeField] private Text titleText;
    [SerializeField] private Text subtitleText;

    [Header("Buttons")]
    [SerializeField] private CommonButtonView restartChpaterButton;
    [SerializeField] private CommonButtonView loadCheckpointButton;
    [SerializeField] private CommonButtonView mainMenuButton;

    private bool canLoadCheckpoint;

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
        RefreshTexts();
        RefreshButtons();
    }

    protected override void OnHide()
    {
        ClearButtons();
    }

    // 체크포인트 재시작 가능 여부를 직접 갱신할 때 사용하는 공개 메서드
    public void SetCheckpointAvailable(bool canLoadCheckpoint)
    {
        this.canLoadCheckpoint = canLoadCheckpoint;
        RefreshButtons();
    }

    private void SubscribeEvents()
    {
        EventBus<UISetGameOverEvent>.action += HandleSetGameOver;
        EventBus<UIResetEvent>.action += HandleReset;
    }

    private void UnsubscribeEvents()
    {
        EventBus<UISetGameOverEvent>.action -= HandleSetGameOver;
        EventBus<UIResetEvent>.action -= HandleReset;
    }

    // 외부 시스템이 전달한 게임 오버 상태를 받아 View 상태에 저장
    private void HandleSetGameOver(UISetGameOverEvent eventData)
    {
        SetCheckpointAvailable(eventData.CanLoadCheckpoint);
    }

    // 전체 UI Reset 시 게임 오버 화면의 내부 상태를 초기화
    private void HandleReset(UIResetEvent eventData)
    {
        canLoadCheckpoint = false;
        ClearButtons();
    }

    // 게임 오버 화면의 고정 문구를 설정
    private void RefreshTexts()
    {
        SetText(titleText, "Game Over");
        SetText(subtitleText, "다시 읽을 방법을 선택하세요.");
    }

    // 버튼 문구, 클릭 콜백, 활성 상태를 현재 상태에 맞게 다시 설정
    public void RefreshButtons()
    {
        if (restartChpaterButton != null)
        {
            restartChpaterButton.Setup(
                "처음부터 다시 읽기",
                HandleRestartChapterClicked);
        }

        if (loadCheckpointButton != null)
        {
            loadCheckpointButton.Setup(
                "책갈피부터 다시 읽기",
                HandleLoadCheckpointClicked);
        }

        if (mainMenuButton != null)
        {
            mainMenuButton.Setup(
                "메인 화면으로 돌아가기",
                HandleMainMenuClicked);
        }
    }

    // 모든 버튼 콜백을 정리
    private void ClearButtons()
    {
        if (restartChpaterButton != null)
        {
            restartChpaterButton.Clear();
        }

        if (loadCheckpointButton != null)
        {
            loadCheckpointButton.Clear();
        }

        if (mainMenuButton != null)
        {
            mainMenuButton.Clear();
        }
    }

    // 현재 챕터 처음부터 재시작 요청을 발행
    private void HandleRestartChapterClicked()
    {
        EventBus<UIShowConfirmPopupEvent>.Publish(
            new UIShowConfirmPopupEvent(
                "처음부터 다시 읽기",
                "현재 챕터의 진행 상황을 초기화하고 처음부터 다시 시작하시겠습니까?",
                () =>
                {
                    EventBus<UIGameOverRestartChapterRequestedEvent>.Publish(
                        new UIGameOverRestartChapterRequestedEvent());
                }));
    }

    // 마지막 체크포인트에서 재시작 요청을 발행
    private void HandleLoadCheckpointClicked()
    {
        EventBus<UIGameOverLoadCheckpointRequestedEvent>.Publish(
            new UIGameOverLoadCheckpointRequestedEvent());
    }

    // 메인 화면 복귀 요청 발행
    private void HandleMainMenuClicked()
    {
        EventBus<UIGameOverMainMenuRequestedEvent>.Publish(
            new UIGameOverMainMenuRequestedEvent());
    }

    private void SetText(Text targetText, string value)
    {
        if (targetText == null)
            return;

        targetText.text = value;
    }
}
