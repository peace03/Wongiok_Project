using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;

// 챕터 선택 화면 전체 흐름 담당 View
// 입장/저장/로딩 기능 x >> EventBus 요청만 발행
public class ChapterSelectView : UIViewBase
{
    // 챕터 연결
    [Serializable]
    private struct ChapterBinding
    {
        public int chapterId;
        public string chapterNumber;
        public string chapterName;
        public Sprite titleCardThumbnail;
        public Sprite titleCardBackground;
        [TextArea] public string description;
        public ChapterListState state;
        public ChapterListView listView;

        public Sprite listTitleSprite;
        public Sprite listSubNumberSprite;

        [Header("Boss Info")]
        public Sprite bossPortrait;
        public string bossName;
        public string bossCodeName;
        public string bossJob;
    }

    [Header("Chapter")]
    [SerializeField] private List<ChapterBinding> chapterBindings = new();

    [Header("Info")]
    [SerializeField] private Text chapterTitleText;
    [SerializeField] private Text chapterDescriptionText;

    [Header("Buttons")]
    [SerializeField] private CommonButtonView enterButton;
    [SerializeField] private CommonButtonView backButton;

    [Header("Boss Info")]
    [SerializeField] private Image bossPortraitImage; 
    [SerializeField] private Text bossNameText;
    [SerializeField] private Text bossCodeNameText;
    [SerializeField] private Text bossJobText;


    private int selectedChapterId = -1;

    protected override void Awake()
    {
        base.Awake();
        EventBus<UISetChapterProgressEvent>.action += HandleSetChapterProgress;
    }

    private void OnDestroy()
    {
        EventBus<UISetChapterProgressEvent>.action -= HandleSetChapterProgress;
    }

    protected override void OnShow()
    {
        ClearSelection();
        RefreshChapterList();
        RefreshEnterButton();
        SetupBackButton();
    }

    protected override void OnHide()
    {
        ClearChapterList();

        if (enterButton != null)
        {
            enterButton.Clear();
        }

        if (backButton != null)
        {
            backButton.Clear();
        }
    }

    public void RefreshChapterList()
    {
        foreach (ChapterBinding binding in chapterBindings)
        {
            if (binding.listView == null)
                continue;

            binding.listView.Setup(
                binding.chapterId,
                binding.chapterNumber,
                binding.chapterName,
                binding.listTitleSprite,
                binding.listSubNumberSprite,
                binding.state,
                HandleChapterSelected);
        }
    }

    private void HandleChapterSelected(int chapterId)
    {
        selectedChapterId = chapterId;

        foreach (ChapterBinding binding in chapterBindings)
        {
            if (binding.listView != null)
            {
                binding.listView.SetSelected(binding.chapterId == selectedChapterId);
            }

            if (binding.chapterId == selectedChapterId)
            {
                SetText(chapterTitleText, binding.chapterName);
                SetText(chapterDescriptionText, binding.description);

                SetImage(bossPortraitImage, binding.bossPortrait);
                SetText(bossNameText, binding.bossName);
                SetText(bossCodeNameText, binding.bossCodeName);
                SetText(bossJobText, binding.bossJob);
            }
        }

        RefreshEnterButton();
    }

    private void HandleEnterClicked()
    {
        if (selectedChapterId < 0)
            return;

        PublishChapterEnterRequested();
    }

    private void HandleBackClicked()
    {
        EventBus<UIChapterBackRequestedEvent>.Publish(default);
    }

    private void HandleSetChapterProgress(UISetChapterProgressEvent eventData)
    {
        for (int i = 0; i < chapterBindings.Count; i++)
        {
            ChapterBinding binding = chapterBindings[i];

            if (binding.chapterId <= eventData.HighestClearedChapterId)
            {
                binding.state = ChapterListState.Cleared;
            }
            else if (binding.chapterId == eventData.HighestClearedChapterId + 1)
            {
                binding.state = ChapterListState.Playable;
            }
            else
            {
                binding.state = ChapterListState.Locked;
            }

            chapterBindings[i] = binding;
        }

        RefreshChapterList();
    }

    private void PublishChapterEnterRequested()
    {
        foreach (ChapterBinding binding in chapterBindings)
        {
            if (binding.chapterId != selectedChapterId)
                continue;

            EventBus<UIChapterEnterRequestedEvent>.Publish(
                new UIChapterEnterRequestedEvent(
                    binding.chapterId,
                    binding.titleCardThumbnail,
                    binding.titleCardBackground));

            return;
        }
    }

    private void RefreshEnterButton()
    {
        if (enterButton == null)
            return;

        enterButton.Setup("입장", HandleEnterClicked, selectedChapterId >= 0);
    }

    private void SetupBackButton()
    {
        if (backButton != null)
        {
            backButton.Setup("뒤로가기", HandleBackClicked);
        }
    }

    private void ClearSelection()
    {
        selectedChapterId = -1;
        SetText(chapterTitleText, string.Empty);
        SetText(chapterDescriptionText, string.Empty);
        SetImage(bossPortraitImage, null);
        SetText(bossNameText, string.Empty);
        SetText(bossCodeNameText, string.Empty);
        SetText(bossJobText, string.Empty);
    }

    private void ClearChapterList()
    {
        foreach (ChapterBinding binding in chapterBindings)
        {
            if (binding.listView != null)
            {
                binding.listView.Clear();
            }
        }
    }

    private void SetText(Text targetText, string value)
    {
        if (targetText == null)
            return;

        targetText.text = value;
    }

    private void SetImage(Image targetImage, Sprite sprite)
    {
        if (targetImage == null)
            return;

        targetImage.sprite = sprite;
        targetImage.enabled = sprite != null;
    }
}