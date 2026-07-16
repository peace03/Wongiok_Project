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
    }

    [SerializeField] private List<ChapterTitleCardBinding> chapterTitleCards = new();
    [SerializeField] private string inGameSceneName = "InGame";
    [SerializeField] private float testLoadingTime = 3f;

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

        EventBus<UIChangeScreenEvent>.Publish(
            new UIChangeScreenEvent(UIScreenState.Title));
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
                    background));

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
        PrototypeGameSession.StartNewGame();

        EventBus<UISetTitleSaveStateEvent>.Publish(
            new UISetTitleSaveStateEvent(true));

        EventBus<UIChangeScreenEvent>.Publish(
            new UIChangeScreenEvent(UIScreenState.ChapterSelect));

        EventBus<UISetChapterProgressEvent>.Publish(
            new UISetChapterProgressEvent(PrototypeGameSession.HighestClearedChapterId));
    }

    private void HandleTitleContinueRequested(UITitleContinueRequestedEvent eventData)
    {
        PrototypeGameSession.EnsureInitialized();

        EventBus<UIChangeScreenEvent>.Publish(
            new UIChangeScreenEvent(UIScreenState.ChapterSelect));

        EventBus<UISetChapterProgressEvent>.Publish(
            new UISetChapterProgressEvent(PrototypeGameSession.HighestClearedChapterId));
    }

    private void HandleChapterTitleCardContinueRequested(UIChapterTitleCardContinueRequestedEvent eventData)
    {
        StartCoroutine(LoadInGame());
    }

    private IEnumerator LoadInGame()
    {
        EventBus<UIChangeScreenEvent>.Publish(
            new UIChangeScreenEvent(UIScreenState.Loading));

        float elapsedTime = 0f;

        EventBus<UISetLoadingProgressEvent>.Publish(
            new UISetLoadingProgressEvent(0f, "페이지 넘기는 중 . . ."));

        while (elapsedTime < testLoadingTime)
        {
            elapsedTime += Time.unscaledDeltaTime;

            float progress = testLoadingTime <= 0f ? 1f : Mathf.Clamp01(elapsedTime / testLoadingTime);

            EventBus<UISetLoadingProgressEvent>.Publish(
                new UISetLoadingProgressEvent(progress, "페이지 넘기는 중 . . ."));

            yield return null;
        }

        EventBus<UISetLoadingProgressEvent>.Publish(
            new UISetLoadingProgressEvent(1f, "페이지 넘기는 중 . . ."));

        SceneManager.LoadScene(inGameSceneName);
    }
}
