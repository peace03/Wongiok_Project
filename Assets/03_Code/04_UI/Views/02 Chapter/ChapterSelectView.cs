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
        [TextArea] public string bossName;
        [TextArea] public string bossCodeName;
        [TextArea] public string bossJob;
    }

    [Header("챕터")]
    [SerializeField] private List<ChapterBinding> chapterBindings = new();

    [Header("챕터 리스트 스크롤")]
    [SerializeField] private ScrollRect chapterListScrollRect;

    private bool preserveListPositionOnNextShow;

    [Header("Info")]
    [SerializeField] private Text chapterTitleText;
    [SerializeField] private Text chapterDescriptionText;

    [Header("버튼")]
    [SerializeField] private CommonButtonView enterButton;
    [SerializeField] private CommonButtonView backButton;

    [Header("Boss Info")]
    [SerializeField] private Image bossPortraitImage; 
    [SerializeField] private Text bossNameText;
    [SerializeField] private Text bossCodeNameText;
    [SerializeField] private Text bossJobText;
    [SerializeField] private GameObject bossPanel;


    private int selectedChapterId = -1;

    // 2026.08.10_UI 정리: 컴포넌트 초기화와 이벤트 구독을 준비한다.
    protected override void Awake()
    {
        base.Awake();
        EventBus<UISetChapterProgressEvent>.action += HandleSetChapterProgress;
        EventBus<UIChapterSelectScrollPreserveRequestedEvent>.action += HandleChapterSelectScrollPreserveRequested;
    }

    // 2026.08.10_UI 정리: 파괴 시 등록한 이벤트와 임시 UI 상태를 정리한다.
    private void OnDestroy()
    {
        EventBus<UISetChapterProgressEvent>.action -= HandleSetChapterProgress;
        EventBus<UIChapterSelectScrollPreserveRequestedEvent>.action -= HandleChapterSelectScrollPreserveRequested;
    }

    // 2026.08.10_UI 정리: 화면 표시 시 필요한 UI 상태를 초기화한다.
    protected override void OnShow()
    {
        ClearSelection();
        RefreshChapterList();
        ResetChapterListScrollIfNeeded();
        RefreshEnterButton();
        SetupBackButton();
    }

    // 2026.08.10_UI 정리: 화면 숨김 시 임시 UI 상태를 정리한다.
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

    // 2026.08.10_UI 정리: 현재 데이터로 챕터 목록 표시를 갱신한다.
    public void RefreshChapterList()
    {
        foreach (ChapterBinding binding in chapterBindings)
        {
            if (binding.listView == null)
                continue;

            binding.listView.Setup(
                binding.chapterId,
                binding.listTitleSprite,
                binding.listSubNumberSprite,
                binding.state,
                HandleChapterSelected);
        }
    }

    // 2026.08.10_UI 정리: 챕터 목록 Scroll If Needed 상태를 기본값으로 초기화한다.
    private void ResetChapterListScrollIfNeeded()
    {
        if (preserveListPositionOnNextShow)
        {
            preserveListPositionOnNextShow = false;
            return;
        }

        if (chapterListScrollRect == null) return;

        Canvas.ForceUpdateCanvases();

        chapterListScrollRect.StopMovement();
        chapterListScrollRect.verticalNormalizedPosition = 1f;
    }

    // 2026.08.10_UI 정리: 챕터 Select Scroll Preserve Requested 관련 입력 또는 EventBus 요청을 처리한다.
    private void HandleChapterSelectScrollPreserveRequested(UIChapterSelectScrollPreserveRequestedEvent eventData)
    {
        preserveListPositionOnNextShow = true;
    }

    // 2026.08.10_UI 정리: 챕터 선택 관련 입력 또는 EventBus 요청을 처리한다.
    private void HandleChapterSelected(int chapterId)
    {
        selectedChapterId = chapterId;

        if (bossPanel != null) bossPanel.SetActive(true);

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

    // 2026.08.10_UI 정리: 입장 클릭 관련 입력 또는 EventBus 요청을 처리한다.
    private void HandleEnterClicked()
    {
        if (selectedChapterId < 0)
            return;

        PublishChapterEnterRequested();
    }

    // 2026.08.10_UI 정리: 뒤로가기 클릭 관련 입력 또는 EventBus 요청을 처리한다.
    private void HandleBackClicked()
    {
        EventBus<UIChapterBackRequestedEvent>.Publish(default);
    }

    // 2026.08.10_UI 정리: Set 챕터 진행도 관련 입력 또는 EventBus 요청을 처리한다.
    private void HandleSetChapterProgress(UISetChapterProgressEvent eventData)
    {
        for (int i = 0; i < chapterBindings.Count; i++)
        {
            ChapterBinding binding = chapterBindings[i];

            // 현재 공개 범위에서는 챕터 1을 클리어 후에도 진행 가능한 상태로 유지한다.
            if (binding.chapterId == 1)
            {
                binding.state = ChapterListState.Playable;
            }
            else if (binding.chapterId <= eventData.HighestClearedChapterId)
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

    // 2026.08.10_UI 정리: 챕터 입장 Requested 요청 또는 상태를 EventBus로 발행한다.
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

    // 2026.08.10_UI 정리: 현재 데이터로 입장 버튼 표시를 갱신한다.
    private void RefreshEnterButton()
    {
        if (enterButton == null)
            return;

        enterButton.Setup(UITextManager.Get("Chapter.Enter"), HandleEnterClicked, selectedChapterId >= 0);
    }

    // 2026.08.10_UI 정리: 전달받은 데이터와 콜백으로 뒤로가기 버튼 상태를 설정한다.
    private void SetupBackButton()
    {
        if (backButton != null)
        {
            backButton.Setup(UITextManager.Get("Chapter.Back"), HandleBackClicked);
        }
    }

    // 2026.08.10_UI 정리: 선택 상태를 정리한다.
    private void ClearSelection()
    {
        if (bossPanel != null) bossPanel.SetActive(false);
        selectedChapterId = -1;
        SetText(chapterTitleText, string.Empty);
        SetText(chapterDescriptionText, string.Empty);
        SetImage(bossPortraitImage, null);
        SetText(bossNameText, string.Empty);
        SetText(bossCodeNameText, string.Empty);
        SetText(bossJobText, string.Empty);
    }

    // 2026.08.10_UI 정리: 챕터 목록 상태를 정리한다.
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

    // 2026.08.10_UI 정리: 텍스트 표시 값을 반영한다.
    private void SetText(Text targetText, string value)
    {
        if (targetText == null)
            return;

        targetText.text = value;
    }

    // 2026.08.10_UI 정리: 이미지 표시 값을 반영한다.
    private void SetImage(Image targetImage, Sprite sprite)
    {
        if (targetImage == null)
            return;

        targetImage.sprite = sprite;
        targetImage.enabled = sprite != null;
    }
}
