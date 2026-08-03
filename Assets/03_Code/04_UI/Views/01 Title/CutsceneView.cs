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
            FinishedCutscene(true);
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
        videoPlayer.Play();

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
            videoPlayer.loopPointReached += HandleVideoFinished;
    }

    private void UnsubscribeVideoEvent()
    {
        if (videoPlayer != null)
            videoPlayer.loopPointReached -= HandleVideoFinished;
    }
}
