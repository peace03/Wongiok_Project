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

    // 프롤로그 영상
    [SerializeField] private VideoClip prologueVideoClip;


    private IEnumerator Start()
    {
        PrototypeGameSession.EnsureInitialized();

        yield return null;  

        EventBus<UISetTitleSaveStateEvent>.Publish(
            new UISetTitleSaveStateEvent(PrototypeGameSession.HasSaveData));

        EventBus<UISetChapterProgressEvent>.Publish(
            new UISetChapterProgressEvent(
                PrototypeGameSession.HighestClearedChapterId));

        if (PrototypeGameSession.TryConsumeSkipPrologueOnNextLobbyEnter())
        {
            if (teamSplashObject != null)
                teamSplashObject.SetActive(false);

            EventBus<UIChangeScreenEvent>.Publish(
                new UIChangeScreenEvent(UIScreenState.Title));
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

        EventBus<UITitleNewGameRequestedEvent>.action += HandleTitleNewGameRequested;
        EventBus<UITitleContinueRequestedEvent>.action += HandleTitleContinueRequested;

        EventBus<UIChapterEnterRequestedEvent>.action += HandleChapterEnterRequested;
        EventBus<UIChapterTitleCardContinueRequestedEvent>.action += HandleChapterTitleCardContinueRequested;
        EventBus<UIChapterTitleCardActivateSceneRequestedEvent>.action += HandleChapterTitleCardActivateSceneRequested;
    }

    private void OnDisable()
    {
        EventBus<UICutsceneFinishedEvent>.action -= HandleCutsceneFinished;

        EventBus<UITitleNewGameRequestedEvent>.action -= HandleTitleNewGameRequested;
        EventBus<UITitleContinueRequestedEvent>.action -= HandleTitleContinueRequested;

        EventBus<UIChapterEnterRequestedEvent>.action -= HandleChapterEnterRequested;
        EventBus<UIChapterTitleCardContinueRequestedEvent>.action -= HandleChapterTitleCardContinueRequested;
        EventBus<UIChapterTitleCardActivateSceneRequestedEvent>.action -= HandleChapterTitleCardActivateSceneRequested;
    }

    private void HandleCutsceneFinished(UICutsceneFinishedEvent eventData)
    {
        if (eventData.CutsceneId != "prologue") return;

        ChangeScreenWithFade(() =>
        {
            EventBus<UIChangeScreenEvent>.Publish(
            new UIChangeScreenEvent(UIScreenState.Title));
        });
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

        EventBus<UISetCutsceneEvent>.Publish(
            new UISetCutsceneEvent(
                "prologue",
                prologueVideoClip,
                string.Empty));

        // 검은 화면 아래에서 프롤로그 시작 후 페이드 걷힘
        EventBus<UIFadeEvent>.Publish(
            new UIFadeEvent(
                1f,
                0f,
                fadeInDuration));
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
