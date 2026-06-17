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
        [TextArea] public string description;
        public ChapterListState state;
        public ChapterListView listView;
    }

    [Header("Chapter")]
    [SerializeField] private List<ChapterBinding> chapterBindings = new();

    [Header("Info")]
    [SerializeField] private Text chapterTitleText;
    [SerializeField] private Text chapterDescriptionText;

    [Header("Buttons")]
    [SerializeField] private CommonButtonView enterButton;
    [SerializeField] private CommonButtonView backButton;

    private int selectedChapterId = -1;

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
            }
        }

        RefreshEnterButton();
    }

    private void HandleEnterClicked()
    {
        if (selectedChapterId < 0)
            return;

        EventBus<UIShowConfirmPopupEvent>.Publish(
            new UIShowConfirmPopupEvent(
                "챕터 입장",
                "선택한 챕터에 입장할까요?",
                PublishChapterEnterRequested));
    }

    private void HandleBackClicked()
    {
        EventBus<UIChapterBackRequestedEvent>.Publish(default);
    }

    private void PublishChapterEnterRequested()
    {
        EventBus<UIChapterEnterRequestedEvent>.Publish(
            new UIChapterEnterRequestedEvent(selectedChapterId));
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
}