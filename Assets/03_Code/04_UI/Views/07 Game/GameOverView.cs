using UnityEngine;

public class GameOverView : UIViewBase
{
    [Header("Buttons")]
    [SerializeField] private CommonButtonView restartChpaterButton;
    [SerializeField] private CommonButtonView loadCheckpointButton;
    [SerializeField] private CommonButtonView mainMenuButton;

    // 2026.08.07_psb수정
    private bool hasCheckpoint;
    private bool hasRemainingLife;

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
        RefreshButtons();
    }

    // 2026.08.10_UI 정리: 화면 숨김 시 임시 UI 상태를 정리한다.
    protected override void OnHide()
    {
        ClearButtons();
    }

    // 체크포인트 재시작 가능 여부를 직접 갱신할 때 사용하는 공개 메서드
    public void SetCheckpointAvailable(
        bool hasCheckpoint,
        bool hasRemainingLife)
    {
        this.hasCheckpoint = hasCheckpoint;
        this.hasRemainingLife = hasRemainingLife;
        RefreshButtons();
    }

    // 2026.08.10_UI 정리: 이벤트 이벤트를 구독한다.
    private void SubscribeEvents()
    {
        EventBus<UISetGameOverEvent>.action += HandleSetGameOver;
        EventBus<UIResetEvent>.action += HandleReset;
    }

    // 2026.08.10_UI 정리: 이벤트 이벤트 구독을 해제한다.
    private void UnsubscribeEvents()
    {
        EventBus<UISetGameOverEvent>.action -= HandleSetGameOver;
        EventBus<UIResetEvent>.action -= HandleReset;
    }

    // 외부 시스템이 전달한 게임 오버 상태를 받아 View 상태에 저장
    private void HandleSetGameOver(UISetGameOverEvent eventData)
    {
        SetCheckpointAvailable(
            eventData.HasCheckpoint,
            eventData.HasRemainingLife);
    }

    // 전체 UI Reset 시 게임 오버 화면의 내부 상태를 초기화
    private void HandleReset(UIResetEvent eventData)
    {
        hasCheckpoint = false;
        hasRemainingLife = false;
        ClearButtons();
    }

    // 게임 오버 화면의 고정 문구를 설정
    // 버튼 문구, 클릭 콜백, 활성 상태를 현재 상태에 맞게 다시 설정
    public void RefreshButtons()
    {
        if (restartChpaterButton != null)
        {
            restartChpaterButton.Setup(
                UITextManager.Get("GameOver.Restart"),
                HandleRestartChapterClicked);
        }

        if (loadCheckpointButton != null)
        {
            loadCheckpointButton.Setup(
                UITextManager.Get("GameOver.LoadCheckpoint"),
                HandleLoadCheckpointClicked,
                hasCheckpoint);
        }

        if (mainMenuButton != null)
        {
            mainMenuButton.Setup(
                UITextManager.Get("GameOver.MainMenu"),
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
                UITextManager.Get("GameOver.RestartConfirmTitle"),
                UITextManager.Get("GameOver.RestartConfirmMessage"),
                () =>
                {
                    EventBus<UIGameOverRestartChapterRequestedEvent>.Publish(
                        new UIGameOverRestartChapterRequestedEvent());
                }));
    }

    // 마지막 체크포인트에서 재시작 요청을 발행
    private void HandleLoadCheckpointClicked()
    {
        // 2026.08.07_psb수정
        // 목숨을 모두 소진했을 때는 복구 요청 대신 안내 Alert를 표시한다.
        if (!hasRemainingLife)
        {
            EventBus<UIShowAlertPopupEvent>.Publish(
                new UIShowAlertPopupEvent(
                    UITextManager.Get("Common.NoticeTitle"),
                    UITextManager.Get("GameOver.LifeDepletedMessage")));
            return;
        }

        EventBus<UIGameOverLoadCheckpointRequestedEvent>.Publish(
            new UIGameOverLoadCheckpointRequestedEvent());
    }

    // 메인 화면 복귀 요청 발행
    private void HandleMainMenuClicked()
    {
        EventBus<UIGameOverMainMenuRequestedEvent>.Publish(
            new UIGameOverMainMenuRequestedEvent());
    }

    // 2026.08.10_UI 정리: 텍스트 표시 값을 반영한다.
}
