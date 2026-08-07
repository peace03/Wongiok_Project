using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Video;


// 테스트 씬 전환
public class PrototypeSceneBridge : MonoBehaviour
{
    [Serializable]
    private struct ChapterTitleCardBinding
    {
        public int chapterId;
        public string title;
        public string subtitle;
        [TextArea] public string description;
        public Sprite thumbnail;
        public Sprite background;
        public VideoClip loadingVideoClip;
    }

    [Header("페이드")]
    [SerializeField] private float fadeOutDuration = 0.35f;
    [SerializeField] private float fadeInDuration = 0.35f;

    [Header("팀 스플래시")]
    [SerializeField] private GameObject teamSplashObject;
    [SerializeField] private GameObject teamNameObject;

    [SerializeField] private float splashFadeInDuration = 0.6f;
    [SerializeField] private float splashDisplayDuration = 1.5f;
    [SerializeField] private float splashFadeOutDuration = 0.6f;

    [SerializeField] private List<ChapterTitleCardBinding> chapterTitleCards = new();
    [SerializeField] private string inGameSceneName = "InGame2";

    [SerializeField] private float testMinimumLoadingPreviewTime = 3f;
    private AsyncOperation pendingInGameLoadOperation;
    private int pendingInGameLoadChapterId = -1;
    private bool isInGameLoading;
    // 2026.08.07_psb수정
    // 다음 영상의 첫 프레임이 준비될 때까지 검은 Fader를 유지하기 위한 전환 대기 상태다.
    private bool isWaitingForPrologueFirstFrame;
    private bool isWaitingForTitleFirstFrame;

    // 프롤로그 영상
    [SerializeField] private VideoClip prologueVideoClip;

    // 2026.08.07_psb수정
    // 첫 렌더링 전에 스플래시와 팀 이름을 숨겨 페이드 준비 전의 한 프레임 노출을 막는다.
    private void Awake()
    {
        if (teamSplashObject != null)
            teamSplashObject.SetActive(false);

        if (teamNameObject != null)
            teamNameObject.SetActive(false);
    }

    private IEnumerator Start()
    {
        PrototypeGameSession.EnsureInitialized();

        yield return null;  

        EventBus<UISetChapterProgressEvent>.Publish(
            new UISetChapterProgressEvent(
                PrototypeGameSession.HighestClearedChapterId));

        if (PrototypeGameSession.TryConsumeSkipPrologueOnNextLobbyEnter())
        {
            if (teamSplashObject != null)
                teamSplashObject.SetActive(false);

            ShowTitle();
        }
        else
        {
            yield return PlayTeamSplashThenPrologue();
        }

        if (PrototypeGameSession.TryConsumePendingTitleCard(out int chapterId))
            ShowChapterTitleCard(chapterId);
    }

    private void OnEnable()
    {
        EventBus<UICutsceneFinishedEvent>.action += HandleCutsceneFinished;
        // 2026.08.07_psb수정
        // 영상 첫 프레임 준비 신호를 받아 검은 화면을 걷는 시점을 결정한다.
        EventBus<UIVideoFirstFrameReadyEvent>.action += HandleVideoFirstFrameReady;

        EventBus<UITitleNewGameRequestedEvent>.action += HandleTitleNewGameRequested;
        EventBus<UITitleContinueRequestedEvent>.action += HandleTitleContinueRequested;

        EventBus<UIChapterEnterRequestedEvent>.action += HandleChapterEnterRequested;
        EventBus<UIChapterTitleCardContinueRequestedEvent>.action += HandleChapterTitleCardContinueRequested;
        EventBus<UIChapterTitleCardActivateSceneRequestedEvent>.action += HandleChapterTitleCardActivateSceneRequested;
    }

    private void OnDisable()
    {
        EventBus<UICutsceneFinishedEvent>.action -= HandleCutsceneFinished;
        // 2026.08.07_psb수정
        // 영상 첫 프레임 준비 신호 구독을 해제한다.
        EventBus<UIVideoFirstFrameReadyEvent>.action -= HandleVideoFirstFrameReady;

        EventBus<UITitleNewGameRequestedEvent>.action -= HandleTitleNewGameRequested;
        EventBus<UITitleContinueRequestedEvent>.action -= HandleTitleContinueRequested;

        EventBus<UIChapterEnterRequestedEvent>.action -= HandleChapterEnterRequested;
        EventBus<UIChapterTitleCardContinueRequestedEvent>.action -= HandleChapterTitleCardContinueRequested;
        EventBus<UIChapterTitleCardActivateSceneRequestedEvent>.action -= HandleChapterTitleCardActivateSceneRequested;
    }

    private void HandleCutsceneFinished(UICutsceneFinishedEvent eventData)
    {
        if (eventData.CutsceneId != "prologue") return;

        if (eventData.WasSkipped)
        {
            // 2026.08.07_psb수정
            // 스킵 경로는 이미 CutsceneView가 검은 화면을 완성했으므로 다시 페이드 아웃하지 않는다.
            PrepareTitleUnderBlackFader();
            return;
        }

        // 2026.08.07_psb수정
        // 프롤로그 종료 후 타이틀 영상의 첫 프레임을 준비한 뒤에만 검은 페이드를 걷는다.
        StartCoroutine(TransitionPrologueToTitle());
    }

    // 2026.08.07_psb수정
    // 검은 화면 아래에서 타이틀 View와 입장 영상을 준비해 빈 화면 노출 없이 전환한다.
    private IEnumerator TransitionPrologueToTitle()
    {
        bool isFadeOutFinished = false;

        EventBus<UIFadeEvent>.Publish(
            new UIFadeEvent(
                0f,
                1f,
                fadeOutDuration,
                () => isFadeOutFinished = true));

        yield return new WaitUntil(() => isFadeOutFinished);

        PrepareTitleUnderBlackFader();
    }

    // 2026.08.07_psb수정
    // 이미 검은 화면인 상태에서 타이틀의 첫 프레임을 준비하고 준비 완료 신호를 기다린다.
    private void PrepareTitleUnderBlackFader()
    {
        isWaitingForTitleFirstFrame = true;
        ShowTitle();
    }

    // 2026.08.07_psb수정
    // 준비된 영상의 첫 프레임이 확인된 경우에만 Fader를 투명하게 전환한다.
    private void HandleVideoFirstFrameReady(UIVideoFirstFrameReadyEvent eventData)
    {
        if (eventData.VideoId == "prologue" && isWaitingForPrologueFirstFrame)
        {
            isWaitingForPrologueFirstFrame = false;
            EventBus<UIFadeEvent>.Publish(new UIFadeEvent(1f, 0f, fadeInDuration));
            return;
        }

        if (eventData.VideoId == "title-enter" && isWaitingForTitleFirstFrame)
        {
            isWaitingForTitleFirstFrame = false;
            EventBus<UIFadeEvent>.Publish(new UIFadeEvent(1f, 0f, fadeInDuration));
        }
    }

    // 2026.08.07_psb수정
    // TitleView가 활성화된 뒤 저장 파일 상태를 전달해 이어하기 버튼이 실제 저장 상태를 반영하도록 한다.
    private void ShowTitle()
    {
        EventBus<UIChangeScreenEvent>.Publish(
            new UIChangeScreenEvent(UIScreenState.Title));

        EventBus<UISetTitleSaveStateEvent>.Publish(
            new UISetTitleSaveStateEvent(PrototypeGameSession.HasSaveData));
    }

    private void ShowChapterTitleCard(int chapterId, Sprite fallbackThumbnail = null, Sprite fallbackBackground = null)
    {
        foreach (ChapterTitleCardBinding binding in chapterTitleCards)
        {
            if (binding.chapterId != chapterId)
                continue;

            Sprite thumbnail = binding.thumbnail != null 
                ? binding.thumbnail
                : fallbackThumbnail;

            Sprite background = binding.background != null
                ? binding.background
                : fallbackBackground;

            EventBus<UIChangeScreenEvent>.Publish(
                new UIChangeScreenEvent(UIScreenState.ChapterTitleCard));

            EventBus<UISetChapterTitleCardEvent>.Publish(
                new UISetChapterTitleCardEvent(
                    binding.chapterId,
                    binding.title,
                    binding.subtitle,
                    binding.description,
                    thumbnail,
                    background,
                    binding.loadingVideoClip));

            return;
        }

        Debug.LogError($"Chapter {chapterId}의 TitleCard Binding이 없다.");
    }

    private void HandleChapterEnterRequested(UIChapterEnterRequestedEvent eventData)
    {
        PrototypeGameSession.BeginChapter(eventData.ChapterId);
        ShowChapterTitleCard(eventData.ChapterId, eventData.Thumbnail, eventData.Background);
    }

    private void HandleTitleNewGameRequested(UITitleNewGameRequestedEvent eventData)
    {
        ChangeScreenWithFade(() =>
        {
            PrototypeGameSession.StartNewGame();

            EventBus<UISetTitleSaveStateEvent>.Publish(
                new UISetTitleSaveStateEvent(true));

            EventBus<UIChangeScreenEvent>.Publish(
                new UIChangeScreenEvent(UIScreenState.ChapterSelect));

            EventBus<UISetChapterProgressEvent>.Publish(
                new UISetChapterProgressEvent(PrototypeGameSession.HighestClearedChapterId));
        });
    }

    private void HandleTitleContinueRequested(UITitleContinueRequestedEvent eventData)
    {
        ChangeScreenWithFade(() =>
        {
            PrototypeGameSession.EnsureInitialized();

            EventBus<UIChangeScreenEvent>.Publish(
                new UIChangeScreenEvent(UIScreenState.ChapterSelect));

            EventBus<UISetChapterProgressEvent>.Publish(
                new UISetChapterProgressEvent(PrototypeGameSession.HighestClearedChapterId));
        });
    }

    private void HandleChapterTitleCardContinueRequested(UIChapterTitleCardContinueRequestedEvent eventData)
    {
        if (isInGameLoading) return;

        StartCoroutine(LoadInGame(eventData.ChapterId));
    }

    private void HandleChapterTitleCardActivateSceneRequested(UIChapterTitleCardActivateSceneRequestedEvent eventData)
    {
        if (!isInGameLoading || pendingInGameLoadOperation == null || eventData.ChapterId != pendingInGameLoadChapterId) return;

        pendingInGameLoadOperation.allowSceneActivation = true;
    }

    private IEnumerator LoadInGame(int chapterId)
    {
        isInGameLoading = true;
        pendingInGameLoadChapterId = chapterId;

        pendingInGameLoadOperation = SceneManager.LoadSceneAsync(inGameSceneName);
        pendingInGameLoadOperation.allowSceneActivation = false;

        float elapsedTime = 0f;

        while (pendingInGameLoadOperation.progress < 0.9f || elapsedTime < testMinimumLoadingPreviewTime)
        {
            elapsedTime += Time.unscaledDeltaTime;
            yield return null;
        }

        EventBus<UIChapterTitleCardLoadingReadyEvent>.Publish(
            new UIChapterTitleCardLoadingReadyEvent(chapterId));

        while (!pendingInGameLoadOperation.isDone)
        {
            yield return null;
        }

        pendingInGameLoadOperation = null;
        pendingInGameLoadChapterId = -1;
        isInGameLoading = false;
    }

    private IEnumerator PlayTeamSplashThenPrologue()
    {
        if (teamSplashObject != null)
            teamSplashObject.SetActive(true);

        if (teamNameObject != null)
            teamNameObject.SetActive(false);

        bool isBlackScreenReady = false;

        // 먼저 기존 페이드를 즉시 검은 화면으로 만들어 이름이 보이지 않게함
        EventBus<UIFadeEvent>.Publish(
            new UIFadeEvent(
                1f,
                1f,
                0f,
                () => isBlackScreenReady = true));

        yield return new WaitUntil(() => isBlackScreenReady);

        if (teamNameObject != null)
            teamNameObject.SetActive(true);

        bool isSplashFadeInFinished = false;

        // 페이드가 걷히며 뒤에 있는 팀 이름이 점점 밝아지는 효과
        EventBus<UIFadeEvent>.Publish(
            new UIFadeEvent(
                1f,
                0f,
                splashFadeInDuration,
                () => isSplashFadeInFinished = true));

        yield return new WaitUntil(() => isSplashFadeInFinished);

        yield return new WaitForSecondsRealtime(splashDisplayDuration);

        bool isSplashFadeOutFinished = false;

        // 페이드 다시 덮으면서 검은 화면 전환
        EventBus<UIFadeEvent>.Publish(
            new UIFadeEvent(
                0f,
                1f,
                splashFadeOutDuration,
                () => isSplashFadeOutFinished = true));

        yield return new WaitUntil(() => isSplashFadeOutFinished);

        if (teamSplashObject != null)
            teamSplashObject.SetActive(false);

        // 2026.08.07_psb수정
        // 검은 Fader 아래에서 Prologue View를 먼저 열어 영상 설정 직전의 빈 화면 노출을 막는다.
        // 2026.08.07_psb수정
        // Prologue 영상의 첫 프레임이 준비될 때까지 검은 Fader를 유지한다.
        isWaitingForPrologueFirstFrame = true;

        EventBus<UIChangeScreenEvent>.Publish(
            new UIChangeScreenEvent(UIScreenState.Prologue));

        EventBus<UISetCutsceneEvent>.Publish(
            new UISetCutsceneEvent(
                "prologue",
                prologueVideoClip,
                string.Empty));

    }

    private void ChangeScreenWithFade(System.Action changeScreenAction)
    {
        EventBus<UIFadeEvent>.Publish(
            new UIFadeEvent(
                0f,
                1f,
                fadeOutDuration,
                () =>
                {
                    changeScreenAction?.Invoke();

                    EventBus<UIFadeEvent>.Publish(
                        new UIFadeEvent(
                            1f,
                            0f,
                            fadeInDuration));
                }));
    }
}
