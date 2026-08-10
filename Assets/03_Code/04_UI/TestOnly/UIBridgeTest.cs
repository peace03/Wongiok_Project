using UnityEngine;

// UI 버튼 EventBus 흐름 테스트 브릿지
public class UIBridgeTest : MonoBehaviour
{
    // 2026.08.10_UI 정리: 활성화 시 필요한 UI 상태와 이벤트 구독을 준비한다.
    private void OnEnable()
    {
        EventBus<UIChapterBackRequestedEvent>.action += HandleChapterBackRequested;
        EventBus<UIChapterTitleCardBackRequestedEvent>.action += HandleChapterTitleCardBackRequested;
    }

    // 2026.08.10_UI 정리: 비활성화 시 등록한 이벤트와 임시 UI 상태를 정리한다.
    private void OnDisable()
    {
        EventBus<UIChapterBackRequestedEvent>.action -= HandleChapterBackRequested;
        EventBus<UIChapterTitleCardBackRequestedEvent>.action -= HandleChapterTitleCardBackRequested;
    }

    // 2026.08.10_UI 정리: 챕터 뒤로가기 Requested 관련 입력 또는 EventBus 요청을 처리한다.
    private void HandleChapterBackRequested(UIChapterBackRequestedEvent eventData)
    {
        EventBus<UIChangeScreenEvent>.Publish(
            new UIChangeScreenEvent(UIScreenState.Title));
    }
    
    // 2026.08.10_UI 정리: 챕터 타이틀 카드 뒤로가기 Requested 관련 입력 또는 EventBus 요청을 처리한다.
    private void HandleChapterTitleCardBackRequested(UIChapterTitleCardBackRequestedEvent eventData)
    {
        PrototypeGameSession.EnsureInitialized();

        EventBus<UIChangeScreenEvent>.Publish(
            new UIChangeScreenEvent(UIScreenState.ChapterSelect));

        EventBus<UISetChapterProgressEvent>.Publish(
            new UISetChapterProgressEvent(PrototypeGameSession.HighestClearedChapterId));
    }
}
