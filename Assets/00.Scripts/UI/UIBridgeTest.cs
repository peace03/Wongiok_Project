using UnityEngine;

// UI 버튼 EventBus 흐름 테스트 브릿지
public class UIBridgeTest : MonoBehaviour
{
    private void OnEnable()
    {
        EventBus<UITitleNewGameRequestedEvent>.action += HandleTitleNewGameRequested;
        EventBus<UIChapterBackRequestedEvent>.action += HandleChapterBackRequested;
    }

    private void OnDisable()
    {
        EventBus<UITitleNewGameRequestedEvent>.action -= HandleTitleNewGameRequested;
        EventBus<UIChapterBackRequestedEvent>.action -= HandleChapterBackRequested;
    }

    private void HandleTitleNewGameRequested(UITitleNewGameRequestedEvent eventData)
    {
        EventBus<UIChangeScreenEvent>.Publish(
            new UIChangeScreenEvent(UIScreenState.ChapterSelect));
    }

    private void HandleChapterBackRequested(UIChapterBackRequestedEvent eventData)
    {
        EventBus<UIChangeScreenEvent>.Publish(
            new UIChangeScreenEvent(UIScreenState.Title));
    }

    public void ShowTitle()
    {
        EventBus<UIChangeScreenEvent>.Publish(
            new UIChangeScreenEvent(UIScreenState.Title));
    }

    public void ShowChapterSelect()
    {
        EventBus<UIChangeScreenEvent>.Publish(
            new UIChangeScreenEvent(UIScreenState.ChapterSelect));
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
                null));
    }

    public void ShowLoading()
    {
        EventBus<UIChangeScreenEvent>.Publish(
            new UIChangeScreenEvent(UIScreenState.Loading));

        EventBus<UISetLoadingProgressEvent>.Publish(
            new UISetLoadingProgressEvent(0.5f, "불러오는 중..."));
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
