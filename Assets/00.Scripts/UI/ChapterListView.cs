using UnityEngine;
using UnityEngine.UI;
using System;

// 챕터 선택 화면에서 반복 사용할 개별 챕터 항목 View
// 직접 EventBus 발행 X, 부모인 ChapterSelectView에 선택 콜백만 전달
public class ChapterListView : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private Text ChapterNumberText;
    [SerializeField] private Text ChapterNameText;
    [SerializeField] private Text stateText;
    [SerializeField] private GameObject selectedMarker;
    [SerializeField] private GameObject lockedMarker;
    [SerializeField] private CanvasGroup canvasGroup;

    private int chapterID;
    private ChapterListState chapterState;
    private Action<int> selectedCallback;

    public void Setup(int chapterId,
        string chapterNumber,
        string chapterName,
        ChapterListState chapterState,
        Action<int> onSelected)
    {
        ClearButtonListener();

        this.chapterID = chapterId;
        this.chapterState = chapterState;
        selectedCallback = onSelected;

        SetText(ChapterNumberText, chapterNumber);
        SetText(ChapterNameText, chapterName);
        RefreshStateVisual();
        SetSelected(false);

        if (button != null)
        {
            button.onClick.AddListener(HandleClicked);
        }
    }

    public void SetSelected(bool isSelected)
    {
        if (selectedMarker != null)
        {
            selectedMarker.SetActive(isSelected);
        }
    }

    public void Clear()
    {
        ClearButtonListener();
        selectedCallback = null;
        SetSelected(false);
    }

    private void OnDestroy()
    {
        Clear();
    }


    private void HandleClicked()
    {
        if (chapterState == ChapterListState.Locked)
            return;

        selectedCallback?.Invoke(chapterID);
    }

    // 챕터 선택 버튼 외형 반영
    private void RefreshStateVisual()
    {
        bool isLocked = chapterState == ChapterListState.Locked;

        if (button != null)
        {
            button.interactable = !isLocked;
        }

        if (lockedMarker != null)
        {
            lockedMarker.SetActive(isLocked);
        }

        if (canvasGroup != null)
        {
            canvasGroup.alpha = isLocked ? 0.45f : 1f;
        }

        // 기존 switch문을 값을 바로 반환하는 형태로 짧게 쓴 문법
        // 해석: stateLabel에 상태에 따른 switch문의 값을 넣는다. (그 외에는 빈 문자열 삽입)
        string stateLabel = chapterState switch
        {
            ChapterListState.Locked => "잠김",
            ChapterListState.Playable => "진행 가능",
            ChapterListState.Cleared => "클리어",
            _ => string.Empty
        };

        SetText(stateText, stateLabel);
    }

    // 이거 필요 없어보임
    private void ClearButtonListener()
    {
        if (button != null)
        {
            button.onClick.RemoveListener(HandleClicked);
        }    
    }

    // 텍스트 설정 메서드
    private void SetText(Text targetText, string value)
    {
        if (targetText == null)
            return;

        targetText.text = value;
    }
}
