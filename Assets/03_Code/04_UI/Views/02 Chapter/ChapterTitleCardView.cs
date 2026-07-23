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

    [Header("로딩 세션 페이드")]
    [SerializeField] private float zoomFadeOutDuration = 0.5f;
    [SerializeField] private float loadingVideoFadeInDuration = 0.5f;
    [SerializeField] private float videoEndFadeOutDuration = 0.5f;

    [Header("로딩 안내")]
    [SerializeField] private GameObject loadingSkipGuideObject;
    [SerializeField] private GameObject proceedGuideObject;

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

    private bool isKeyboardInputReady;
    private Coroutine keyboardInputReadyCoroutine;

    private enum LoadingTransitionState
    {
        Idle,
        ZoomingOut,
        VideoPlaying,
        WaitingForProceed
    }

    private LoadingTransitionState loadingTransitionState;
    private bool isLoadingReady;
    private bool isVideoFinished;
    private bool isFinishingVideo;

    protected override void Awake()
    {
        base.Awake();

        CaptureInitialVisualState();
        SubscribeEvents();

        if (loadingVideoPlayer != null)
            loadingVideoPlayer.loopPointReached += HandleLoadingVideoFinished;
    }

    private void OnDestroy()
    {
        UnsubscribeEvents();

        if (loadingVideoPlayer != null)
            loadingVideoPlayer.loopPointReached -= HandleLoadingVideoFinished;
    }

    protected override void OnShow()
    {
        ResetLoadingVisual();

        isKeyboardInputReady = false;

        if (keyboardInputReadyCoroutine != null)
        {
            StopCoroutine(keyboardInputReadyCoroutine);
        }

        keyboardInputReadyCoroutine = StartCoroutine(EnableKeyboardInputNextFrame());

        RefreshContinueButton();
        SetupBackButton();
    }

    protected override void OnHide()
    {
        isKeyboardInputReady = false;

        if (keyboardInputReadyCoroutine != null)
        {
            StopCoroutine(keyboardInputReadyCoroutine);
            keyboardInputReadyCoroutine = null;
        }

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
        EventBus<UIChapterTitleCardLoadingReadyEvent>.action += HandleLoadingReady;
        EventBus<UIChapterTitleCardInputSkipRequestedEvent>.action += HandleSkipRequested;
    }

    private void UnsubscribeEvents()
    {
        EventBus<UISetChapterTitleCardEvent>.action -= HandleSetChapterTitleCard;
        EventBus<UIChapterTitleCardInputContinueRequestedEvent>.action -= HandleInputContinueRequested;
        EventBus<UIChapterTitleCardLoadingReadyEvent>.action -= HandleLoadingReady;
        EventBus<UIChapterTitleCardInputSkipRequestedEvent>.action -= HandleSkipRequested;
    }

    private IEnumerator EnableKeyboardInputNextFrame()
    {
        yield return null;

        isKeyboardInputReady = true;
        keyboardInputReadyCoroutine = null;
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
        if (!IsVisible) return;

        if (!isKeyboardInputReady) return;

        if (loadingTransitionState == LoadingTransitionState.Idle)
        {
            HandleContinueClicked();
            return;
        }

        if (loadingTransitionState == LoadingTransitionState.WaitingForProceed)
        {
            HandleProceedRequested();
        }
    }

    private void HandleBackClicked()
    {
        EventBus<UIChapterSelectScrollPreserveRequestedEvent>.Publish(default);
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
        if (currentChapterId < 0 || isTransitioning || loadingTransitionState != LoadingTransitionState.Idle) return;

        isTransitioning = true;
        loadingTransitionState = LoadingTransitionState.ZoomingOut;

        if (continueButton != null)
            continueButton.SetInteractable(false);

        if (backButton != null)
            backButton.SetInteractable(false);

        transitionCoroutine = StartCoroutine(PlayLoadingTransition());
    }

    private IEnumerator PlayLoadingTransition()
    {
        RectTransform titleCardRect = thumbnailImage.rectTransform;

        bool fadeOutCompleted = false;

        EventBus<UIFadeEvent>.Publish(
            new UIFadeEvent(
                0f,
                1f,
                zoomFadeOutDuration,
                () => fadeOutCompleted = true));

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

        while (!fadeOutCompleted)
        {
            yield return null;
        }

        SetCardDetailObjectsActive(false);

        yield return StartCoroutine(StartLoadingVisualAfterVideoStarts());

        loadingTransitionState = LoadingTransitionState.VideoPlaying;

        EventBus<UIChapterTitleCardContinueRequestedEvent>.Publish(
            new UIChapterTitleCardContinueRequestedEvent(currentChapterId));

        EventBus<UIFadeEvent>.Publish(
            new UIFadeEvent(
                1f,
                0f,
                loadingVideoFadeInDuration));

        transitionCoroutine = null;
    }

    private IEnumerator StartLoadingVisualAfterVideoStarts()
    {
        isLoadingReady = false;
        isVideoFinished = false;
        isFinishingVideo = false;

        if (loadingSkipGuideObject != null)
            loadingSkipGuideObject.SetActive(false);

        if (proceedGuideObject != null)
            proceedGuideObject.SetActive(false);

        isLoadingVisualActive = true;

        if (loadingSpinner != null)
        {
            loadingSpinner.localRotation = Quaternion.identity;
            loadingSpinner.gameObject.SetActive(true);
        }

        if (currentLoadingVideoClip == null || loadingVideoPlayer == null || loadingVideoImage == null)
        {
            isVideoFinished = true;
            yield break;
        }

        loadingVideoImage.gameObject.SetActive(true);

        loadingVideoPlayer.Stop();
        loadingVideoPlayer.clip = currentLoadingVideoClip;
        loadingVideoPlayer.isLooping = false;
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

        loadingTransitionState = LoadingTransitionState.Idle;
        isLoadingReady = false;
        isVideoFinished = false;
        isFinishingVideo = false;

        if (loadingSkipGuideObject != null)
            loadingSkipGuideObject.SetActive(false);

        if (proceedGuideObject != null)
            proceedGuideObject.SetActive(false);
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

    private void HandleLoadingVideoFinished(VideoPlayer source)
    {
        isVideoFinished = true;
        TryFinishVideoStage();
    }

    private void HandleLoadingReady(UIChapterTitleCardLoadingReadyEvent eventData)
    {
        if (eventData.ChapterId != currentChapterId || loadingTransitionState != LoadingTransitionState.VideoPlaying) return;

        isLoadingReady = true;

        if (!isVideoFinished && loadingSkipGuideObject != null)
        {
            loadingSkipGuideObject.SetActive(true);
        }

        TryFinishVideoStage();
    }

    private void HandleSkipRequested(UIChapterTitleCardInputSkipRequestedEvent eventData)
    {
        if (loadingTransitionState != LoadingTransitionState.VideoPlaying || !isLoadingReady || isVideoFinished) return;

        if (loadingVideoPlayer != null)
        {
            loadingVideoPlayer.Pause();
        }

        isVideoFinished = true;
        TryFinishVideoStage();
    }

    private void TryFinishVideoStage()
    {
        if (!isVideoFinished || !isLoadingReady || isFinishingVideo) return;

        StartCoroutine(FinishVideoStage());
    }

    private IEnumerator FinishVideoStage()
    {
        isFinishingVideo = true;

        if (loadingSkipGuideObject != null)
            loadingSkipGuideObject.SetActive(false);

        bool fadeOutCompleted = false;

        EventBus<UIFadeEvent>.Publish(
            new UIFadeEvent(
                0f,
                1f,
                videoEndFadeOutDuration,
                () => fadeOutCompleted = true));

        while (!fadeOutCompleted)
        {
            yield return null;
        }

        isLoadingVisualActive = false;

        if (loadingVideoPlayer != null)
            loadingVideoPlayer.Stop();

        if (loadingVideoImage != null)
            loadingVideoImage.gameObject.SetActive(false);

        if (loadingSpinner != null)
        {
            loadingSpinner.localRotation = Quaternion.identity;
            loadingSpinner.gameObject.SetActive(false);
        }

        if (proceedGuideObject != null)
            proceedGuideObject.SetActive(true);

        loadingTransitionState = LoadingTransitionState.WaitingForProceed;
        isFinishingVideo = false;
    }

    private void HandleProceedRequested()
    {
        if (loadingTransitionState != LoadingTransitionState.WaitingForProceed) return;

        if (proceedGuideObject != null)
            proceedGuideObject.SetActive(false);

        EventBus<UIChapterTitleCardActivateSceneRequestedEvent>.Publish(
            new UIChapterTitleCardActivateSceneRequestedEvent(currentChapterId));
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
