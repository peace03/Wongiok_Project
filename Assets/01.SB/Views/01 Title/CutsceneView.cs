using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

// 영상 컷씬 오버레이 전체 담당 View
public class CutsceneView : UIViewBase
{
    [Header("Video")]
    // 컷씬 영상 재생 비디오 플레이어
    [SerializeField] private VideoPlayer videoPlayer;
    // 비디오 플레이어 출력 렌더 텍스쳐를 보여주는 로우 이미지
    [SerializeField] private RawImage videoImage;

    [Header("Skip Guide")]
    // ESC 스킵 안내 문구
    [SerializeField] private GameObject skipGuideObject;
    // 스킵 안내 텍스트
    [SerializeField] private Text skipGuideText;

    private string currentCutsceneId;
    private VideoClip currentVideoClip;
    private string currentSkipSummary;

    private bool isPlayingCutscene;
    // 완료 이벤트 중복 발행 방지
    private bool isFinished;
    // 스킵 확인 팝업 중복 표시 방지
    private bool isSkipPopupOpen;

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

    protected override void OnShow()
    {
        RefreshSkipGuide();
        PlayCurrentCutscene();
    }

    protected override void OnHide()
    {
        StopCurrentCutscene();
        isSkipPopupOpen = false;
    }

    private void SubscribeEvents()
    {
        EventBus<UISetCutsceneEvent>.action += HandleSetCutscene;
        EventBus<UICutsceneSkipRequestedEvent>.action += HandleSkipRequested;
        EventBus<UIResetEvent>.action += HandleReset;
    }

    private void UnsubscribeEvents()
    {
        EventBus<UISetCutsceneEvent>.action -= HandleSetCutscene;
        EventBus<UICutsceneSkipRequestedEvent>.action -= HandleSkipRequested;
        EventBus<UIResetEvent>.action -= HandleReset;
    }

    private void SubscribeVideoEvent()
    {
        if (videoPlayer == null)
            return;

        videoPlayer.loopPointReached += HandleVideoFinished;
    }

    private void UnsubscribeVideoEvent()
    {
        if (videoPlayer == null)
            return;

        videoPlayer.loopPointReached -= HandleVideoFinished;
    }

    private void HandleSetCutscene(UISetCutsceneEvent eventData)
    {
        currentCutsceneId = eventData.CutsceneId;
        currentVideoClip = eventData.VideoClip;
        currentSkipSummary = eventData.SkipSummary;

        isFinished = false;
        isSkipPopupOpen = false;

        if (IsVisible)
        {
            RefreshSkipGuide();
            PlayCurrentCutscene();
        }
    }

    private void HandleSkipRequested(UICutsceneSkipRequestedEvent eventData)
    {
        if (!IsVisible)
            return;

        if (!isPlayingCutscene)
            return;

        if (isFinished)
            return;

        if (isSkipPopupOpen)
            return;

        PauseCurrentCutscene();
        ShowSkipConfirmPopup();
    }

    private void HandleReset(UIResetEvent eventData)
    {
        StopCurrentCutscene();

        currentCutsceneId = string.Empty;
        currentVideoClip = null;
        currentSkipSummary = string.Empty;

        isFinished = false;
        isSkipPopupOpen = false;
    }

    private void RefreshSkipGuide()
    {
        if (skipGuideObject != null)
            skipGuideObject.SetActive(true);

        SetText(skipGuideText, "ESC: Skip");
    }

    // 현재 저장된 VideoClip을 비디오 플레이어에 넣고 재생
    private void PlayCurrentCutscene()
    {
        if (videoPlayer == null)
            return;

        if (currentVideoClip == null)
        {
            Debug.Log("UI: 재생할 VideoClip이 없음");
            return;
        }

        videoPlayer.Stop();
        videoPlayer.clip = currentVideoClip;
        videoPlayer.isLooping = false;
        videoPlayer.Play();

        if (videoImage != null)
        {
            videoImage.enabled = true;
        }

        isPlayingCutscene = true;
        isFinished = false;
    }

    // 컷씬 일시정지
    private void PauseCurrentCutscene()
    {
        if (videoPlayer != null && videoPlayer.isPlaying)
        {
            videoPlayer.Pause();
        }
    }

    // 일시정지 컷씬 이어서 재생
    private void ResumeCurrentCutscene()
    {
        if (videoPlayer != null && !isFinished)
        {
            videoPlayer.Play();
        }

        isPlayingCutscene = true;
    }

    // 컷씬 영상 완전 정지
    private void StopCurrentCutscene()
    {
        if (videoPlayer != null)
            videoPlayer.Stop();

        isPlayingCutscene = false;

        if (videoImage != null)
            videoImage.enabled = false;
    }

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

    private void HandleVideoFinished(VideoPlayer source)
    {
        FinishedCutscene(false);
    }

    private void FinishedCutscene(bool wasSkipped)
    {
        if (isFinished)
            return;

        isFinished = true;
        StopCurrentCutscene();

        EventBus<UICutsceneFinishedEvent>.Publish(
            new UICutsceneFinishedEvent(currentCutsceneId, wasSkipped));
    }

    private void SetText(Text targetText, string value)
    {
        if (targetText == null)
            return;

        targetText.text = value;
    }
}
