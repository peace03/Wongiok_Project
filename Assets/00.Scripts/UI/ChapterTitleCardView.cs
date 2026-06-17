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

    [Header("Button")]
    [SerializeField] private CommonButtonView continueButton;

    private int currentChapterId = -1;
    private Sprite currentThumbnail;

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
        Sprite thumbnail)
    {
        currentChapterId = chapterId;
        currentThumbnail = thumbnail;

        SetText(titleText, title);
        SetText(subtitleText, subtitle);
        SetText(descriptionText, description);
        SetThumbnail(thumbnail);

        RefreshContinueButton();
    }

    public void Clear()
    {
        currentChapterId = -1;
        currentThumbnail = null;

        SetText(titleText, string.Empty);
        SetText(subtitleText, string.Empty);
        SetText(descriptionText, string.Empty);
        SetThumbnail(null);

        if (continueButton != null)
        {
            continueButton.Clear();
        }
    }

    private void SubscribeEvents()
    {
        EventBus<UISetChapterTitleCardEvent>.action += HandleSetChapterTitleCard;
    }

    private void UnsubscribeEvents()
    {
        EventBus<UISetChapterTitleCardEvent>.action -= HandleSetChapterTitleCard;
    }

    private void HandleSetChapterTitleCard(UISetChapterTitleCardEvent eventData)
    {
        Setup(
            eventData.ChapterId,
            eventData.Title,
            eventData.Subtitle,
            eventData.Description,
            eventData.Thumbnail);
    }

    private void RefreshContinueButton()
    {
        if (continueButton == null)
            return;

        continueButton.Setup("계속", HandleContinueClicked, currentChapterId >= 0);
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
}
