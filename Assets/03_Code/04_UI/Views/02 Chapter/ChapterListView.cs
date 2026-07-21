using UnityEngine;
using UnityEngine.UI;
using System;

// 챕터 선택 화면에서 반복 사용할 개별 챕터 항목 View
// 직접 EventBus 발행 X, 부모인 ChapterSelectView에 선택 콜백만 전달
public class ChapterListView : MonoBehaviour
{
    [Header("공용 버튼 비주얼 - 클리어 / 잠김")]
    [SerializeField] private GameObject commonVisualRoot;
    [SerializeField] private Image commonBackgroundImage;
    [SerializeField] private Image commonTitleImage;
    [SerializeField] private Image commonSubNumberImage;
    [SerializeField] private GameObject clearMarkerObject;
    [SerializeField] private GameObject lockedMarkerObject;


    [Header("플레이 가능한 비주얼")]
    [SerializeField] private GameObject playableVisualRoot;
    [SerializeField] private Image playableBackgroundImage;
    [SerializeField] private Image playableTitleImage;
    [SerializeField] private Image playableSubNumberImage;
    [SerializeField] private GameObject playableIconObject;
    [SerializeField] private GameObject playableMarkerObject;

    [Header("상태 스프라이트")]
    [SerializeField] private Sprite clearedSprite;
    [SerializeField] private Sprite playableSprite;
    [SerializeField] private Sprite lockedSprite;

    [Header("UI Elements")]
    [SerializeField] private Button button;
    [SerializeField] private GameObject selectedMarker;
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("텍스트 사용x")]
    [SerializeField] private Text ChapterNumberText;
    [SerializeField] private Text stateText;

    private int chapterID;
    private ChapterListState chapterState;
    private Action<int> selectedCallback;

    public void Setup(
        int chapterId,
        string chapterNumber,
        string chapterName,
        Sprite titleSprite,
        Sprite subNumberSprite,
        ChapterListState chapterState,
        Action<int> onSelected)
    {
        ClearButtonListener();

        this.chapterID = chapterId;
        this.chapterState = chapterState;
        selectedCallback = onSelected;

        HideLegacyTexts();
        RefreshStateVisual(titleSprite, subNumberSprite);
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
    private void RefreshStateVisual(Sprite titleSprite, Sprite subNumberSprite)
    {
        bool isLocked = chapterState == ChapterListState.Locked;
        bool isCleared = chapterState == ChapterListState.Cleared;
        bool isPlayable = chapterState == ChapterListState.Playable;

        if (button != null)
        {
            button.interactable = !isLocked;
        }

        SetObjectActive(commonVisualRoot, isCleared || isLocked);
        SetObjectActive(playableVisualRoot, isPlayable);

        if (isCleared)
        {
            SetImage(commonBackgroundImage, clearedSprite);
            SetImage(commonTitleImage, titleSprite);
            SetImage(commonSubNumberImage, subNumberSprite);

            SetObjectActive(clearMarkerObject, true);
            SetObjectActive(lockedMarkerObject, false);
        }
        else if (isLocked)
        {
            SetImage(commonBackgroundImage, lockedSprite);
            SetImage(commonTitleImage, titleSprite);
            SetImage(commonSubNumberImage, subNumberSprite);

            SetObjectActive(clearMarkerObject, false);
            SetObjectActive(lockedMarkerObject, true);
        }
        else if (isPlayable)
        {
            SetImage(playableBackgroundImage, playableSprite);
            SetImage(playableTitleImage, titleSprite);
            SetImage(playableSubNumberImage, subNumberSprite);

            SetObjectActive(playableIconObject, true);
            SetObjectActive(playableMarkerObject, true);
        }

        if (!isPlayable)
        {
            SetObjectActive(playableIconObject, false);
            SetObjectActive(playableMarkerObject, false);
        }

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
        }
    }

    private void HideLegacyTexts()
    {
        SetObjectActive(ChapterNumberText, false);
        SetObjectActive(ChapterNumberText, true);
        SetObjectActive(stateText, false);
    }

    // 이거 필요 없어보임
    private void ClearButtonListener()
    {
        if (button != null)
        {
            button.onClick.RemoveListener(HandleClicked);
        }    
    }

    private void SetImage(Image targetImage, Sprite sprite)
    {
        if (targetImage == null)
            return;

        targetImage.sprite = sprite;
        targetImage.enabled = sprite != null;
        targetImage.gameObject.SetActive(sprite != null);
    }

    private void SetObjectActive(Component targetComponent, bool isActive)
    {
        if (targetComponent == null)
            return;

        targetComponent.gameObject.SetActive(isActive);
    }

    private void SetObjectActive(GameObject targetObject, bool isActive)
    {
        if (targetObject == null)
            return;

        targetObject.SetActive(isActive);
    }
}
