using UnityEngine;
using UnityEngine.UI;

public class ChapterTitleCardView : UIViewBase
{
    [Header("Text")]
    [SerializeField] private Text titleText;
    [SerializeField] private Text subtitleText;
    [SerializeField] private Text descriptionText;

    [Header("Visual")]
    [SerializeField] private Image thumbnailImage;
    [SerializeField] private Image backgroundImage;

    [Header("Button")]
    [SerializeField] private CommonButtonView continueButton;
    [SerializeField] private CommonButtonView backButton;

    private int currentChapterId = -1;
    private Sprite currentThumbnail;
    private Sprite currentBackground;

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
        RefreshContinueButton();
        SetupBackButton();
    }

    protected override void OnHide()
    {
        Clear();
    }

    public void Setup(
        int chapterId,
        string title,
        string subtitle,
        string description,
        Sprite thumbnail,
        Sprite background)
    {
        currentChapterId = chapterId;
        currentThumbnail = thumbnail;
        currentBackground = background;

        SetText(titleText, title);
        SetText(subtitleText, subtitle);
        SetText(descriptionText, description);
        SetThumbnail(thumbnail);
        SetBackground(background);

        RefreshContinueButton();
    }

    public void Clear()
    {
        currentChapterId = -1;
        currentThumbnail = null;
        currentBackground = null;

        SetText(titleText, string.Empty);
        SetText(subtitleText, string.Empty);
        SetText(descriptionText, string.Empty);
        SetThumbnail(null);
        SetBackground(null);

        if (continueButton != null)
        {
            continueButton.Clear();
        }

        if (backButton != null)
        {
            backButton.Clear();
        }
    }

    private void SubscribeEvents()
    {
        EventBus<UISetChapterTitleCardEvent>.action += HandleSetChapterTitleCard;
        EventBus<UIChapterTitleCardInputContinueRequestedEvent>.action += HandleInputContinueRequested;
    }

    private void UnsubscribeEvents()
    {
        EventBus<UISetChapterTitleCardEvent>.action -= HandleSetChapterTitleCard;
        EventBus<UIChapterTitleCardInputContinueRequestedEvent>.action -= HandleInputContinueRequested;
    }

    private void HandleSetChapterTitleCard(UISetChapterTitleCardEvent eventData)
    {
        Setup(
            eventData.ChapterId,
            eventData.Title,
            eventData.Subtitle,
            eventData.Description,
            eventData.Thumbnail,
            eventData.Background);
    }

    private void HandleInputContinueRequested(UIChapterTitleCardInputContinueRequestedEvent eventData)
    {
        if (!IsVisible)
            return;

        HandleContinueClicked();
    }

    private void HandleBackClicked()
    {
        EventBus<UIChapterTitleCardBackRequestedEvent>.Publish(default);
    }

    private void RefreshContinueButton()
    {
        if (continueButton == null)
            return;

        continueButton.Setup("계속", HandleContinueClicked, currentChapterId >= 0);
    }

    private void SetupBackButton()
    {
        if (backButton != null)
        {
            backButton.Setup("뒤로가기", HandleBackClicked);
        }
    }

    private void HandleContinueClicked()
    {
        if (currentChapterId < 0)
            return;

        EventBus<UIChapterTitleCardContinueRequestedEvent>.Publish(
            new UIChapterTitleCardContinueRequestedEvent(currentChapterId));
    }

    private void SetText(Text targetText, string value)
    {
        if (targetText == null)
            return;

        targetText.text = value;
    }

    private void SetThumbnail(Sprite thumbnail)
    {
        if (thumbnailImage == null)
            return;

        thumbnailImage.sprite = thumbnail;
        thumbnailImage.enabled = thumbnail != null;

    }

    private void SetBackground(Sprite background)
    {
        if (backgroundImage == null)
            return;

        backgroundImage.sprite = background;
        backgroundImage.enabled = background != null;
    }
}
