using UnityEngine;

public class UIInputBridge : MonoBehaviour, IInitializable
{
    [SerializeField] private UIManager uiManager;
    [SerializeField] private GameInputReader _input;

    private bool isInitialized;
    private bool isCursorRefreshQueued;

    // UIManager가 먼저 초기화된 뒤 ServiceLocator에서 가져올 수 있도록 UI보다 뒤에 초기화
    public int Priority => (int)InitOrder.UI + 30;

    // 2026.08.10_UI 정리: 이 메서드의 UI 처리 역할을 수행한다.
    public void Init()
    {
        if (isInitialized)
            return;

        if (uiManager == null)
        {
            uiManager = ServiceLocator.Get<UIManager>();
        }

        SubscribeCursorEvents();
        RefreshCursorState();
        QueueCursorStateRefresh();
        isInitialized = true;
    }

    // 2026.08.10_UI 정리: 파괴 시 등록한 이벤트와 임시 UI 상태를 정리한다.
    private void OnDestroy()
    {
        if (!isInitialized)
            return;

        UnsubscribeCursorEvents();
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    // 2026.08.10_UI 정리: 프레임 단위 UI 상태와 입력을 갱신한다.
    private void Update()
    {
        if (!isInitialized) return;

        if (uiManager == null) return;

        if (uiManager.CurrentPopupType != UIPopupType.None) return;

        HandlePrologueSkipInput();
        HandleEscapeInput();
        HandlePauseTabInput();
        HandleSpaceInput();
        HandleChapterLoadingSkipInput();
        HandleCutsceneBackQuoteInput();
    }

    // 2026.08.10_UI 정리: 프레임 종료 시점에 UI 상태를 보정한다.
    private void LateUpdate()
    {
        if (!isInitialized || !isCursorRefreshQueued)
            return;

        isCursorRefreshQueued = false;
        RefreshCursorState();
    }

    // 2026.08.10_UI 정리: 프롤로그 Skip 입력 관련 입력 또는 EventBus 요청을 처리한다.
    private void HandlePrologueSkipInput()
    {
        if (uiManager.CurrentScreenState != UIScreenState.Prologue) return;

        //if (!Input.GetKeyDown(KeyCode.BackQuote)) return;
        if (!_input.TitleStartPressed) return;

        EventBus<UICutsceneSkipRequestedEvent>.Publish(
            new UICutsceneSkipRequestedEvent(CutsceneSkipInput.BackQuote));
    }

    // 2026.08.10_UI 정리: Escape 입력 관련 입력 또는 EventBus 요청을 처리한다.
    private void HandleEscapeInput()
    {
        if (!_input.MenuPressed)
            return;

        if (uiManager.CurrentOverlayState == UIOverlayState.Cutscene)
        {
            EventBus<UICutsceneSkipRequestedEvent>.Publish(
                new UICutsceneSkipRequestedEvent(CutsceneSkipInput.Escape));
            return;
        }

        if (uiManager.CurrentOverlayState == UIOverlayState.Pause)
        {
            EventBus<UICloseOverlayEvent>.Publish(
                new UICloseOverlayEvent(UIOverlayState.Pause));
            return;
        }

        if (uiManager.CurrentOverlayState == UIOverlayState.LevelUp)
        {
            return;
        }

        if (uiManager.CurrentScreenState == UIScreenState.InGame &&
            uiManager.CurrentOverlayState == UIOverlayState.None)
        {
            EventBus<UIOpenOverlayEvent>.Publish(
                new UIOpenOverlayEvent(UIOverlayState.Pause));
        }
    }

    // 2026.08.10_UI 정리: 일시정지 탭 입력 관련 입력 또는 EventBus 요청을 처리한다.
    private void HandlePauseTabInput()
    {
        if (uiManager.CurrentOverlayState != UIOverlayState.Pause)
            return;

        if (_input.PreviousPauseTabPressed)
        {
            EventBus<UIPauseTabMoveRequestedEvent>.Publish(
                new UIPauseTabMoveRequestedEvent(-1));
            return;
        }

        if (_input.NextPauseTabPressed)
        {
            EventBus<UIPauseTabMoveRequestedEvent>.Publish(
                new UIPauseTabMoveRequestedEvent(1));
        }
    }

    // 2026.08.10_UI 정리: Space 입력 관련 입력 또는 EventBus 요청을 처리한다.
    private void HandleSpaceInput()
    {
        if (uiManager.CurrentOverlayState == UIOverlayState.Cutscene)
        {
            if (!_input.SubmitPressed) return;

            EventBus<UICutsceneProceedRequestedEvent>.Publish(
                new UICutsceneProceedRequestedEvent());
            return;
        }

        if (uiManager.CurrentScreenState != UIScreenState.ChapterTitleCard) return;

        if (!_input.SubmitPressed) return;

        EventBus<UIChapterTitleCardInputContinueRequestedEvent>.Publish(
            new UIChapterTitleCardInputContinueRequestedEvent());
    }

    // 2026.08.10_UI 정리: 챕터 로딩 Skip 입력 관련 입력 또는 EventBus 요청을 처리한다.
    private void HandleChapterLoadingSkipInput()
    {
        if (uiManager.CurrentScreenState != UIScreenState.ChapterTitleCard) return;

        if (!_input.TitleStartPressed) return;

        EventBus<UIChapterTitleCardInputSkipRequestedEvent>.Publish(
            new UIChapterTitleCardInputSkipRequestedEvent());
    }

    // 2026.08.10_UI 정리: 컷신 뒤로가기 Quote 입력 관련 입력 또는 EventBus 요청을 처리한다.
    private void HandleCutsceneBackQuoteInput()
    {
        if (uiManager.CurrentOverlayState != UIOverlayState.Cutscene) return;

        if (!_input.TitleStartPressed) return;

        EventBus<UICutsceneSkipRequestedEvent>.Publish(
            new UICutsceneSkipRequestedEvent(CutsceneSkipInput.BackQuote));
    }

    // 2026.08.07_psb수정
    // 화면과 오버레이가 바뀔 때마다 게임 플레이 전용 커서 정책을 다시 적용한다.
    private void SubscribeCursorEvents()
    {
        EventBus<UIChangeScreenEvent>.action += HandleUiStateChanged;
        EventBus<UIOpenOverlayEvent>.action += HandleOverlayOpened;
        EventBus<UICloseOverlayEvent>.action += HandleOverlayClosed;
        EventBus<UIResetEvent>.action += HandleUiReset;
    }

    // 2026.08.07_psb수정
    // 오브젝트가 파괴될 때 정적 EventBus 구독을 해제한다.
    private void UnsubscribeCursorEvents()
    {
        EventBus<UIChangeScreenEvent>.action -= HandleUiStateChanged;
        EventBus<UIOpenOverlayEvent>.action -= HandleOverlayOpened;
        EventBus<UICloseOverlayEvent>.action -= HandleOverlayClosed;
        EventBus<UIResetEvent>.action -= HandleUiReset;
    }

    // 2026.08.07_psb수정
    // 화면 전환 후 커서 표시 상태를 현재 UI 상태에 맞춘다.
    private void HandleUiStateChanged(UIChangeScreenEvent eventData)
    {
        QueueCursorStateRefresh();
    }

    // 2026.08.07_psb수정
    // 오버레이가 열린 프레임에 커서 정책을 다시 적용한다.
    private void HandleOverlayOpened(UIOpenOverlayEvent eventData)
    {
        QueueCursorStateRefresh();
    }

    // 2026.08.07_psb수정
    // 오버레이가 닫힌 프레임에 커서 정책을 다시 적용한다.
    private void HandleOverlayClosed(UICloseOverlayEvent eventData)
    {
        QueueCursorStateRefresh();
    }

    // 2026.08.07_psb수정
    // UI 전체 초기화 뒤에도 타이틀과 인게임 상태에 맞는 커서를 보장한다.
    private void HandleUiReset(UIResetEvent eventData)
    {
        QueueCursorStateRefresh();
    }

    // 2026.08.07_psb수정
    // EventBus 요청 처리로 UI 상태가 확정된 뒤 같은 프레임의 LateUpdate에서 커서를 갱신한다.
    private void QueueCursorStateRefresh()
    {
        isCursorRefreshQueued = true;
    }

    // 2026.08.07_psb수정
    // 일반 인게임과 컷신에서는 숨기고, 메뉴·일시정지·레벨업에서는 포인터를 표시한다.
    private void RefreshCursorState()
    {
        if (uiManager == null)
            return;

        bool shouldShowCursor =
            uiManager.CurrentScreenState != UIScreenState.InGame ||
            uiManager.CurrentOverlayState == UIOverlayState.Pause ||
            uiManager.CurrentOverlayState == UIOverlayState.LevelUp;

        Cursor.visible = shouldShowCursor;
        Cursor.lockState = shouldShowCursor
            ? CursorLockMode.None
            : CursorLockMode.Locked;
    }
}
