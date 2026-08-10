using UnityEngine;

// 챕터 클리어 결과 화면 전체를 담당하는 View
public class GameClearView : UIViewBase
{
    [Header("Buttons")]
    // 다음 챕터 진행
    [SerializeField] private CommonButtonView nextChapterButton;
    // 메인 메뉴로 복귀
    [SerializeField] private CommonButtonView mainMenuButton;
    // 게임 종료
    [SerializeField] private CommonButtonView quitGameButton;

    // 다음 챕터 존재 여부
    private bool hasNextChapter;

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

    // 2026.08.10_UI 정리: 다음 챕터 Available 표시 값을 반영한다.
    public void SetNextChapterAvailable(bool hasNextChapter)
    {
        this.hasNextChapter = hasNextChapter;
        RefreshButtons();
    }

    // 2026.08.10_UI 정리: 이벤트 이벤트를 구독한다.
    private void SubscribeEvents()
    {
        EventBus<UISetChapterClearEvent>.action += HandleSetChapterClear;
        EventBus<UIResetEvent>.action += HandleReset;
    }

    // 2026.08.10_UI 정리: 이벤트 이벤트 구독을 해제한다.
    private void UnsubscribeEvents()
    {
        EventBus<UISetChapterClearEvent>.action -= HandleSetChapterClear;
        EventBus<UIResetEvent>.action -= HandleReset;
    }

    // 2026.08.10_UI 정리: Set 챕터 클리어 관련 입력 또는 EventBus 요청을 처리한다.
    private void HandleSetChapterClear(UISetChapterClearEvent eventData)
    {
        SetNextChapterAvailable(eventData.HasNextChapter);
    }

    // 2026.08.10_UI 정리: 초기화 관련 입력 또는 EventBus 요청을 처리한다.
    private void HandleReset(UIResetEvent eventData)
    {
        hasNextChapter = false;
        ClearButtons();
    }

    // 2026.08.10_UI 정리: 현재 데이터로 Texts 표시를 갱신한다.
    // 2026.08.10_UI 정리: 현재 데이터로 Buttons 표시를 갱신한다.
    public void RefreshButtons()
    {
        if (nextChapterButton != null)
        {
            nextChapterButton.Setup(
                UITextManager.Get("GameClear.Next"),
                HandleNextChapterClicked);
        }

        if (mainMenuButton != null)
        {
            mainMenuButton.Setup(
                UITextManager.Get("GameClear.MainMenu"),
                HandleMainMenuClicked);
        }

        if (quitGameButton != null)
        {
            quitGameButton.Setup(
                UITextManager.Get("GameClear.Quit"),
                HandleQuitGameClicked);
        }
    }

    // 2026.08.10_UI 정리: Buttons 상태를 정리한다.
    private void ClearButtons()
    {
        if (nextChapterButton != null)
        {
            nextChapterButton.Clear();
        }

        if (mainMenuButton != null)
        {
            mainMenuButton.Clear();
        }

        if (quitGameButton != null)
        {
            quitGameButton.Clear();
        }
    }

    // 다음 챕터가 없으면 버튼 클릭 X
    private void HandleNextChapterClicked()
    {
        //if (!hasNextChapter) return;

        EventBus<UIChapterClearNextRequestedEvent>.Publish(
            new UIChapterClearNextRequestedEvent());
    }

    // 2026.08.10_UI 정리: 메인 메뉴 클릭 관련 입력 또는 EventBus 요청을 처리한다.
    private void HandleMainMenuClicked()
    {
        EventBus<UIChapterClearMainMenuRequestedEvent>.Publish(
            new UIChapterClearMainMenuRequestedEvent());
    }

    // 게임 종료 확인 팝업이 먼저 나옴
    private void HandleQuitGameClicked()
    {
        EventBus<UIShowConfirmPopupEvent>.Publish(
            new UIShowConfirmPopupEvent(
                UITextManager.Get("GameClear.QuitConfirmTitle"),
                UITextManager.Get("GameClear.QuitConfirmMessage"),
                () =>
                {
                    EventBus<UIChapterClearQuitGameRequestedEvent>.Publish(
                        new UIChapterClearQuitGameRequestedEvent());
                }));
    }

    // 2026.08.10_UI 정리: 텍스트 표시 값을 반영한다.
}
