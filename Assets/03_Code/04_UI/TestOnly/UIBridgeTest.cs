using UnityEngine;

// UI 버튼 EventBus 흐름 테스트 브릿지
public class UIBridgeTest : MonoBehaviour
{
    private void OnEnable()
    {
        EventBus<UIChapterBackRequestedEvent>.action += HandleChapterBackRequested;
        EventBus<UIChapterTitleCardBackRequestedEvent>.action += HandleChapterTitleCardBackRequested;
    }

    private void OnDisable()
    {
        EventBus<UIChapterBackRequestedEvent>.action -= HandleChapterBackRequested;
        EventBus<UIChapterTitleCardBackRequestedEvent>.action -= HandleChapterTitleCardBackRequested;
    }

    private void HandleChapterBackRequested(UIChapterBackRequestedEvent eventData)
    {
        EventBus<UIChangeScreenEvent>.Publish(
            new UIChangeScreenEvent(UIScreenState.Title));
    }
    
    private void HandleChapterTitleCardBackRequested(UIChapterTitleCardBackRequestedEvent eventData)
    {
        PrototypeGameSession.EnsureInitialized();

        EventBus<UIChangeScreenEvent>.Publish(
            new UIChangeScreenEvent(UIScreenState.ChapterSelect));

        EventBus<UISetChapterProgressEvent>.Publish(
            new UISetChapterProgressEvent(PrototypeGameSession.HighestClearedChapterId));
    }

    public void ShowTitle()
    {
        EventBus<UIChangeScreenEvent>.Publish(
            new UIChangeScreenEvent(UIScreenState.Title));
    }

    public void ShowChapterSelect()
    {
        PrototypeGameSession.EnsureInitialized();

        EventBus<UIChangeScreenEvent>.Publish(
            new UIChangeScreenEvent(UIScreenState.ChapterSelect));

        EventBus<UISetChapterProgressEvent>.Publish(
            new UISetChapterProgressEvent(PrototypeGameSession.HighestClearedChapterId));
    }

    public void ShowChapterTitleCard()
    {
        EventBus<UIChangeScreenEvent>.Publish(
            new UIChangeScreenEvent(UIScreenState.ChapterTitleCard));

        EventBus<UISetChapterTitleCardEvent>.Publish(
            new UISetChapterTitleCardEvent(
                1,
                "Chapter 1",
                "첫 번째 이야기",
                "테스트 용",
                null,
                null,
                null));
    }

    public void ShowConfirm()
    {
        EventBus<UIShowConfirmPopupEvent>.Publish(
            new UIShowConfirmPopupEvent(
                "테스트 확인창",
                "확인/취소 버튼 동작 확인",
                () => Debug.Log("Confirm 확인"),
                () => Debug.Log("Confirm 취소")));
    }

    public void ShowAlert()
    {
        EventBus<UIShowAlertPopupEvent>.Publish(
            new UIShowAlertPopupEvent(
                "테스트 알림",
                "Alert 팝업 테스트",
                () => Debug.Log("Alert 확인")));
    }
}
