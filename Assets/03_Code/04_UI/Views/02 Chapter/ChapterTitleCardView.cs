using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using System.Collections;

public class ChapterTitleCardView : UIViewBase
{
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

    // 2026.08.10_UI 정리: 컴포넌트 초기화와 이벤트 구독을 준비한다.
    protected override void Awake()
    {
        base.Awake();

        CaptureInitialVisualState();
        SubscribeEvents();

        if (loadingVideoPlayer != null)
            loadingVideoPlayer.loopPointReached += HandleLoadingVideoFinished;
    }

    // 2026.08.10_UI 정리: 파괴 시 등록한 이벤트와 임시 UI 상태를 정리한다.
    private void OnDestroy()
    {
        UnsubscribeEvents();

        if (loadingVideoPlayer != null)
            loadingVideoPlayer.loopPointReached -= HandleLoadingVideoFinished;
    }

    // 2026.08.10_UI 정리: 화면 표시 시 필요한 UI 상태를 초기화한다.
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

    // 2026.08.10_UI 정리: 화면 숨김 시 임시 UI 상태를 정리한다.
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

    // 2026.08.10_UI 정리: 프레임 단위 UI 상태와 입력을 갱신한다.
    private void Update()
    {
        if (!isLoadingVisualActive || loadingSpinner == null) return;

        loadingSpinner.Rotate(0f, 0f, -loadingSpinnerSpeed * Time.unscaledDeltaTime);
    }

    // 2026.08.10_UI 정리: 전달받은 데이터와 콜백으로 UI 상태 상태를 설정한다.
    public void Setup(
        int chapterId,
        Sprite thumbnail,
        Sprite background,
        VideoClip loadingVideoClip)
    {
        currentChapterId = chapterId;
        currentThumbnail = thumbnail;
        currentBackground = background;
        currentLoadingVideoClip = loadingVideoClip;

        SetThumbnail(thumbnail);
        SetBackground(background);

        RefreshContinueButton();
    }

    // 2026.08.10_UI 정리: UI 상태 상태를 정리한다.
    public void Clear()
    {
        currentChapterId = -1;
        currentThumbnail = null;
        currentBackground = null;

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

    // 2026.08.10_UI 정리: 이벤트 이벤트를 구독한다.
    private void SubscribeEvents()
    {
        EventBus<UISetChapterTitleCardEvent>.action += HandleSetChapterTitleCard;
        EventBus<UIChapterTitleCardInputContinueRequestedEvent>.action += HandleInputContinueRequested;
        EventBus<UIChapterTitleCardLoadingReadyEvent>.action += HandleLoadingReady;
        EventBus<UIChapterTitleCardInputSkipRequestedEvent>.action += HandleSkipRequested;
    }

    // 2026.08.10_UI 정리: 이벤트 이벤트 구독을 해제한다.
    private void UnsubscribeEvents()
    {
        EventBus<UISetChapterTitleCardEvent>.action -= HandleSetChapterTitleCard;
        EventBus<UIChapterTitleCardInputContinueRequestedEvent>.action -= HandleInputContinueRequested;
        EventBus<UIChapterTitleCardLoadingReadyEvent>.action -= HandleLoadingReady;
        EventBus<UIChapterTitleCardInputSkipRequestedEvent>.action -= HandleSkipRequested;
    }

    // 2026.08.10_UI 정리: 키보드 입력 다음 프레임 UI 입력 또는 표시를 활성화한다.
    private IEnumerator EnableKeyboardInputNextFrame()
    {
        yield return null;

        isKeyboardInputReady = true;
        keyboardInputReadyCoroutine = null;
    }

    // 2026.08.10_UI 정리: Set 챕터 타이틀 카드 관련 입력 또는 EventBus 요청을 처리한다.
    private void HandleSetChapterTitleCard(UISetChapterTitleCardEvent eventData)
    {
        Setup(
            eventData.ChapterId,
            eventData.Thumbnail,
            eventData.Background,
            eventData.LoadingVideoClip);
    }

    // 2026.08.10_UI 정리: 입력 계속 Requested 관련 입력 또는 EventBus 요청을 처리한다.
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

    // 2026.08.10_UI 정리: 뒤로가기 클릭 관련 입력 또는 EventBus 요청을 처리한다.
    private void HandleBackClicked()
    {
        EventBus<UIChapterSelectScrollPreserveRequestedEvent>.Publish(default);
        EventBus<UIChapterTitleCardBackRequestedEvent>.Publish(default);
    }

    // 2026.08.10_UI 정리: 현재 데이터로 계속 버튼 표시를 갱신한다.
    private void RefreshContinueButton()
    {
        if (continueButton == null)
            return;

        continueButton.Setup(UITextManager.Get("Chapter.Continue"), HandleContinueClicked, currentChapterId >= 0);
    }

    // 2026.08.10_UI 정리: 전달받은 데이터와 콜백으로 뒤로가기 버튼 상태를 설정한다.
    private void SetupBackButton()
    {
        if (backButton != null)
        {
            backButton.Setup(UITextManager.Get("Chapter.BackToSelect"), HandleBackClicked);
        }
    }

    // 2026.08.10_UI 정리: 계속 클릭 관련 입력 또는 EventBus 요청을 처리한다.
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

    // 2026.08.10_UI 정리: 로딩 전환 UI 연출을 재생한다.
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

    // 2026.08.10_UI 정리: 이 메서드의 UI 처리 역할을 수행한다.
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

    // 2026.08.10_UI 정리: 현재 Initial 비주얼 상태 상태를 저장한다.
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

    // 2026.08.10_UI 정리: 로딩 비주얼 상태를 기본값으로 초기화한다.
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

    // 2026.08.10_UI 정리: 카드 상세 Objects 액티브 표시 값을 반영한다.
    private void SetCardDetailObjectsActive(bool isActive)
    {
        foreach (GameObject detailObject in cardDetailObjects)
        {
            if (detailObject != null)
                detailObject.SetActive(isActive);
        }
    }

    // 2026.08.10_UI 정리: 저장된 카드 상세 Objects 상태를 복원한다.
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

    // 2026.08.10_UI 정리: 로딩 영상 Finished 관련 입력 또는 EventBus 요청을 처리한다.
    private void HandleLoadingVideoFinished(VideoPlayer source)
    {
        isVideoFinished = true;
        TryFinishVideoStage();
    }

    // 2026.08.10_UI 정리: 로딩 준비 관련 입력 또는 EventBus 요청을 처리한다.
    private void HandleLoadingReady(UIChapterTitleCardLoadingReadyEvent eventData)
    {
        if (eventData.ChapterId != currentChapterId || loadingTransitionState != LoadingTransitionState.VideoPlaying) return;

        isLoadingReady = true;

        // 2026.08.07_psb수정
        // 실제 씬 로딩이 준비되면 대기 중임을 나타내던 스피너를 즉시 숨긴다.
        if (loadingSpinner != null)
            loadingSpinner.gameObject.SetActive(false);

        if (!isVideoFinished && loadingSkipGuideObject != null)
        {
            loadingSkipGuideObject.SetActive(true);
        }

        TryFinishVideoStage();
    }

    // 2026.08.10_UI 정리: Skip Requested 관련 입력 또는 EventBus 요청을 처리한다.
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

    // 2026.08.10_UI 정리: Finish 영상 Stage 동작을 시도한다.
    private void TryFinishVideoStage()
    {
        if (!isVideoFinished || !isLoadingReady || isFinishingVideo) return;

        StartCoroutine(FinishVideoStage());
    }

    // 2026.08.10_UI 정리: 이 메서드의 UI 처리 역할을 수행한다.
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

    // 2026.08.10_UI 정리: 진행 Requested 관련 입력 또는 EventBus 요청을 처리한다.
    private void HandleProceedRequested()
    {
        if (loadingTransitionState != LoadingTransitionState.WaitingForProceed) return;

        if (proceedGuideObject != null)
            proceedGuideObject.SetActive(false);

        EventBus<UIChapterTitleCardActivateSceneRequestedEvent>.Publish(
            new UIChapterTitleCardActivateSceneRequestedEvent(currentChapterId));
    }

    // 2026.08.10_UI 정리: 텍스트 표시 값을 반영한다.
    // 2026.08.10_UI 정리: 썸네일 표시 값을 반영한다.
    private void SetThumbnail(Sprite thumbnail)
    {
        if (thumbnailImage == null)
            return;

        thumbnailImage.sprite = thumbnail;
        thumbnailImage.enabled = thumbnail != null;

    }

    // 2026.08.10_UI 정리: 배경 표시 값을 반영한다.
    private void SetBackground(Sprite background)
    {
        if (backgroundImage == null)
            return;

        backgroundImage.sprite = background;
        backgroundImage.enabled = background != null;
    }
}
