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

    private void OnEnable()
    {
        EventBus<UIBossEncounterRequestedEvent>.action += HandleBossEncounterRequested;
        EventBus<UIBossEncounterActivateSceneRequestedEvent>.action += HandleBossEncounterActivateSceneRequested;
        EventBus<BossDeathPresentationFinishedEvent>.action += HandleBossDeathPresentationFinished;
        EventBus<UICutsceneFinishedEvent>.action += HandleCutsceneFinished;
    }

    private void OnDisable()
    {
        EventBus<UIBossEncounterRequestedEvent>.action -= HandleBossEncounterRequested;
        EventBus<UIBossEncounterActivateSceneRequestedEvent>.action -= HandleBossEncounterActivateSceneRequested;
        EventBus<BossDeathPresentationFinishedEvent>.action -= HandleBossDeathPresentationFinished;
        EventBus<UICutsceneFinishedEvent>.action -= HandleCutsceneFinished;
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

    // 조우 영상 재생과 보스 씬 비동기 로딩을 같은 전환 구간에서 시작합니다.
    private IEnumerator LoadBossSceneWithEncounterCutscene()
    {
        isEncounterLoading = true;

        EventBus<UIOpenOverlayEvent>.Publish(
            new UIOpenOverlayEvent(UIOverlayState.Cutscene));

        yield return Fade(0f, 1f, encounterFadeOutDuration);

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

        EventBus<UIFadeEvent>.Publish(
            new UIFadeEvent(1f, 0f, encounterFadeInDuration));

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

        EventBus<UISetCutsceneEvent>.Publish(
            new UISetCutsceneEvent(
                BossClearCutsceneId,
                bossClearVideoClip,
                string.Empty,
                CutscenePlaybackType.BossClear));

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
