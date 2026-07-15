using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// 일시정지 메뉴의 스킬 페이지 담당
// 스킬 데이터를 화면에 표시
public class PauseSkillPageView : MonoBehaviour
{
    // 현재 장착 중인 액티브 스킬 슬롯 View 배열
    [Header("Equipped Active Skills")]
    [SerializeField] private PauseSkillInfoView[] equippedActiveSkillViews;

    [Header("보유 스킬 목록")]
    // 보유 중인 교체 가능 스킬 목록 View
    [SerializeField] private Transform ownedSkillContentRoot;
    [SerializeField] private GameObject ownedSkillItemPrefab;
    [SerializeField] private Canvas rootCanvas;
    [SerializeField] private RectTransform dragPreviewRoot;
    [SerializeField] private Image dragPreviewIconImage;
    [SerializeField] private int ownedSkillPoolMaxSize = 100;
    [SerializeField] private Image ownedSkillLockImage;

    private PauseSkillOwnedDropView ownedSkillDropView;
    private bool isOwnedSkillListUnlocked;

    private UnityEngine.Pool.IObjectPool<GameObject> ownedSkillPool;
    private readonly List<GameObject> activeOwnedSkillObjects = new();

    // 보유 중인 스킬 없을 때 표시할 빈 상태 안내 옵젝
    [SerializeField] private GameObject emptyOwnedSkillObject;

    [Header("Selected Skill Detail")]
    // 스킬 상세 정보 표시
    [SerializeField] private PauseSkillInfoView selectedSkillDetailView;

    [Header("Drag Items")]
    [SerializeField] private PauseSkillDragView[] equippedActiveDragItems;

    [Header("Drop Slots")]
    [SerializeField] private PauseSkillSlotDropView[] equippedSlotDropViews;

    [Header("스킬 오버 프리뷰")]
    [SerializeField] private GameObject hoverPreviewRoot;
    [SerializeField] private Text hoverSkillNameText;
    [SerializeField] private Text hoverSkillLevelText;
    [SerializeField] private Text hoverSkillDescriptionText;

    // 스킬 탭 재접근 시 초기화를 하기 위함
    [Header("캐릭터 프리뷰")]
    [SerializeField] private PauseCharacterPreviewView characterPreviewView;
    [SerializeField] private CommonButtonView characterPreviewResetButton;

    // 액티브 스킬 데이터
    private UIPauseSkillInfoData[] currentEquippedActiveSkills;
    // 보유 스킬 데이터
    private UIPauseSkillInfoData[] currentOwnedSkills;

    // 선택된 스킬 정보가 있는지 확인
    private bool hasSelectedSkill;

    // 선택 스킬 상세 데이터
    private UIPauseSkillInfoData currentSelectedSkill;

    private void Awake()
    {
        if (ownedSkillContentRoot != null)
        {
            ownedSkillDropView = ownedSkillContentRoot.GetComponent<PauseSkillOwnedDropView>();
        }

        ownedSkillPool = CustomObjectPool.CreatePool(
            ownedSkillItemPrefab,
            ownedSkillPoolMaxSize,
            ownedSkillContentRoot);

        SubscribeEvents();
    }

    private void OnEnable()
    {
        SetupCharacterPreviewButtons();
        RefreshAll();

        if (characterPreviewView != null)
        {
            characterPreviewView.ResetPreview();
        }
    }

    private void OnDisable()
    {
        ClearCharacterPreviewButtons();
    }

    private void OnDestroy()
    {
        ClearCharacterPreviewButtons();
        UnsubscribeEvents();
    }

    private void SubscribeEvents()
    {
        EventBus<UISetPauseSkillPageEvent>.action += HandleSetPauseSkillPage;
        EventBus<RefreshUIEventT>.action += HandleRefreshUIEvent;
        EventBus<UIResetEvent>.action += HandleReset;
    }

    private void UnsubscribeEvents()
    {
        EventBus<UISetPauseSkillPageEvent>.action -= HandleSetPauseSkillPage;
        EventBus<RefreshUIEventT>.action -= HandleRefreshUIEvent;
        EventBus<UIResetEvent>.action -= HandleReset;
    }

    // 외부 스킬 시스템에서 전달한 스킬 페이지 표시 데이터를 캐싱하고 화면 갱신
    private void HandleSetPauseSkillPage(UISetPauseSkillPageEvent eventData)
    {
        currentEquippedActiveSkills = eventData.EquippedActiveSkills;
        currentOwnedSkills = eventData.OwnedSkills;
        hasSelectedSkill = eventData.HasSelectedSkills;
        currentSelectedSkill = eventData.SelectedSkill;
        isOwnedSkillListUnlocked = eventData.IsOwnedSkillListUnlocked;

        RefreshAll();
    }

    // 임시 테스트
    private void HandleRefreshUIEvent(RefreshUIEventT eventData)
    {
        currentEquippedActiveSkills = eventData.EquippedActiveSkills;
        currentOwnedSkills = eventData.OwnedSkills;
        hasSelectedSkill = false;
        currentSelectedSkill = default;

        RefreshAll();
    }

    private void HandleReset(UIResetEvent eventData)
    {
        ResetSkillPage();
    }

    private void ResetSkillPage()
    {
        currentEquippedActiveSkills = null;
        currentOwnedSkills = null;
        hasSelectedSkill = false;
        currentSelectedSkill = default;
        isOwnedSkillListUnlocked = false;

        RefreshAll();
    }

    private void RefreshAll()
    {
        RefreshEquippedActiveSkills();
        RefreshOwnedSkillLock();
        RefreshOwnedSkills();
        RefreshSelectedSkillDetail();
        HideHoverPreview();
    }

    private void RefreshOwnedSkillLock()
    {
        bool isLocked = !isOwnedSkillListUnlocked;

        if (ownedSkillLockImage != null)
        {
            ownedSkillLockImage.gameObject.SetActive(isLocked);
        }

        if (ownedSkillDropView != null)
        {
            ownedSkillDropView.enabled = !isLocked;
        }
    }

    // 캐릭터 모델 프리뷰 초기화 전용
    private void SetupCharacterPreviewButtons()
    {
        if (characterPreviewResetButton != null)
        {
            characterPreviewResetButton.Setup("리셋", HandleCharacterPreviewResetClicked);
        }
    }

    private void ClearCharacterPreviewButtons()
    {
        if (characterPreviewResetButton != null)
        {
            characterPreviewResetButton.Clear();
        }
    }

    private void HandleCharacterPreviewResetClicked()
    {
        if (characterPreviewView != null)
        {
            characterPreviewView.ResetPreview();
        }
    }

    // 장착 중인 액티브 스킬 슬롯 표시 갱신
    private void RefreshEquippedActiveSkills()
    {
        RefreshEquippedSkillViews();
        RefreshEquippedDragItems();
        RefreshDropSlots();
    }

    private void RefreshEquippedSkillViews()
    {
        if (equippedActiveSkillViews == null)
            return;

        for (int i = 0; i < equippedActiveSkillViews.Length; i++)
        {
            if (equippedActiveSkillViews[i] == null)
                continue;

            bool hasData = currentEquippedActiveSkills != null &&
                i < currentEquippedActiveSkills.Length &&
                currentEquippedActiveSkills[i].SkillId >= 0;

            equippedActiveSkillViews[i].gameObject.SetActive(true);

            if (hasData)
            {
                equippedActiveSkillViews[i].Setup(currentEquippedActiveSkills[i]);
                equippedActiveSkillViews[i].SetDetailVisible(false);

                if (equippedActiveSkillViews[i].TryGetComponent(out PauseSkillHoverPreviewView hoverView))
                {
                    hoverView.Setup(this, currentEquippedActiveSkills[i]);
                }
            }
            else
            {
                equippedActiveSkillViews[i].Clear();
                if (equippedActiveSkillViews[i].TryGetComponent(out PauseSkillHoverPreviewView hoverView))
                {
                    hoverView.Clear();
                }
            }
        }
    }

    // 보유 중인 교체 가능 스킬 목록 표시 갱신
    private void RefreshOwnedSkills()
    {
        ClearOwnedSkillItems();

        if (!isOwnedSkillListUnlocked)
        {
            if (emptyOwnedSkillObject != null)
            {
                emptyOwnedSkillObject.SetActive(false);
            }

            return;
        }

        bool hasOwnedSkills = currentOwnedSkills != null && currentOwnedSkills.Length > 0;
        if (emptyOwnedSkillObject != null)
            emptyOwnedSkillObject.SetActive(!hasOwnedSkills);

        if (!hasOwnedSkills || ownedSkillPool == null)
            return;

        foreach (var skillData in currentOwnedSkills)
        {
            if (skillData.SkillId < 0)
                continue;

            GameObject itemObject = ownedSkillPool.Get();
            activeOwnedSkillObjects.Add(itemObject);

            itemObject.transform.SetParent(ownedSkillContentRoot, false);

            if (itemObject.TryGetComponent(out PauseSkillInfoView infoView))
            {
                infoView.Setup(skillData);
                infoView.SetDetailVisible(false);
            }

            if (itemObject.TryGetComponent(out PauseSkillHoverPreviewView hoverView))
            {
                hoverView.Setup(this, skillData);
            }

            if (itemObject.TryGetComponent(out PauseSkillDragView dragView))
            {
                dragView.SetupDragVisualRefs(rootCanvas, dragPreviewRoot, dragPreviewIconImage);
                dragView.SetupOwnedSkill(skillData.SkillId);
            }
        }
    }

    private void ClearOwnedSkillItems()
    {
        for (int i = 0; i < activeOwnedSkillObjects.Count; i++)
        {
            GameObject itemObject = activeOwnedSkillObjects[i];

            if (itemObject.TryGetComponent(out PauseSkillInfoView infoView))
                infoView.Clear();

            if (itemObject.TryGetComponent(out PauseSkillHoverPreviewView hoverView))
                hoverView.Clear();

            if (itemObject.TryGetComponent(out PauseSkillDragView dragView))
                dragView.ClearDragData();

            ownedSkillPool.Release(itemObject);
        }

        activeOwnedSkillObjects.Clear();
    }

    // 선택된 스킬 상세 정보 영역 갱신
    private void RefreshSelectedSkillDetail()
    {
        if (selectedSkillDetailView == null)
            return;

        if (hasSelectedSkill)
        {
            selectedSkillDetailView.gameObject.SetActive(true);
            selectedSkillDetailView.Setup(currentSelectedSkill);
            return;
        }

        selectedSkillDetailView.Clear();
        selectedSkillDetailView.gameObject.SetActive(false);
    }

    public void ShowHoverPreview(UIPauseSkillInfoData skillData)
    {
        if (skillData.SkillId < 0)
        {
            HideHoverPreview();
            return;
        }

        if (hoverPreviewRoot != null)
        {
            hoverPreviewRoot.SetActive(true);
        }

        SetHoverText(hoverSkillNameText, skillData.SkillName);
        SetHoverText(hoverSkillLevelText, $"Lv.{skillData.Level}");
        SetHoverText(hoverSkillDescriptionText, skillData.Description);
    }

    public void HideHoverPreview()
    {
        if (hoverPreviewRoot != null)
        {
            hoverPreviewRoot.SetActive(false);
        }
    }

    private void SetHoverText(Text targetText, string value)
    {
        if (targetText == null) return;

        targetText.text = value;
    }

    private void RefreshEquippedDragItems()
    {
        if (equippedActiveDragItems == null)
            return;

        for (int i = 0; i < equippedActiveDragItems.Length; i++)
        {
            if (equippedActiveDragItems[i] == null)
                continue;

            bool hasData = currentEquippedActiveSkills != null && i < currentEquippedActiveSkills.Length;
            int skillId = hasData ? currentEquippedActiveSkills[i].SkillId : -1;

            if (skillId >= 0)
            {
                equippedActiveDragItems[i].SetupEquippedSlot(skillId, i);
            }
            else
            {
                equippedActiveDragItems[i].ClearDragData();
            }
        }
    }

    private void RefreshDropSlots()
    {
        if (equippedSlotDropViews == null)
            return;

        for (int i = 0; i < equippedSlotDropViews.Length; i++)
        {
            if (equippedSlotDropViews[i] == null)
                continue;

            equippedSlotDropViews[i].Setup(i);
        }
    }
}
