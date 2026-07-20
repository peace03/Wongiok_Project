using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using System.Collections;

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

    [Header("로딩 세션")]
    [SerializeField] private VideoPlayer loadingVideoPlayer;
    [SerializeField] private RawImage loadingVideoImage;
    [SerializeField] private RectTransform loadingSpinner;
    [SerializeField] private GameObject[] cardDetailObjects;
    [SerializeField, Min(0f)] private float titleCardZoomDuration = 0.5f;
    [SerializeField] private float loadingSpinnerSpeed = 180f;
    [SerializeField, Min(1f)] private float titleCardZoomEndScale = 1.35f;
    [SerializeField] private Vector2 titleCardZoomEndPosition = Vector2.zero;

    private VideoClip currentLoadingVideoClip;
    private bool isTransitioning;
    private bool isLoadingVisualActive;
    private Coroutine transitionCoroutine;

    private Vector2 initialAnchorMin;
    private Vector2 initialAnchorMax;
    private Vector2 initialAnchorPosition;
    private Vector2 initialSizeDelta;
    private Vector3 initialLocalScale;
    private bool[] initialDetailActiveStates;

    private int currentChapterId = -1;
    private Sprite currentThumbnail;
    private Sprite currentBackground;

    protected override void Awake()
    {
        base.Awake();

        CaptureInitialVisualState();
        SubscribeEvents();
    }

    private void OnDestroy()
    {
        UnsubscribeEvents();
    }

    protected override void OnShow()
    {
        ResetLoadingVisual();
        RefreshContinueButton();
        SetupBackButton();
    }

    protected override void OnHide()
    {
        ResetLoadingVisual();
        currentLoadingVideoClip = null;
        Clear();
    }

    private void Update()
    {
        if (!isLoadingVisualActive || loadingSpinner == null) return;

        loadingSpinner.Rotate(0f, 0f, -loadingSpinnerSpeed * Time.unscaledDeltaTime);
    }

    public void Setup(
        int chapterId,
        string title,
        string subtitle,
        string description,
        Sprite thumbnail,
        Sprite background,
        VideoClip loadingVideoClip)
    {
        currentChapterId = chapterId;
        currentThumbnail = thumbnail;
        currentBackground = background;
        currentLoadingVideoClip = loadingVideoClip;

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
            eventData.Background,
            eventData.LoadingVideoClip);
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
        if (currentChapterId < 0 || isTransitioning) return;

        isTransitioning = true;

        if (continueButton != null)
            continueButton.SetInteractable(false);

        if (backButton != null)
            backButton.SetInteractable(false);

        transitionCoroutine = StartCoroutine(PlayLoadingTransition());
    }

    private IEnumerator PlayLoadingTransition()
    {
        RectTransform titleCardRect = thumbnailImage.rectTransform;

        Vector2 startPosition = titleCardRect.anchoredPosition;
        Vector3 startScale = titleCardRect.localScale;

        if (continueButton != null)
            continueButton.gameObject.SetActive(false);

        if (backButton != null)
            backButton.gameObject.SetActive(false);

        Vector3 targetScale = initialLocalScale * titleCardZoomEndScale;
        Vector2 targetPosition = titleCardZoomEndPosition;

        float elapsed = 0f;

        while (elapsed < titleCardZoomDuration)
        {
            elapsed += Time.unscaledDeltaTime;

            float progress = titleCardZoomDuration <= 0f
                ? 1f
                : Mathf.Clamp01(elapsed / titleCardZoomDuration);

            titleCardRect.anchoredPosition = Vector2.Lerp(
                startPosition,
                targetPosition,
                progress);

            titleCardRect.localScale = Vector3.Lerp(
                startScale,
                targetScale,
                progress);

            yield return null;
        }

        titleCardRect.anchoredPosition = targetPosition;
        titleCardRect.localScale = targetScale;

        SetCardDetailObjectsActive(false);

        yield return StartCoroutine(StartLoadingVisualAfterVideoStarts());

        EventBus<UIChapterTitleCardContinueRequestedEvent>.Publish(
            new UIChapterTitleCardContinueRequestedEvent(currentChapterId));

        transitionCoroutine = null;
    }

    private IEnumerator StartLoadingVisualAfterVideoStarts()
    {
        isLoadingVisualActive = true;

        if (loadingSpinner != null)
        {
            loadingSpinner.localRotation = Quaternion.identity;
            loadingSpinner.gameObject.SetActive(true);
        }

        if (currentLoadingVideoClip == null || loadingVideoPlayer == null || loadingVideoImage == null)
        {
            yield break;
        }

        loadingVideoImage.gameObject.SetActive(true);

        loadingVideoPlayer.Stop();
        loadingVideoPlayer.clip = currentLoadingVideoClip;
        loadingVideoPlayer.isLooping = true;
        loadingVideoPlayer.Prepare();

        while (!loadingVideoPlayer.isPrepared)
            yield return null;

        loadingVideoPlayer.Play();

        while (!loadingVideoPlayer.isPlaying)
            yield return null;

        yield return null;
    }

    private void CaptureInitialVisualState()
    {
        if (thumbnailImage != null)
        {
            RectTransform titleCardRect = thumbnailImage.rectTransform;

            initialAnchorMin = titleCardRect.anchorMin;
            initialAnchorMax = titleCardRect.anchorMax;
            initialAnchorPosition = titleCardRect.anchoredPosition;
            initialSizeDelta = titleCardRect.sizeDelta;
            initialLocalScale = titleCardRect.localScale;
        }

        initialDetailActiveStates = new bool[cardDetailObjects.Length];

        for (int i = 0; i < cardDetailObjects.Length; i++)
        {
            if (cardDetailObjects[i] != null)
            {
                initialDetailActiveStates[i] = cardDetailObjects[i].activeSelf;
            }
        }
    }

    private void ResetLoadingVisual()
    {
        if (transitionCoroutine != null)
        {
            StopCoroutine(transitionCoroutine);
            transitionCoroutine = null;
        }

        isTransitioning = false;
        isLoadingVisualActive = false;

        if (thumbnailImage != null)
        {
            RectTransform titleCardRect = thumbnailImage.rectTransform;

            titleCardRect.anchorMin = initialAnchorMin;
            titleCardRect.anchorMax = initialAnchorMax;
            titleCardRect.anchoredPosition = initialAnchorPosition;
            titleCardRect.sizeDelta = initialSizeDelta;
            titleCardRect.localScale = initialLocalScale;
        }

        if (loadingVideoPlayer != null)
        {
            loadingVideoPlayer.Stop();
            loadingVideoPlayer.clip = null;
        }

        if (loadingVideoImage != null)
        {
            loadingVideoImage.gameObject.SetActive(false);
        }

        if (loadingSpinner != null)
        {
            loadingSpinner.localRotation = Quaternion.identity;
            loadingSpinner.gameObject.SetActive(false);
        }

        RestoreCardDetailObjects();
    }

    private void SetCardDetailObjectsActive(bool isActive)
    {
        foreach (GameObject detailObject in cardDetailObjects)
        {
            if (detailObject != null)
                detailObject.SetActive(isActive);
        }
    }

    private void RestoreCardDetailObjects()
    {
        for (int i = 0; i < cardDetailObjects.Length; i++)
        {
            if (cardDetailObjects[i] != null)
            {
                cardDetailObjects[i].SetActive(initialDetailActiveStates[i]);
            }
        }
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
