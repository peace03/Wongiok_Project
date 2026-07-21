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
}
