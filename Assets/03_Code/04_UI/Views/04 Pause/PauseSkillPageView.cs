using System.Collections.Generic;
using System;
using System.Linq;
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
    [SerializeField] private Canvas rootCanvas;
    [SerializeField] private RectTransform dragPreviewRoot;
    [SerializeField] private Image dragPreviewIconImage;
    [SerializeField] private Image ownedSkillLockImage;

    private bool isOwnedSkillListUnlocked;

    [Serializable]
    private struct OwnedSkillSlotBinding
    {
        public GameObject RootObject;
        public PauseSkillInfoView InfoView;
        public PauseSkillHoverPreviewView HoverView;
        public PauseSkillDragView DragView;
    }

    [SerializeField] private OwnedSkillSlotBinding[] ownedSkillSlots;

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
                    UIPauseSkillInfoData skillData = currentEquippedActiveSkills[i];

                    hoverView.Setup(this, skillData, skillData.IsUnlocked);
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
        for (int i = 0; i < ownedSkillSlots.Length; i++)
        {
            OwnedSkillSlotBinding binding = ownedSkillSlots[i];

            if (binding.RootObject == null) continue;

            binding.RootObject.SetActive(true);

            UIPauseSkillInfoData skillData =
                currentOwnedSkills != null && i < currentOwnedSkills.Length
                ? currentOwnedSkills[i]
                : CreateLockedPlaceholderData();

            bool canInteract =
                isOwnedSkillListUnlocked &&
                skillData.SkillId >= 0 &&
                skillData.IsUnlocked;

            if (binding.InfoView != null)
            {
                binding.InfoView.Setup(skillData);
                binding.InfoView.SetDetailVisible(false);
            }

            if (binding.HoverView != null)
            {
                binding.HoverView.Setup(
                    this,
                    skillData,
                    canInteract);
            }

            if (binding.DragView != null)
            {
                binding.DragView.SetupDragVisualRefs(
                    rootCanvas,
                    dragPreviewRoot,
                    dragPreviewIconImage);

                binding.DragView.SetupOwnedSkill(
                    skillData.SkillId,
                    i,
                    canInteract);
            }
        }
    }

    private UIPauseSkillInfoData CreateLockedPlaceholderData()
    {
        return new UIPauseSkillInfoData(
            null, string.Empty, 0, string.Empty, false, -1, false);
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
        SetHoverText(hoverSkillDescriptionText, BuildSkillEffectDescription(skillData.SkillId, skillData.Level));
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

    private string BuildSkillEffectDescription(int skillId, int level)
    {
        BaseSkillData[] skillDatas = Resources.LoadAll<BaseSkillData>("Datas/Skills");

        BaseSkillData skillData = null;

        foreach (BaseSkillData data in skillDatas)
        {
            if (data != null && data.Id == skillId)
            {
                skillData = data;
                break;
            }
        }

        if (skillData == null) return string.Empty;

        if (skillData is ActiveSkillData activeSkillData)
            return BuildActiveSkillEffectDescription(activeSkillData, level);

        if (skillData is PassiveSkillData passiveSkillData)
            return BuildPassiveSkillEffectDescription(passiveSkillData, level);

        return string.Empty;
    }

    private string BuildActiveSkillEffectDescription(ActiveSkillData skillData, int level)
    {
        ActiveSkillLevelData levelData = skillData.GetLevelData(level);

        if (levelData == null) return string.Empty;

        List<string> lines = new();

        if (levelData is ProjectileSkillLevelData projectileData)
        {
            AddEffectNumberLine(lines, "피해량", projectileData.GetDamage());
            AddEffectSecondsLine(lines, "쿨타임", projectileData.MaxCoolTime);

            if (projectileData.ProjectileCount > 1)
            {
                AddEffectLine(lines, "발사 횟수", projectileData.ProjectileCount.ToString());
            }

            if (projectileData.PenetrationCount < 0)
            {
                AddEffectLine(lines, "관통 횟수", "무한");
            }
            else if (projectileData.PenetrationCount > 0)
            {
                AddEffectLine(lines, "관통 횟수", projectileData.PenetrationCount.ToString());
            }

            AddEffectSecondsLine(lines, "차징 시간", projectileData.MaxChargingTime);
            AddEffectSecondsLine(lines, "지속시간", projectileData.MaxDuration);
        }
        else if (levelData is AreaSkillLevelData areaData)
        {
            AddAreaSkillEffectLines(lines, areaData);
        }

        return string.Join("\n", lines);
    }

    private string BuildPassiveSkillEffectDescription(PassiveSkillData skillData, int level)
    {
        PassiveSkillLevelData levelData = skillData.GetLevelData(level);

        if (levelData == null) return string.Empty;

        List<string> lines = new();

        foreach (StatAdjustment stat in levelData.GetAppliedStats())
        {
            float value = stat.modify == MODIFY_TYPE.Addition
                ? stat.amount
                : -stat.amount;

            string sign = value > 0f ? "+" : string.Empty;

            AddEffectLine(lines, GetStatDisplayName(stat.stat), $"{sign}{FormatEffectNumber(value)}");
        }

        return string.Join("\n", lines);
    }

    private void AddAreaSkillEffectLines(List<string> lines, AreaSkillLevelData areaData)
    {
        if (areaData.Stages == null || areaData.Stages.Count == 0) return;

        float minDamage = float.MaxValue;
        float maxDamage = float.MinValue;
        float maxDistance = 0f;
        float minTickInterval = float.MaxValue;
        bool hasDamage = false;
        bool hasTickInterval = false;

        foreach (AreaSkillStageData stage in areaData.Stages)
        {
            if (stage.damage > 0f)
            {
                minDamage = Mathf.Min(minDamage, stage.damage);
                maxDamage = Mathf.Max(maxDamage, stage.damage);
                hasDamage = true;
            }

            maxDistance = Mathf.Max(maxDistance, stage.distance);

            if (stage.tickInterval > 0f)
            {
                minTickInterval = Mathf.Min(minTickInterval, stage.tickInterval);

                hasTickInterval = true;
            }
        }

        if (hasDamage)
        {
            string damageText = Mathf.Approximately(minDamage, maxDamage)
                ? FormatEffectNumber(maxDamage)
                : $"{FormatEffectNumber(minDamage)}~{FormatEffectNumber(maxDamage)}";

            AddEffectLine(lines, "피해량", damageText);
        }

        AddEffectSecondsLine(lines, "쿨타임", areaData.MaxCoolTime);
        AddEffectSecondsLine(lines, "지속시간", areaData.MaxDuration);

        if (hasTickInterval)
        {
            AddEffectSecondsLine(lines, "타격 간격", minTickInterval);
        }

        if (maxDistance > 0f)
        {
            AddEffectLine(lines, "범위", $"플레이어 전방 약 {FormatEffectNumber(maxDistance)}");
        }
    }

    private void AddEffectLine(List<string> lines, string label, string value)
    {
        if (string.IsNullOrEmpty(value)) return;

        lines.Add($"{label}: {value}");
    }

    private void AddEffectNumberLine(List<string> lines, string label, float value)
    {
        if (value <= 0f) return;

        AddEffectLine(lines, label, FormatEffectNumber(value));
    }

    private void AddEffectSecondsLine(List<string> lines, string label, float value)
    {
        if (value <= 0f) return;

        AddEffectLine(lines, label, $"{FormatEffectNumber(value)}초");
    }

    private string FormatEffectNumber(float value)
    {
        return value.ToString("0.##");
    }

    private string GetStatDisplayName(STAT_TYPE statType)
    {
        return statType switch
        {
            STAT_TYPE.Health => "체력",
            STAT_TYPE.AtkPower => "공격력",
            STAT_TYPE.MoveSpeed => "이동 속도",
            STAT_TYPE.AtkSpeed => "공격 속도",
            _ => statType.ToString()
        };
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
