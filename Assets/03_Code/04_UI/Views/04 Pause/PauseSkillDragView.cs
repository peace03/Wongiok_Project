using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public enum PauseSkillDragSourceType
{
    None = 0,
    OwnedSkill,
    EquippedSlot
}

public class PauseSkillDragView : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("Drag Visual")]
    [SerializeField] private Canvas rootCanvas;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private Image iconImage;
    [SerializeField] private RectTransform dragPreviewRoot;
    [SerializeField] private Image dragPreviewIconImage;
    [SerializeField] private float draggingAlpha = 0.6f;

    private PauseSkillDragSourceType sourceType = PauseSkillDragSourceType.None;
    private int skillId = -1;
    private int sourceSlotIndex = -1;
    private bool canDrag;
    private int sourceOwnedSlotIndex = -1;

    public PauseSkillDragSourceType SourceType => sourceType;
    public int SkillId => skillId;
    public int SourceSlotIndex => sourceSlotIndex;
    public int SourceOwnedSlotIndex => sourceOwnedSlotIndex;

    // 2026.08.10_UI 정리: 컴포넌트 초기화와 이벤트 구독을 준비한다.
    private void Awake()
    {
        if (rootCanvas == null)
        {
            rootCanvas = GetComponentInParent<Canvas>();
        }

        HideDragPreview();
    }

    // 2026.08.10_UI 정리: 전달받은 데이터와 콜백으로 보유 스킬 상태를 설정한다.
    public void SetupOwnedSkill(int skillId, int sourceOwnedSlotIndex, bool canDrag)
    {
        sourceType = PauseSkillDragSourceType.OwnedSkill;
        this.skillId = skillId;
        this.sourceOwnedSlotIndex = sourceOwnedSlotIndex;
        sourceSlotIndex = -1;
        this.canDrag = canDrag && skillId >= 0;
    }

    // 2026.08.10_UI 정리: 전달받은 데이터와 콜백으로 장착 슬롯 상태를 설정한다.
    public void SetupEquippedSlot(int skillId, int sourceSlotIndex, bool canDrag =true)
    {
        sourceType = PauseSkillDragSourceType.EquippedSlot;
        this.skillId = skillId;
        this.sourceSlotIndex = sourceSlotIndex;
        this.canDrag = canDrag && skillId >= 0 && sourceSlotIndex >= 0;
    }

    // 2026.08.10_UI 정리: 전달받은 데이터와 콜백으로 드래그 비주얼 Refs 상태를 설정한다.
    public void SetupDragVisualRefs(Canvas rootCanvas, RectTransform dragPreviewRoot, Image dragPreviewIconImage)
    {
        this.rootCanvas = rootCanvas;
        this.dragPreviewRoot = dragPreviewRoot;
        this.dragPreviewIconImage = dragPreviewIconImage;
    }

    // 2026.08.10_UI 정리: 드래그 데이터 상태를 정리한다.
    public void ClearDragData()
    {
        sourceType = PauseSkillDragSourceType.None;
        skillId = -1;
        sourceSlotIndex = -1;
        sourceOwnedSlotIndex = -1;
        canDrag = false;
        HideDragPreview();
        RestoreOriginalVisual();
    }

    // 2026.08.10_UI 정리: 스킬 드래그 시작 상태와 프리뷰를 준비한다.
    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!CanStartDrag())
            return;

        SetOriginalDraggingVisual(true);
        ShowDragPreview(eventData);
    }

    // 2026.08.10_UI 정리: 포인터 위치에 맞춰 드래그 프리뷰를 이동한다.
    public void OnDrag(PointerEventData eventData)
    {
        if (!CanStartDrag())
            return;

        MoveDragPreview(eventData);
    }

    // 2026.08.10_UI 정리: 드래그 종료 후 원본 UI 상태를 복원한다.
    public void OnEndDrag(PointerEventData eventData)
    {
        if (!CanStartDrag())
            return;

        HideDragPreview();
        RestoreOriginalVisual();
    }

    // 2026.08.10_UI 정리: Start 드래그 동작이 가능한지 판단한다.
    private bool CanStartDrag()
    {
        return canDrag && skillId >= 0 && sourceType != PauseSkillDragSourceType.None;
    }

    // 2026.08.10_UI 정리: 원본 Dragging 비주얼 표시 값을 반영한다.
    private void SetOriginalDraggingVisual(bool isDragging)
    {
        if (canvasGroup == null)
            return;

        canvasGroup.alpha = isDragging ? draggingAlpha : 1f;
        canvasGroup.blocksRaycasts = !isDragging;
    }

    // 2026.08.10_UI 정리: 저장된 원본 비주얼 상태를 복원한다.
    private void RestoreOriginalVisual()
    {
        if (canvasGroup == null)
            return;

        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = true;
    }

    // 2026.08.10_UI 정리: 드래그 프리뷰 UI 요소를 표시한다.
    private void ShowDragPreview(PointerEventData eventData)
    {
        if (dragPreviewRoot == null)
            return;

        if (dragPreviewIconImage != null && iconImage != null)
        {
            dragPreviewIconImage.sprite = iconImage.sprite;
            // DragPreviewIcon 자식 오브젝트가 비활성되어 있어도,
            // 드래그 시작 시 마우스를 따라오는 아이콘이 보이도록 킴
            dragPreviewIconImage.enabled = iconImage.sprite != null;
            dragPreviewIconImage.gameObject.SetActive(true);
        }

        dragPreviewRoot.gameObject.SetActive(true);
        MoveDragPreview(eventData);
    }

    // 2026.08.10_UI 정리: 드래그 프리뷰 상태를 이동한다.
    private void MoveDragPreview(PointerEventData eventData)
    {
        if (dragPreviewRoot == null || rootCanvas == null)
            return;

        RectTransform canvasRect = rootCanvas.transform as RectTransform;

        if (canvasRect == null)
            return;

        Camera eventCamera = rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay
            ? null : rootCanvas.worldCamera;

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect,
            eventData.position,
            eventCamera,
            out Vector2 localPoint))
        {
            dragPreviewRoot.anchoredPosition = localPoint;
        }
    }

    // 2026.08.10_UI 정리: 드래그 프리뷰 UI 요소를 숨긴다.
    private void HideDragPreview()
    {
        if (dragPreviewRoot != null)
        {
            dragPreviewRoot.gameObject.SetActive(false);
        }

        // 드래그가 끝난 뒤 PreviewIcon이 화면에 남지 않도록,
        // Preview Root뿐 아니라 실제 아이콘 오브젝트도 함께 끔
        if (dragPreviewIconImage != null)
        {
            dragPreviewIconImage.gameObject.SetActive(false);
        }
    }
}
