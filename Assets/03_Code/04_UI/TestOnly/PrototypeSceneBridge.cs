using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;
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



    [SerializeField] private List<ChapterTitleCardBinding> chapterTitleCards = new();
    [SerializeField] private string inGameSceneName = "InGame";

    [SerializeField] private float testMinimumLoadingPreviewTime = 3f;

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

        EventBus<UISetCutsceneEvent>.Publish(
            new UISetCutsceneEvent("prologue", prologueVideoClip, string.Empty));

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
    }

    private void OnDisable()
    {
        EventBus<UICutsceneFinishedEvent>.action -= HandleCutsceneFinished;

        EventBus<UITitleNewGameRequestedEvent>.action -= HandleTitleNewGameRequested;
        EventBus<UITitleContinueRequestedEvent>.action -= HandleTitleContinueRequested;

        EventBus<UIChapterEnterRequestedEvent>.action -= HandleChapterEnterRequested;
        EventBus<UIChapterTitleCardContinueRequestedEvent>.action -= HandleChapterTitleCardContinueRequested;
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
        StartCoroutine(LoadInGame());
    }

    private IEnumerator LoadInGame()
    {
        AsyncOperation loadOperation = SceneManager.LoadSceneAsync(inGameSceneName);

        // 테스트 용 (빌드 시 삭제 코드)
        loadOperation.allowSceneActivation = false;

        float elapsedTime = 0f;

        // 씬 로딩 기다림
        while (loadOperation.progress < 0.9f || elapsedTime < testMinimumLoadingPreviewTime)
        {
            elapsedTime += Time.unscaledDeltaTime;
            yield return null;
        }

        loadOperation.allowSceneActivation = true;

        while (!loadOperation.isDone)
        {
            yield return null;
        }
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
