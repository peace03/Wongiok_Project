using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

public class TitleView : UIViewBase
{
    [Header("Buttons")]
    [SerializeField] private CommonButtonView newGameButton;
    [SerializeField] private CommonButtonView continueButton;
    [SerializeField] private CommonButtonView exitButton;

    [Header("버튼 연출")]
    [SerializeField] private CanvasGroup newGameButtonCanvasGroup;
    [SerializeField] private CanvasGroup continueButtonCanvasGroup;
    [SerializeField] private CanvasGroup exitButtonCanvasGroup;

    [SerializeField, Min(0f)] private float newGameRevealDelay = 0f;
    [SerializeField, Min(0f)] private float continueRevealDelay = 0.15f;
    [SerializeField, Min(0f)] private float exitRevealDelay = 0.15f;
    [SerializeField, Min(0f)] private float buttonRevealDuration = 0.3f;

    private Coroutine buttonRevealCoroutine;

    [Header("Video")]
    [SerializeField] private VideoPlayer videoPlayer;
    [SerializeField] private RawImage videoImage;
    [SerializeField] private VideoClip titleEnterVideoClip;
    [SerializeField] private VideoClip titleWaitingVideoClip;
    [SerializeField] private VideoClip titleStartVideoClip;

    private enum TitleVidoeState
    {
        None,
        Enter,
        Waiting,
        Start
    }

    private enum PendingTitleRequest
    {
        None,
        NewGame,
        Continue
    }

    // 저장 파일이 존재하는지 여부
    // 실제 파일 검사는 다른 쪽에서 처리하고 전달 받은 상태만 적용
    private bool hasSaveFile;
    private bool isTitleInputReady;
    private TitleVidoeState titleVideoState;
    private PendingTitleRequest pendingTitleRequest;
    // 2026.08.07_psb수정
    // 타이틀 입장 영상의 첫 프레임이 준비되기 전에는 전환 페이드를 걷지 않도록 상태를 보관한다.
    private bool isWaitingForFirstVideoFrame;

    // 구독 및 구독 해제
    protected override void Awake()
    {
        base.Awake();
        SubscribeEvents();
        SubscribeVideoEvent();
    }

    // 2026.08.10_UI 정리: 파괴 시 등록한 이벤트와 임시 UI 상태를 정리한다.
    private void OnDestroy()
    {
        UnsubscribeEvents();
        UnsubscribeVideoEvent();
    }

    // 상황에 맞는 버튼 기능 동작
    protected override void OnShow()
    {
        pendingTitleRequest = PendingTitleRequest.None;
        isTitleInputReady = false;

        PlayTitleEnterVideo();
        RefreshButtons();
        HideTitleButtons();
    }

    // 숨기면서 초기화
    protected override void OnHide()
    {
        StopButtonRevealCoroutine();
        HideTitleButtons();

        StopTitleVideo();
        ClearButtons();

        pendingTitleRequest = PendingTitleRequest.None;
        isTitleInputReady = false;
    }

    // 저장 파일이 존재하는지 외부에서 확인해 받음
    public void SetSaveFileAvailable(bool hasSaveFile)
    {
        this.hasSaveFile = hasSaveFile;
        RefreshButtons();
    }

    // if문을 사용해 상황별 기능 세팅
    public void RefreshButtons()
    {
        if (newGameButton != null)
        {
            newGameButton.Setup(UITextManager.Get("Title.NewGame"), HandleNewGameClicked, isTitleInputReady);
        }

        if (continueButton != null)
        {
            continueButton.Setup(UITextManager.Get("Title.Continue"), HandleContinueClicked, isTitleInputReady && hasSaveFile);
        }

        if (exitButton != null)
        {
            exitButton.Setup(UITextManager.Get("Title.Quit"), HandleExitClicked, isTitleInputReady);
        }
    }

    // 2026.08.10_UI 정리: 이벤트 이벤트를 구독한다.
    private void SubscribeEvents()
    {
        EventBus<UISetTitleSaveStateEvent>.action += HandleSetTitleSaveState;
    }

    // 2026.08.10_UI 정리: 이벤트 이벤트 구독을 해제한다.
    private void UnsubscribeEvents()
    {
        EventBus<UISetTitleSaveStateEvent>.action -= HandleSetTitleSaveState;
    }

    // 2026.08.10_UI 정리: 영상 이벤트 이벤트를 구독한다.
    private void SubscribeVideoEvent()
    {
        if (videoPlayer != null)
        {
            videoPlayer.loopPointReached += HandleVideoFinished;
            // 2026.08.07_psb수정
            // 디코더 준비 및 첫 프레임 출력 시점을 구독해 빈 영상 텍스처 노출을 막는다.
            videoPlayer.prepareCompleted += HandleVideoPrepared;
            videoPlayer.frameReady += HandleVideoFrameReady;
            videoPlayer.sendFrameReadyEvents = true;
        }
    }

    // 2026.08.10_UI 정리: 영상 이벤트 이벤트 구독을 해제한다.
    private void UnsubscribeVideoEvent()
    {
        if (videoPlayer != null)
        {
            videoPlayer.loopPointReached -= HandleVideoFinished;
            videoPlayer.prepareCompleted -= HandleVideoPrepared;
            videoPlayer.frameReady -= HandleVideoFrameReady;
        }
    }

    // 2026.08.10_UI 정리: Set 타이틀 저장 상태 관련 입력 또는 EventBus 요청을 처리한다.
    private void HandleSetTitleSaveState(UISetTitleSaveStateEvent eventData)
    {
        SetSaveFileAvailable(eventData.HasSaveFile);
    }

    // 2026.08.10_UI 정리: New 게임 클릭 관련 입력 또는 EventBus 요청을 처리한다.
    private void HandleNewGameClicked()
    {
        if (hasSaveFile)
        {
            EventBus<UIShowConfirmPopupEvent>.Publish(
                new UIShowConfirmPopupEvent(
                    UITextManager.Get("Title.NewGameConfirmTitle"),
                    UITextManager.Get("Title.NewGameConfirmMessage"),
                    BeginNewGameStartVideo));

            return;
        }

        BeginNewGameStartVideo();
    }

    // 2026.08.10_UI 정리: 계속 클릭 관련 입력 또는 EventBus 요청을 처리한다.
    private void HandleContinueClicked()
    {
        if (!hasSaveFile)
            return;

        BeginContinueStartVideo();
    }

    // 2026.08.10_UI 정리: 종료 클릭 관련 입력 또는 EventBus 요청을 처리한다.
    private void HandleExitClicked()
    {
        EventBus<UIShowConfirmPopupEvent>.Publish(
            new UIShowConfirmPopupEvent(
                UITextManager.Get("Title.QuitConfirmTitle"),
                UITextManager.Get("Title.QuitConfirmMessage"),
                PublishExitRequested));
    }

    // 2026.08.10_UI 정리: 이 메서드의 UI 처리 역할을 수행한다.
    private void BeginNewGameStartVideo()
    {
        PlayTitleStartVideo(PendingTitleRequest.NewGame);
    }

    // 2026.08.10_UI 정리: 이 메서드의 UI 처리 역할을 수행한다.
    private void BeginContinueStartVideo()
    {
        PlayTitleStartVideo(PendingTitleRequest.Continue);
    }

    // 2026.08.10_UI 정리: 타이틀 입장 영상 UI 연출을 재생한다.
    private void PlayTitleEnterVideo()
    {
        StopButtonRevealCoroutine();
        HideTitleButtons();

        titleVideoState = TitleVidoeState.Enter;
        PlayVideo(titleEnterVideoClip, false);
    }

    // 2026.08.10_UI 정리: 타이틀 대기 영상 UI 연출을 재생한다.
    private void PlayTitleWaitingVideo()
    {
        titleVideoState = TitleVidoeState.Waiting;

        isTitleInputReady = false;
        RefreshButtons();

        StopButtonRevealCoroutine();
        HideTitleButtons();

        PlayVideo(titleWaitingVideoClip, true);

        buttonRevealCoroutine = StartCoroutine(RevealButtonsSequentially());
    }

    // 2026.08.10_UI 정리: 타이틀 Start 영상 UI 연출을 재생한다.
    private void PlayTitleStartVideo(PendingTitleRequest request)
    {
        StopButtonRevealCoroutine();
        HideTitleButtons();

        pendingTitleRequest = request;
        titleVideoState = TitleVidoeState.Start;
        isTitleInputReady = false;

        RefreshButtons();
        PlayVideo(titleStartVideoClip, false);
    }

    // 2026.08.10_UI 정리: 영상 UI 연출을 재생한다.
    private void PlayVideo(VideoClip videoClip, bool isLooping)
    {
        if (videoPlayer == null || videoClip == null)
        {
            HandleMissingVideoClip();
            return;
        }

        videoPlayer.Stop();
        videoPlayer.clip = videoClip;
        videoPlayer.isLooping = isLooping;
        // 2026.08.07_psb수정
        // 첫 프레임을 실제로 출력한 뒤에만 외부 전환 페이드가 시작될 수 있도록 Prepare를 사용한다.
        isWaitingForFirstVideoFrame = true;
        videoPlayer.Prepare();

        if (videoImage != null)
            videoImage.enabled = true;
    }

    // 2026.08.10_UI 정리: 누락 영상 Clip 관련 입력 또는 EventBus 요청을 처리한다.
    private void HandleMissingVideoClip()
    {
        if (titleVideoState == TitleVidoeState.Enter)
        {
            PlayTitleWaitingVideo();
            return;
        }

        if (titleVideoState == TitleVidoeState.Start)
        {
            CompleteTitleStartVideo();
        }
    }

    // 2026.08.10_UI 정리: 영상 Finished 관련 입력 또는 EventBus 요청을 처리한다.
    private void HandleVideoFinished(VideoPlayer source)
    {
        if (titleVideoState == TitleVidoeState.Enter)
        {
            PlayTitleWaitingVideo();
            return;
        }

        if (titleVideoState == TitleVidoeState.Start)
        {
            CompleteTitleStartVideo();
        }    
    }

    // 2026.08.10_UI 정리: 타이틀 Start 영상 UI 전환을 완료 처리한다.
    private void CompleteTitleStartVideo()
    {
        PendingTitleRequest request = pendingTitleRequest;

        pendingTitleRequest = PendingTitleRequest.None;
        titleVideoState = TitleVidoeState.None;

        if (request == PendingTitleRequest.NewGame)
        {
            EventBus<UITitleNewGameRequestedEvent>.Publish(default);
            return;
        }

        if (request == PendingTitleRequest.Continue)
        {
            EventBus<UITitleContinueRequestedEvent>.Publish(default);
        }
    }

    // 2026.08.10_UI 정리: 진행 중인 타이틀 영상 UI 연출을 중지한다.
    private void StopTitleVideo()
    {
        if (videoPlayer != null)
            videoPlayer.Stop();

        // 2026.08.07_psb수정
        // View가 닫힌 뒤 늦게 도착한 준비 완료 콜백을 무시한다.
        isWaitingForFirstVideoFrame = false;

        if (videoImage != null)
            videoImage.enabled = false;

        titleVideoState = TitleVidoeState.None;
    }

    // 2026.08.10_UI 정리: 이 메서드의 UI 처리 역할을 수행한다.
    private IEnumerator RevealButtonsSequentially()
    {
        yield return RevealButton(
            newGameButton,
            newGameButtonCanvasGroup,
            newGameRevealDelay,
            true);

        yield return RevealButton(
            continueButton,
            continueButtonCanvasGroup,
            continueRevealDelay,
            hasSaveFile);

        yield return RevealButton(
            exitButton,
            exitButtonCanvasGroup,
            exitRevealDelay,
            true);

        isTitleInputReady = true;
        buttonRevealCoroutine = null;
    }

    // 2026.08.10_UI 정리: 이 메서드의 UI 처리 역할을 수행한다.
    private IEnumerator RevealButton(CommonButtonView button, CanvasGroup canvasGroup, float delay, bool isInteractable)
    {
        if (canvasGroup == null) yield break;

        if (delay > 0f) yield return new WaitForSeconds(delay);

        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        float targetAlpha = isInteractable ? 1f : 0.45f;
        float elapsed = 0f;

        while (elapsed < buttonRevealDuration)
        {
            elapsed += Time.deltaTime;

            float progress = buttonRevealDuration <= 0f
                ? 1f
                : Mathf.Clamp01(elapsed / buttonRevealDuration);

            canvasGroup.alpha = Mathf.Lerp(0f, targetAlpha, progress);

            yield return null;
        }

        canvasGroup.alpha = targetAlpha;
        canvasGroup.interactable = isInteractable;
        canvasGroup.blocksRaycasts = isInteractable;

        button.SetInteractable(isInteractable);
    }

    // 2026.08.10_UI 정리: 타이틀 Buttons UI 요소를 숨긴다.
    private void HideTitleButtons()
    {
        HideButton(newGameButtonCanvasGroup);
        HideButton(continueButtonCanvasGroup);
        HideButton(exitButtonCanvasGroup);
    }

    // 2026.08.10_UI 정리: 버튼 UI 요소를 숨긴다.
    private void HideButton(CanvasGroup canvasGroup)
    {
        if (canvasGroup == null) return;

        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
    }
    
    // 2026.08.10_UI 정리: 진행 중인 버튼 Reveal Coroutine UI 연출을 중지한다.
    private void StopButtonRevealCoroutine()
    {
        if (buttonRevealCoroutine == null) return;

        StopCoroutine(buttonRevealCoroutine);
        buttonRevealCoroutine = null;
    }

    // 2026.08.10_UI 정리: 종료 Requested 요청 또는 상태를 EventBus로 발행한다.
    private void PublishExitRequested()
    {
        EventBus<UITitleExitRequestedEvent>.Publish(default);
    }

    // 2026.08.10_UI 정리: Buttons 상태를 정리한다.
    private void ClearButtons()
    {
        if (newGameButton != null)
        {
            newGameButton.Clear();
        }

        if (continueButton != null)
        {
            continueButton.Clear();
        }

        if (exitButton != null)
        {
            exitButton.Clear(); 
        }
    }

    // 2026.08.07_psb수정
    // VideoPlayer 준비가 끝난 시점에 재생을 시작한다.
    private void HandleVideoPrepared(VideoPlayer source)
    {
        if (!isWaitingForFirstVideoFrame || source != videoPlayer)
            return;

        source.Play();
    }

    // 2026.08.07_psb수정
    // 타이틀 입장 영상의 첫 프레임이 준비되면 전환 담당자에게 페이드 인 가능 상태를 알린다.
    private void HandleVideoFrameReady(VideoPlayer source, long frameIndex)
    {
        if (!isWaitingForFirstVideoFrame || source != videoPlayer)
            return;

        isWaitingForFirstVideoFrame = false;

        if (titleVideoState != TitleVidoeState.Enter)
            return;

        EventBus<UIVideoFirstFrameReadyEvent>.Publish(
            new UIVideoFirstFrameReadyEvent("title-enter"));
    }
}
