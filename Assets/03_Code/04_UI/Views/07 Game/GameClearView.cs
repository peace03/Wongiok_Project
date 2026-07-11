using UnityEngine;
using UnityEngine.UI;

// 챕터 클리어 결과 화면 전체를 담당하는 View
public class GameClearView : UIViewBase
{
    [Header("Text")]
    [SerializeField] private Text titleText;
    [SerializeField] private Text subtitleText;

    [Header("Buttons")]
    // 다음 챕터 진행
    [SerializeField] private CommonButtonView nextChapterButton;
    // 메인 메뉴로 복귀
    [SerializeField] private CommonButtonView mainMenuButton;
    // 게임 종료
    [SerializeField] private CommonButtonView quitGameButton;

    // 다음 챕터 존재 여부
    private bool hasNextChapter;

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

    public void SetNextChapterAvailable(bool hasNextChapter)
    {
        this.hasNextChapter = hasNextChapter;
        RefreshButtons();
    }

    private void SubscribeEvents()
    {
        EventBus<UISetChapterClearEvent>.action += HandleSetChapterClear;
        EventBus<UIResetEvent>.action += HandleReset;
    }

    private void UnsubscribeEvents()
    {
        EventBus<UISetChapterClearEvent>.action -= HandleSetChapterClear;
        EventBus<UIResetEvent>.action -= HandleReset;
    }

    private void HandleSetChapterClear(UISetChapterClearEvent eventData)
    {
        SetNextChapterAvailable(eventData.HasNextChapter);
    }

    private void HandleReset(UIResetEvent eventData)
    {
        hasNextChapter = false;
        ClearButtons();
    }

    private void RefreshTexts()
    {
        SetText(titleText, "Chapter Clear");
        SetText(subtitleText, "다음 이야기를 선택하세요.");
    }

    public void RefreshButtons()
    {
        if (nextChapterButton != null)
        {
            nextChapterButton.Setup(
                "다음 이야기 읽기",
                HandleNextChapterClicked,
                hasNextChapter);
        }

        if (mainMenuButton != null)
        {
            mainMenuButton.Setup(
                "메인 메뉴",
                HandleMainMenuClicked);
        }

        if (quitGameButton != null)
        {
            quitGameButton.Setup(
                "게임 종료",
                HandleQuitGameClicked);
        }
    }

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
        if (!hasNextChapter)
            return;

        EventBus<UIChapterClearNextRequestedEvent>.Publish(
            new UIChapterClearNextRequestedEvent());
    }

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
                "게임 종료",
                "게임을 종료하시겠습니까?",
                () =>
                {
                    EventBus<UIChapterClearQuitGameRequestedEvent>.Publish(
                        new UIChapterClearQuitGameRequestedEvent());
                }));
    }

    private void SetText(Text targetText, string value)
    {
        if (targetText == null)
            return;

        targetText.text = value;
    }
}
