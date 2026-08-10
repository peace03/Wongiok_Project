using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Video;

// 보스 조우 씬 로딩과 보스 클리어 영상 시작 시점을 중계합니다.
public class BossCutsceneFlowController : MonoBehaviour
{
    private const string BossEncounterCutsceneId = "boss-encounter";
    private const string BossClearCutsceneId = "boss-clear";

    [Header("Boss Encounter")]
    [SerializeField] private string bossSceneName = "CinderellaBossScene_Y";
    [SerializeField] private VideoClip bossEncounterVideoClip;
    [SerializeField, Min(0f)] private float minimumEncounterLoadingPreviewTime = 3f;
    [SerializeField, Min(0f)] private float encounterFadeOutDuration = 0.5f;
    [SerializeField, Min(0f)] private float encounterFadeInDuration = 0.5f;

    [Header("Boss Clear")]
    [SerializeField] private VideoClip bossClearVideoClip;
    [SerializeField, Min(0f)] private float bossClearFadeOutDelay = 1f;
    [SerializeField, Min(0f)] private float bossClearFadeOutDuration = 0.5f;
    [SerializeField, Min(0f)] private float bossClearVideoFadeInDelay = 0.2f;
    [SerializeField, Min(0f)] private float bossClearFadeInDuration = 0.5f;

    private AsyncOperation pendingBossSceneLoadOperation;
    private bool isEncounterLoading;
    private bool isBossClearPlaying;
    // 2026.08.07_psb수정
    // 보스 영상의 첫 프레임이 RenderTexture에 준비될 때까지 Fader를 유지하기 위한 상태다.
    private bool isWaitingForEncounterFirstFrame;
    private bool isWaitingForBossClearFirstFrame;

    // 2026.08.10_UI 정리: 활성화 시 필요한 UI 상태와 이벤트 구독을 준비한다.
    private void OnEnable()
    {
        EventBus<UIBossEncounterRequestedEvent>.action += HandleBossEncounterRequested;
        EventBus<UIBossEncounterActivateSceneRequestedEvent>.action += HandleBossEncounterActivateSceneRequested;
        EventBus<BossDeathPresentationFinishedEvent>.action += HandleBossDeathPresentationFinished;
        EventBus<UICutsceneFinishedEvent>.action += HandleCutsceneFinished;
        // 2026.08.07_psb수정
        // CutsceneView가 알린 첫 프레임 준비 시점에만 영상 페이드 인을 시작한다.
        EventBus<UIVideoFirstFrameReadyEvent>.action += HandleVideoFirstFrameReady;
    }

    // 2026.08.10_UI 정리: 비활성화 시 등록한 이벤트와 임시 UI 상태를 정리한다.
    private void OnDisable()
    {
        EventBus<UIBossEncounterRequestedEvent>.action -= HandleBossEncounterRequested;
        EventBus<UIBossEncounterActivateSceneRequestedEvent>.action -= HandleBossEncounterActivateSceneRequested;
        EventBus<BossDeathPresentationFinishedEvent>.action -= HandleBossDeathPresentationFinished;
        EventBus<UICutsceneFinishedEvent>.action -= HandleCutsceneFinished;
        // 2026.08.07_psb수정
        // 영상 첫 프레임 준비 신호 구독을 해제한다.
        EventBus<UIVideoFirstFrameReadyEvent>.action -= HandleVideoFirstFrameReady;
    }

    // 포털 또는 임시 F6 입력이 요청한 보스 조우 연출을 시작합니다.
    private void HandleBossEncounterRequested(UIBossEncounterRequestedEvent eventData)
    {
        if (isEncounterLoading)
            return;

        if (bossEncounterVideoClip == null)
        {
            Debug.LogWarning("Boss encounter VideoClip is not assigned.", this);
            return;
        }

        StartCoroutine(LoadBossSceneWithEncounterCutscene());
    }

    // 최종 Space 입력이 들어왔을 때만 준비된 보스 씬을 활성화합니다.
    private void HandleBossEncounterActivateSceneRequested(
        UIBossEncounterActivateSceneRequestedEvent eventData)
    {
        if (!isEncounterLoading || pendingBossSceneLoadOperation == null)
            return;

        pendingBossSceneLoadOperation.allowSceneActivation = true;
    }

    // 보스 사망 연출이 끝난 시점에서만 클리어 영상을 시작합니다.
    private void HandleBossDeathPresentationFinished(
        BossDeathPresentationFinishedEvent eventData)
    {
        if (isBossClearPlaying)
            return;

        if (bossClearVideoClip == null)
        {
            Debug.LogWarning("Boss clear VideoClip is not assigned.", this);
            return;
        }

        StartCoroutine(PlayBossClearCutscene());
    }

    // 보스 클리어 영상의 자연 종료 또는 스킵 완료를 게임 진행 처리로 전달합니다.
    private void HandleCutsceneFinished(UICutsceneFinishedEvent eventData)
    {
        if (eventData.CutsceneId != BossClearCutsceneId)
            return;

        isBossClearPlaying = false;

        EventBus<UIBossClearVideoFinishedEvent>.Publish(
            new UIBossClearVideoFinishedEvent());

        EventBus<UIFadeEvent>.Publish(
            new UIFadeEvent(1f, 0f, bossClearFadeInDuration));
    }

    // 2026.08.07_psb수정
    // 보스 영상이 실제로 출력 가능한 첫 프레임을 받은 뒤에만 검은 Fader를 걷는다.
    private void HandleVideoFirstFrameReady(UIVideoFirstFrameReadyEvent eventData)
    {
        if (eventData.VideoId == BossEncounterCutsceneId &&
            isWaitingForEncounterFirstFrame)
        {
            isWaitingForEncounterFirstFrame = false;
            EventBus<UIFadeEvent>.Publish(
                new UIFadeEvent(1f, 0f, encounterFadeInDuration));
            return;
        }

        if (eventData.VideoId == BossClearCutsceneId &&
            isWaitingForBossClearFirstFrame)
        {
            isWaitingForBossClearFirstFrame = false;
            StartCoroutine(FadeInBossClearVideoAfterFirstFrame());
        }
    }

    // 조우 영상 재생과 보스 씬 비동기 로딩을 같은 전환 구간에서 시작합니다.
    private IEnumerator LoadBossSceneWithEncounterCutscene()
    {
        isEncounterLoading = true;

        EventBus<UIOpenOverlayEvent>.Publish(
            new UIOpenOverlayEvent(UIOverlayState.Cutscene));

        yield return Fade(0f, 1f, encounterFadeOutDuration);

        // 2026.08.07_psb수정
        // 영상 첫 프레임이 준비되기 전에는 검은 화면을 유지한다.
        isWaitingForEncounterFirstFrame = true;

        EventBus<UISetCutsceneEvent>.Publish(
            new UISetCutsceneEvent(
                BossEncounterCutsceneId,
                bossEncounterVideoClip,
                string.Empty,
                CutscenePlaybackType.BossEncounter));

        pendingBossSceneLoadOperation = SceneManager.LoadSceneAsync(bossSceneName);

        if (pendingBossSceneLoadOperation == null)
        {
            Debug.LogError($"Failed to load boss scene: {bossSceneName}", this);
            isEncounterLoading = false;
            yield break;
        }

        pendingBossSceneLoadOperation.allowSceneActivation = false;

        float elapsedTime = 0f;

        while (pendingBossSceneLoadOperation.progress < 0.9f ||
               elapsedTime < minimumEncounterLoadingPreviewTime)
        {
            elapsedTime += Time.unscaledDeltaTime;
            yield return null;
        }

        EventBus<UIBossEncounterLoadingReadyEvent>.Publish(
            new UIBossEncounterLoadingReadyEvent());

        while (!pendingBossSceneLoadOperation.isDone)
            yield return null;

        pendingBossSceneLoadOperation = null;
        isEncounterLoading = false;
    }

    // 보스 사망 후 영상의 첫 프레임을 검은 화면 아래에서 재생해 자연스럽게 전환합니다.
    private IEnumerator PlayBossClearCutscene()
    {
        isBossClearPlaying = true;

        yield return new WaitForSecondsRealtime(bossClearFadeOutDelay);

        EventBus<UIOpenOverlayEvent>.Publish(
            new UIOpenOverlayEvent(UIOverlayState.Cutscene));

        yield return Fade(0f, 1f, bossClearFadeOutDuration);

        // 2026.08.07_psb수정
        // 영상 첫 프레임이 준비되기 전에는 검은 화면을 유지한다.
        isWaitingForBossClearFirstFrame = true;

        EventBus<UISetCutsceneEvent>.Publish(
            new UISetCutsceneEvent(
                BossClearCutsceneId,
                bossClearVideoClip,
                string.Empty,
                CutscenePlaybackType.BossClear));

    }

    // 2026.08.07_psb수정
    // 영상 첫 프레임 확인 후 기존 연출 지연 시간을 적용해 클리어 영상을 자연스럽게 노출한다.
    private IEnumerator FadeInBossClearVideoAfterFirstFrame()
    {
        yield return new WaitForSecondsRealtime(bossClearVideoFadeInDelay);

        EventBus<UIFadeEvent>.Publish(
            new UIFadeEvent(1f, 0f, bossClearFadeInDuration));
    }

    // 공통 Fader 완료 시점까지 기다려 영상 재생과 페이드 순서를 보장합니다.
    private IEnumerator Fade(float fromAlpha, float toAlpha, float duration)
    {
        bool isFinished = false;

        EventBus<UIFadeEvent>.Publish(
            new UIFadeEvent(
                fromAlpha,
                toAlpha,
                duration,
                () => isFinished = true));

        yield return new WaitUntil(() => isFinished);
    }
}
