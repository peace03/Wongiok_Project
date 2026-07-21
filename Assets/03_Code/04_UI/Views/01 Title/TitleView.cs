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

    // 구독 및 구독 해제
    protected override void Awake()
    {
        base.Awake();
        SubscribeEvents();
        SubscribeVideoEvent();
    }

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
            newGameButton.Setup("New", HandleNewGameClicked, isTitleInputReady);
        }

        if (continueButton != null)
        {
            continueButton.Setup("Continue", HandleContinueClicked, isTitleInputReady && hasSaveFile);
        }

        if (exitButton != null)
        {
            exitButton.Setup("Quit", HandleExitClicked, isTitleInputReady);
        }
    }

    private void SubscribeEvents()
    {
        EventBus<UISetTitleSaveStateEvent>.action += HandleSetTitleSaveState;
    }

    private void UnsubscribeEvents()
    {
        EventBus<UISetTitleSaveStateEvent>.action -= HandleSetTitleSaveState;
    }

    private void SubscribeVideoEvent()
    {
        if (videoPlayer != null)
            videoPlayer.loopPointReached += HandleVideoFinished;
    }

    private void UnsubscribeVideoEvent()
    {
        if (videoPlayer != null)
            videoPlayer.loopPointReached -= HandleVideoFinished;
    }

    private void HandleSetTitleSaveState(UISetTitleSaveStateEvent eventData)
    {
        SetSaveFileAvailable(eventData.HasSaveFile);
    }

    private void HandleNewGameClicked()
    {
        if (hasSaveFile)
        {
            EventBus<UIShowConfirmPopupEvent>.Publish(
                new UIShowConfirmPopupEvent(
                    "새 게임 시작",
                    "이전 데이터를 파기하고 새로 시작하시겠습니까?",
                    BeginNewGameStartVideo));

            return;
        }

        BeginNewGameStartVideo();
    }

    private void HandleContinueClicked()
    {
        if (!hasSaveFile)
            return;

        BeginContinueStartVideo();
    }

    private void HandleExitClicked()
    {
        EventBus<UIShowConfirmPopupEvent>.Publish(
            new UIShowConfirmPopupEvent(
                "게임 종료",
                "정말 게임 종료?",
                PublishExitRequested));
    }

    private void BeginNewGameStartVideo()
    {
        PlayTitleStartVideo(PendingTitleRequest.NewGame);
    }

    private void BeginContinueStartVideo()
    {
        PlayTitleStartVideo(PendingTitleRequest.Continue);
    }

    private void PlayTitleEnterVideo()
    {
        StopButtonRevealCoroutine();
        HideTitleButtons();

        titleVideoState = TitleVidoeState.Enter;
        PlayVideo(titleEnterVideoClip, false);
    }

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
        videoPlayer.Play();

        if (videoImage != null)
            videoImage.enabled = true;
    }

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

    private void StopTitleVideo()
    {
        if (videoPlayer != null)
            videoPlayer.Stop();

        if (videoImage != null)
            videoImage.enabled = false;

        titleVideoState = TitleVidoeState.None;
    }

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

    private void HideTitleButtons()
    {
        HideButton(newGameButtonCanvasGroup);
        HideButton(continueButtonCanvasGroup);
        HideButton(exitButtonCanvasGroup);
    }

    private void HideButton(CanvasGroup canvasGroup)
    {
        if (canvasGroup == null) return;

        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
    }
    
    private void StopButtonRevealCoroutine()
    {
        if (buttonRevealCoroutine == null) return;

        StopCoroutine(buttonRevealCoroutine);
        buttonRevealCoroutine = null;
    }

    private void PublishExitRequested()
    {
        EventBus<UITitleExitRequestedEvent>.Publish(default);
    }

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
}
