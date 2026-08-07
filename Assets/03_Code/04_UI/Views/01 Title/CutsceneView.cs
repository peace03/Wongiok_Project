using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

// 프롤로그와 보스 영상 전환을 공통으로 표시하는 Cutscene Overlay입니다.
public class CutsceneView : UIViewBase
{
    private const string BossEncounterCutsceneId = "boss-encounter";
    private const string BossClearCutsceneId = "boss-clear";

    [Header("Video")]
    [SerializeField] private VideoPlayer videoPlayer;
    [SerializeField] private RawImage videoImage;

    [Header("Skip Guide")]
    [SerializeField] private GameObject skipGuideObject;
    [SerializeField] private Text skipGuideText;

    [Header("Boss Transition")]
    [SerializeField] private RectTransform loadingSpinner;
    [SerializeField] private GameObject proceedGuideObject;
    // 2026.08.07_psb수정
    // 프롤로그 스킵 시 영상 View를 숨기기 전에 검은 화면으로 전환하는 시간이다.
    [SerializeField, Min(0f)] private float prologueSkipFadeOutDuration = 0.35f;
    [SerializeField, Min(0f)] private float bossEncounterFadeOutDuration = 0.5f;
    [SerializeField, Min(0f)] private float bossClearEndFadeOutDuration = 0.5f;
    [SerializeField] private float loadingSpinnerSpeed = 180f;

    private string currentCutsceneId;
    private VideoClip currentVideoClip;
    private string currentSkipSummary;
    private CutscenePlaybackType currentPlaybackType;

    private bool isPlayingCutscene;
    private bool isFinished;
    private bool isSkipPopupOpen;
    private bool isEncounterLoadingReady;
    private bool isEncounterVideoFinished;
    private bool isWaitingForEncounterProceed;
    private bool isTransitionFinishing;
    private bool isBossClearSkipGuideShown;
    // 2026.08.07_psb수정
    // 페이드가 빈 RenderTexture를 드러내지 않도록 현재 영상의 첫 프레임 준비 여부를 추적한다.
    private bool isWaitingForFirstVideoFrame;

    protected override void Awake()
    {
        base.Awake();
        SubscribeEvents();
        SubscribeVideoEvent();
        ResetBossTransitionVisuals();
    }

    private void OnDestroy()
    {
        UnsubscribeEvents();
        UnsubscribeVideoEvent();
    }

    private void Update()
    {
        if (loadingSpinner == null || !loadingSpinner.gameObject.activeSelf)
            return;

        loadingSpinner.Rotate(
            0f,
            0f,
            -loadingSpinnerSpeed * Time.unscaledDeltaTime);
    }

    protected override void OnShow()
    {
        if (currentVideoClip == null)
        {
            SetSkipGuideVisible(false, string.Empty);
            return;
        }

        RefreshSkipGuide();
        PlayCurrentCutscene();
    }

    protected override void OnHide()
    {
        StopCurrentCutscene();
        ResetBossTransitionState();
    }

    // 영상 설정과 재생 유형을 받아 현재 열려 있는 Overlay에 반영합니다.
    private void HandleSetCutscene(UISetCutsceneEvent eventData)
    {
        currentCutsceneId = eventData.CutsceneId;
        currentVideoClip = eventData.VideoClip;
        currentSkipSummary = eventData.SkipSummary;
        currentPlaybackType = eventData.PlaybackType;

        isFinished = false;
        isSkipPopupOpen = false;
        ResetBossTransitionState();

        if (!IsVisible)
            return;

        RefreshSkipGuide();
        PlayCurrentCutscene();
    }

    // 입력 종류에 맞춰 일반 Cutscene과 보스 영상의 스킵 정책을 분기합니다.
    private void HandleSkipRequested(UICutsceneSkipRequestedEvent eventData)
    {
        if (!IsVisible || !isPlayingCutscene || isFinished || isSkipPopupOpen)
            return;

        if (currentPlaybackType == CutscenePlaybackType.BossEncounter)
        {
            if (eventData.Input != CutsceneSkipInput.BackQuote ||
                !isEncounterLoadingReady)
            {
                return;
            }

            isEncounterVideoFinished = true;
            TryEnterEncounterProceedState();
            return;
        }

        if (currentPlaybackType == CutscenePlaybackType.BossClear)
        {
            if (eventData.Input != CutsceneSkipInput.BackQuote)
                return;

            if (!isBossClearSkipGuideShown)
            {
                isBossClearSkipGuideShown = true;
                SetSkipGuideVisible(true, "~ : Skip");
                return;
            }

            FinishBossClearCutscene(true);
            return;
        }

        if (currentCutsceneId == "prologue")
        {
            // 2026.08.07_psb수정
            // 스킵 직후 VideoPlayer를 멈추지 않고, 검은 Fader가 덮인 뒤 종료한다.
            StartCoroutine(FinishPrologueAfterFadeOut());
            return;
        }

        if (eventData.Input != CutsceneSkipInput.Escape)
            return;

        PauseCurrentCutscene();
        ShowSkipConfirmPopup();
    }

    // 보스 씬 비동기 로딩이 끝난 뒤에만 조우 영상 스킵 안내를 활성화합니다.
    private void HandleBossEncounterLoadingReady(UIBossEncounterLoadingReadyEvent eventData)
    {
        if (!IsVisible || currentPlaybackType != CutscenePlaybackType.BossEncounter)
            return;

        isEncounterLoadingReady = true;

        // 2026.08.07_psb수정
        // 보스 씬 비동기 로딩이 끝난 뒤에는 회전 스피너를 더 이상 표시하지 않는다.
        if (loadingSpinner != null)
            loadingSpinner.gameObject.SetActive(false);

        SetSkipGuideVisible(true, "~ : Skip");
        TryEnterEncounterProceedState();
    }

    // 조우 영상 종료 후의 최종 Space 입력을 보스 씬 활성화 요청으로 바꿉니다.
    private void HandleProceedRequested(UICutsceneProceedRequestedEvent eventData)
    {
        if (!IsVisible || !isWaitingForEncounterProceed)
            return;

        isWaitingForEncounterProceed = false;

        if (proceedGuideObject != null)
            proceedGuideObject.SetActive(false);

        EventBus<UIBossEncounterActivateSceneRequestedEvent>.Publish(
            new UIBossEncounterActivateSceneRequestedEvent());
    }

    // 전체 UI Reset 시 재생 중인 영상과 전환 안내를 정리합니다.
    private void HandleReset(UIResetEvent eventData)
    {
        StopCurrentCutscene();

        currentCutsceneId = string.Empty;
        currentVideoClip = null;
        currentSkipSummary = string.Empty;
        currentPlaybackType = CutscenePlaybackType.Standard;

        isFinished = false;
        isSkipPopupOpen = false;
        ResetBossTransitionState();
    }

    // 현재 재생 유형에 맞는 초기 스킵 안내 상태를 적용합니다.
    private void RefreshSkipGuide()
    {
        if (currentPlaybackType == CutscenePlaybackType.BossEncounter ||
            currentPlaybackType == CutscenePlaybackType.BossClear)
        {
            SetSkipGuideVisible(false, string.Empty);
            return;
        }

        SetSkipGuideVisible(
            true,
            currentCutsceneId == "prologue" ? "~ : Skip" : "ESC : Skip");
    }

    // 전달된 VideoClip을 출력 RawImage에 재생합니다.
    private void PlayCurrentCutscene()
    {
        if (videoPlayer == null || currentVideoClip == null)
            return;

        videoPlayer.Stop();
        videoPlayer.clip = currentVideoClip;
        videoPlayer.isLooping = false;
        // 2026.08.07_psb수정
        // Play 직후가 아닌 첫 프레임 준비 후에만 전환 페이드를 걷도록 영상을 먼저 준비한다.
        isWaitingForFirstVideoFrame = true;
        videoPlayer.Prepare();

        if (videoImage != null)
            videoImage.enabled = true;

        if (currentPlaybackType == CutscenePlaybackType.BossEncounter &&
            loadingSpinner != null)
        {
            loadingSpinner.localRotation = Quaternion.identity;
            loadingSpinner.gameObject.SetActive(true);
        }

        isPlayingCutscene = true;
        isFinished = false;
    }

    // 일반 Cutscene의 확인 팝업 표시 중 영상 재생만 일시 정지합니다.
    private void PauseCurrentCutscene()
    {
        if (videoPlayer != null && videoPlayer.isPlaying)
            videoPlayer.Pause();
    }

    // 일반 Cutscene의 스킵 취소 시 같은 영상 위치에서 재생을 이어갑니다.
    private void ResumeCurrentCutscene()
    {
        if (videoPlayer != null && !isFinished)
            videoPlayer.Play();

        isPlayingCutscene = true;
    }

    // 현재 영상 출력과 재생 상태를 중지합니다.
    private void StopCurrentCutscene()
    {
        if (videoPlayer != null)
            videoPlayer.Stop();

        // 2026.08.07_psb수정
        // 숨김 또는 종료 뒤 늦게 도착한 프레임 준비 콜백은 무시한다.
        isWaitingForFirstVideoFrame = false;

        isPlayingCutscene = false;

        if (videoImage != null)
            videoImage.enabled = false;
    }

    // 일반 Cutscene 전용 스킵 확인 팝업을 표시합니다.
    private void ShowSkipConfirmPopup()
    {
        isSkipPopupOpen = true;

        EventBus<UIShowConfirmPopupEvent>.Publish(
            new UIShowConfirmPopupEvent(
                "건너뛰기",
                currentSkipSummary,
                HandleSkipConfirmed,
                HandleSkipCanceled,
                "확인",
                "취소"));
    }

    private void HandleSkipConfirmed()
    {
        isSkipPopupOpen = false;
        FinishedCutscene(true);
    }

    private void HandleSkipCanceled()
    {
        isSkipPopupOpen = false;
        ResumeCurrentCutscene();
    }

    // 2026.08.07_psb수정
    // 프롤로그 스킵 시 빈 RenderTexture가 드러나지 않도록 페이드 아웃 완료 후 영상을 종료한다.
    private IEnumerator FinishPrologueAfterFadeOut()
    {
        if (isTransitionFinishing || isFinished)
            yield break;

        isTransitionFinishing = true;
        SetSkipGuideVisible(false, string.Empty);

        bool isFadeOutFinished = false;

        EventBus<UIFadeEvent>.Publish(
            new UIFadeEvent(
                0f,
                1f,
                prologueSkipFadeOutDuration,
                () => isFadeOutFinished = true));

        yield return new WaitUntil(() => isFadeOutFinished);

        isTransitionFinishing = false;
        FinishedCutscene(true);
    }

    // VideoPlayer의 자연 종료를 재생 유형별 완료 단계로 전달합니다.
    private void HandleVideoFinished(VideoPlayer source)
    {
        if (currentPlaybackType == CutscenePlaybackType.BossEncounter)
        {
            isEncounterVideoFinished = true;
            TryEnterEncounterProceedState();
            return;
        }

        if (currentPlaybackType == CutscenePlaybackType.BossClear)
        {
            FinishBossClearCutscene(false);
            return;
        }

        FinishedCutscene(false);
    }

    // 영상 종료와 씬 로딩 완료가 모두 충족됐을 때 최종 Space 안내 단계로 전환합니다.
    private void TryEnterEncounterProceedState()
    {
        if (!isEncounterLoadingReady ||
            !isEncounterVideoFinished ||
            isTransitionFinishing ||
            isWaitingForEncounterProceed)
        {
            return;
        }

        isTransitionFinishing = true;
        SetSkipGuideVisible(false, string.Empty);

        EventBus<UIFadeEvent>.Publish(
            new UIFadeEvent(
                0f,
                1f,
                bossEncounterFadeOutDuration,
                () =>
                {
                    StopCurrentCutscene();
                    isTransitionFinishing = false;
                    isWaitingForEncounterProceed = true;

                    if (proceedGuideObject != null)
                        proceedGuideObject.SetActive(true);
                }));
    }

    // 보스 클리어 영상 종료 후 검은 화면으로 전환하고 진행 완료를 알립니다.
    private void FinishBossClearCutscene(bool wasSkipped)
    {
        if (isTransitionFinishing || isFinished)
            return;

        isTransitionFinishing = true;
        SetSkipGuideVisible(false, string.Empty);

        EventBus<UIFadeEvent>.Publish(
            new UIFadeEvent(
                0f,
                1f,
                bossClearEndFadeOutDuration,
                () =>
                {
                    isTransitionFinishing = false;
                    FinishedCutscene(wasSkipped);
                }));
    }

    // Cutscene 완료 이벤트를 한 번만 발행합니다.
    private void FinishedCutscene(bool wasSkipped)
    {
        if (isFinished)
            return;

        isFinished = true;
        StopCurrentCutscene();

        EventBus<UICutsceneFinishedEvent>.Publish(
            new UICutsceneFinishedEvent(currentCutsceneId, wasSkipped));
    }

    // 보스 조우 및 클리어 안내 요소를 초기 상태로 되돌립니다.
    private void ResetBossTransitionState()
    {
        isEncounterLoadingReady = false;
        isEncounterVideoFinished = false;
        isWaitingForEncounterProceed = false;
        isTransitionFinishing = false;
        isBossClearSkipGuideShown = false;
        ResetBossTransitionVisuals();
    }

    private void ResetBossTransitionVisuals()
    {
        if (loadingSpinner != null)
        {
            loadingSpinner.gameObject.SetActive(false);
            loadingSpinner.localRotation = Quaternion.identity;
        }

        if (proceedGuideObject != null)
            proceedGuideObject.SetActive(false);
    }

    // 보스 영상에서 사용하는 스킵 안내의 표시 상태와 문구를 갱신합니다.
    private void SetSkipGuideVisible(bool visible, string text)
    {
        if (skipGuideObject != null)
            skipGuideObject.SetActive(visible);

        if (skipGuideText != null)
            skipGuideText.text = text;
    }

    private void SubscribeEvents()
    {
        EventBus<UISetCutsceneEvent>.action += HandleSetCutscene;
        EventBus<UICutsceneSkipRequestedEvent>.action += HandleSkipRequested;
        EventBus<UIBossEncounterLoadingReadyEvent>.action += HandleBossEncounterLoadingReady;
        EventBus<UICutsceneProceedRequestedEvent>.action += HandleProceedRequested;
        EventBus<UIResetEvent>.action += HandleReset;
    }

    private void UnsubscribeEvents()
    {
        EventBus<UISetCutsceneEvent>.action -= HandleSetCutscene;
        EventBus<UICutsceneSkipRequestedEvent>.action -= HandleSkipRequested;
        EventBus<UIBossEncounterLoadingReadyEvent>.action -= HandleBossEncounterLoadingReady;
        EventBus<UICutsceneProceedRequestedEvent>.action -= HandleProceedRequested;
        EventBus<UIResetEvent>.action -= HandleReset;
    }

    private void SubscribeVideoEvent()
    {
        if (videoPlayer != null)
        {
            videoPlayer.loopPointReached += HandleVideoFinished;
            // 2026.08.07_psb수정
            // VideoPlayer가 실제로 출력할 첫 프레임을 받는 시점을 전환 완료 기준으로 사용한다.
            videoPlayer.prepareCompleted += HandleVideoPrepared;
            videoPlayer.frameReady += HandleVideoFrameReady;
            videoPlayer.sendFrameReadyEvents = true;
        }
    }

    private void UnsubscribeVideoEvent()
    {
        if (videoPlayer != null)
        {
            videoPlayer.loopPointReached -= HandleVideoFinished;
            videoPlayer.prepareCompleted -= HandleVideoPrepared;
            videoPlayer.frameReady -= HandleVideoFrameReady;
        }
    }

    // 2026.08.07_psb수정
    // 영상 디코더 준비가 끝난 뒤 재생을 시작해 첫 프레임 콜백을 받는다.
    private void HandleVideoPrepared(VideoPlayer source)
    {
        if (!isWaitingForFirstVideoFrame || source != videoPlayer)
            return;

        source.Play();
    }

    // 2026.08.07_psb수정
    // 첫 프레임이 RenderTexture에 준비된 시점에만 다음 화면 페이드 인을 허용한다.
    private void HandleVideoFrameReady(VideoPlayer source, long frameIndex)
    {
        if (!isWaitingForFirstVideoFrame || source != videoPlayer)
            return;

        isWaitingForFirstVideoFrame = false;

        EventBus<UIVideoFirstFrameReadyEvent>.Publish(
            new UIVideoFirstFrameReadyEvent(currentCutsceneId));
    }
}
