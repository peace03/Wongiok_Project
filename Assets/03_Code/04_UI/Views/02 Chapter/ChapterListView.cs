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

    private int chapterID;
    private ChapterListState chapterState;
    private Action<int> selectedCallback;

    // 2026.08.10_UI 정리: 전달받은 데이터와 콜백으로 UI 상태 상태를 설정한다.
    public void Setup(
        int chapterId,
        Sprite titleSprite,
        Sprite subNumberSprite,
        ChapterListState chapterState,
        Action<int> onSelected)
    {
        ClearButtonListener();

        this.chapterID = chapterId;
        this.chapterState = chapterState;
        selectedCallback = onSelected;

        RefreshStateVisual(titleSprite, subNumberSprite);
        SetSelected(false);

        if (button != null)
        {
            button.onClick.AddListener(HandleClicked);
        }
    }

    // 2026.08.10_UI 정리: 선택 표시 값을 반영한다.
    public void SetSelected(bool isSelected)
    {
        if (selectedMarker != null)
        {
            selectedMarker.SetActive(isSelected);
        }
    }

    // 2026.08.10_UI 정리: UI 상태 상태를 정리한다.
    public void Clear()
    {
        ClearButtonListener();
        selectedCallback = null;
        SetSelected(false);
    }

    // 2026.08.10_UI 정리: 파괴 시 등록한 이벤트와 임시 UI 상태를 정리한다.
    private void OnDestroy()
    {
        Clear();
    }


    // 2026.08.10_UI 정리: 클릭 관련 입력 또는 EventBus 요청을 처리한다.
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

    // 2026.08.10_UI 정리: 버튼 Listener 상태를 정리한다.
    private void ClearButtonListener()
    {
        if (button != null)
        {
            button.onClick.RemoveListener(HandleClicked);
        }    
    }

    // 2026.08.10_UI 정리: 이미지 표시 값을 반영한다.
    private void SetImage(Image targetImage, Sprite sprite)
    {
        if (targetImage == null)
            return;

        targetImage.sprite = sprite;
        targetImage.enabled = sprite != null;
        targetImage.gameObject.SetActive(sprite != null);
    }

    // 2026.08.10_UI 정리: Object 액티브 표시 값을 반영한다.
    private void SetObjectActive(Component targetComponent, bool isActive)
    {
        if (targetComponent == null)
            return;

        targetComponent.gameObject.SetActive(isActive);
    }

    // 2026.08.10_UI 정리: Object 액티브 표시 값을 반영한다.
    private void SetObjectActive(GameObject targetObject, bool isActive)
    {
        if (targetObject == null)
            return;

        targetObject.SetActive(isActive);
    }
}
