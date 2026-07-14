using UnityEngine;

public class UIInputBridge : MonoBehaviour, IInitializable
{
    [SerializeField] private UIManager uiManager;

    private bool isInitialized;

    // UIManager가 먼저 초기화된 뒤 ServiceLocator에서 가져올 수 있도록 UI보다 뒤에 초기화
    public int Priority => (int)InitOrder.UI + 30;

    public void Init()
    {
        if (isInitialized)
            return;

        if (uiManager == null)
        {
            uiManager = ServiceLocator.Get<UIManager>();
        }

        isInitialized = true;
    }

    private void Update()
    {
        if (!isInitialized)
            return;

        if (uiManager == null)
            return;

        if (uiManager.CurrentPopupType != UIPopupType.None)
            return;

        HandleEscapeInput();
        HandlePauseTabInput();
        HandleSpaceInput();
    }

    private void HandleEscapeInput()
    {
        if (!Input.GetKeyDown(KeyCode.Escape))
            return;

        if (uiManager.CurrentOverlayState == UIOverlayState.Cutscene)
        {
            EventBus<UICutsceneSkipRequestedEvent>.Publish(
                new UICutsceneSkipRequestedEvent());
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

    private void HandlePauseTabInput()
    {
        if (uiManager.CurrentOverlayState != UIOverlayState.Pause)
            return;

        if (Input.GetKeyDown(KeyCode.Q))
        {
            EventBus<UIPauseTabMoveRequestedEvent>.Publish(
                new UIPauseTabMoveRequestedEvent(-1));
            return;
        }

        if (Input.GetKeyDown(KeyCode.E))
        {
            EventBus<UIPauseTabMoveRequestedEvent>.Publish(
                new UIPauseTabMoveRequestedEvent(1));
        }
    }

    private void HandleSpaceInput()
    {
        if (!Input.GetKeyDown(KeyCode.Space))
            return;

        if (uiManager.CurrentScreenState != UIScreenState.ChapterTitleCard)
            return;

        EventBus<UIChapterTitleCardInputContinueRequestedEvent>.Publish(
            new UIChapterTitleCardInputContinueRequestedEvent());
    }
}
