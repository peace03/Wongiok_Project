using System.Collections.Generic;
using System.Collections;
using System.Linq;
using UnityEngine;

public class PrototypeTestScene : MonoBehaviour
{
    [SerializeField] private Sprite fireIcon;
    [SerializeField] private Sprite iceIcon;
    [SerializeField] private Sprite dashIcon;
    [SerializeField] private Sprite powerUpIcon;
    [SerializeField] private string mainMenuSceneName = "Lobby";

    // 테스트용 Pause 스킬 데이터를 캐싱해서 드래그 교체 결과를 유지
    // 정식 스킬 시스템 연동 전까지 임시 브릿지 데이터 역할
    private UIPauseSkillInfoData[] currentActiveSkills;
    private UIPauseSkillInfoData[] currentPassiveSkills;
    private UIPauseSkillInfoData[] currentOwnedSkills;

    [SerializeField] private float testSkillCooldownDuration = 3f;

    private static readonly string[] SkillKeyTexts = { "A", "S", "D" };

    private float[] skillCooldownRemaining = new float[3];
    private float[] skillCooldownDuration = new float[3];

    private void OnEnable()
    {
        // 보유 스킬을 장착 슬롯에 드롭했을 때 테스트 데이터 교체를 처리하기 위한 이벤트 구독
        EventBus<UIOpenOverlayEvent>.action += HandleOpenOverlay;
        // 장착 슬롯끼리 드래그 했을 때 테스트 데이터 스왑을 처리하기 위한 이벤트 구독
        EventBus<UIPauseMainMenuRequestedEvent>.action += HandlePauseMainMenuRequested;
        EventBus<UIPauseQuitGameRequestedEvent>.action += HandlePauseOuitGameRequested;
        EventBus<UIPauseSkillEquipRequestedEvent>.action += HandlePauseSkillEquipRequested;
        EventBus<UIPauseSkillSwapRequestedEvent>.action += HandlePauseSkillSwapRequested;
        EventBus<UIPauseSkillUnequipRequestedEvent>.action += HandlePauseSkillUnequipRequsted;
        EventBus<UILevelUpSkillSelectedEvent>.action += HandleLevelUpSkillSelected;
        EventBus<TestRestoreSkillCheckpointEvent>.action += HandleRestoreSkillCheckpoint;
        EventBus<TestPlayerSkillUsedEvent>.action += HandleTestPlayerSkillUsed;
    }

    private void OnDisable()
    {
        EventBus<UIOpenOverlayEvent>.action -= HandleOpenOverlay;
        EventBus<UIPauseMainMenuRequestedEvent>.action -= HandlePauseMainMenuRequested;
        EventBus<UIPauseQuitGameRequestedEvent>.action -= HandlePauseOuitGameRequested;
        EventBus<UIPauseSkillEquipRequestedEvent>.action -= HandlePauseSkillEquipRequested;
        EventBus<UIPauseSkillSwapRequestedEvent>.action -= HandlePauseSkillSwapRequested;
        EventBus<UIPauseSkillUnequipRequestedEvent>.action -= HandlePauseSkillUnequipRequsted;
        EventBus<UILevelUpSkillSelectedEvent>.action -= HandleLevelUpSkillSelected;
        EventBus<TestRestoreSkillCheckpointEvent>.action -= HandleRestoreSkillCheckpoint;
        EventBus<TestPlayerSkillUsedEvent>.action -= HandleTestPlayerSkillUsed;
    }

    private void Start()
    {
        PublishInGameHudData();
        PublishPauseTestData();
    }

    private void Update()
    {
        TickSkillCooldowns();
    }

    private void HandleOpenOverlay(UIOpenOverlayEvent eventData)
    {
        if (eventData.OverlayState != UIOverlayState.Pause)
            return;

        StartCoroutine(PublishPauseDataNextFrame());
    }
    private void HandlePauseMainMenuRequested(UIPauseMainMenuRequestedEvent eventData)
    {
        Time.timeScale = 1f;
        PrototypeGameSession.ReturnToMainMenu();
        UnityEngine.SceneManagement.SceneManager.LoadScene(mainMenuSceneName);
    }

    private void HandlePauseOuitGameRequested(UIPauseQuitGameRequestedEvent eventData)
    {
        Time.timeScale = 1f;

    #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
    #else
        Application.Quit();
    #endif
    }

    private IEnumerator PublishPauseDataNextFrame()
    {
        yield return null;

        PublishPauseTestData();
    }

    private void PublishInGameHudData()
    {
        EventBus<UIChangeScreenEvent>.Publish(
            new UIChangeScreenEvent(UIScreenState.InGame));
    }

    private void PublishPauseTestData()
    {
        EnsurePauseTestData();
        PublishCurrentPauseData();
    }

    private void EnsurePauseTestData()
    {
        if (currentActiveSkills != null && currentOwnedSkills != null)
            return;

        PrototypeProgressSnapshot snapshot = PrototypeGameSession.GetChapterStart();

        BaseSkillData[] skillDatas = Resources.LoadAll<BaseSkillData>("Datas/Skills");

        currentActiveSkills = new UIPauseSkillInfoData[3];

        for (int i = 0; i < currentActiveSkills.Length; i++)
            currentActiveSkills[i] = CreateEmptySkillData();

        List<UIPauseSkillInfoData> ownedSkills = new();

        foreach (PrototypeSkillState state in snapshot.Skills)
        {
            BaseSkillData data = skillDatas.FirstOrDefault(skill => skill.Id == state.SkillId);

            if (data == null)
                continue;

            bool isEquipped = state.SlotIndex >= 0 && state.SlotIndex < currentActiveSkills.Length;

            UIPauseSkillInfoData uiData = new(
                data.Icon,
                data.SkillName,
                Mathf.Max(1, state.Level),
                data.Desc,
                isEquipped,
                data.Id);

            if (isEquipped)
                currentActiveSkills[state.SlotIndex] = uiData;
            else
                ownedSkills.Add(uiData);
        }

        currentOwnedSkills = ownedSkills.ToArray();
        currentPassiveSkills = System.Array.Empty<UIPauseSkillInfoData>();
    }

    private void PublishCurrentPauseData()
    {
        EventBus<UISetPauseSkillPageEvent>.Publish(
            new UISetPauseSkillPageEvent(
                currentActiveSkills,
                currentOwnedSkills));

        EventBus<RefreshUIEventT>.Publish(
            new RefreshUIEventT(
                currentActiveSkills,
                currentOwnedSkills));

        PublishCurrentPlayerSkillSlots();
    }

    private bool TryLevelUpSkill(UIPauseSkillInfoData[] skills, int skillId)
    {
        const int maxSkillLevel = 3;

        int skillIndex = FindSkillIndex(skills, skillId);

        if (skillIndex < 0) return false;

        UIPauseSkillInfoData skill = skills[skillIndex];

        if (skill.Level >= maxSkillLevel) return false;

        skills[skillIndex] = new UIPauseSkillInfoData(
            skill.Icon,
            skill.SkillName,
            Mathf.Min(skill.Level + 1, maxSkillLevel),
            skill.Description,
            skill.IsEquipped,
            skill.SkillId);

        return true;
    }

    private void HandlePauseSkillEquipRequested(UIPauseSkillEquipRequestedEvent eventData)
    {
        EnsurePauseTestData();

        if (!IsValidSlotIndex(eventData.TargetSlotIndex))
            return;

        int ownedIndex = FindSkillIndex(currentOwnedSkills, eventData.SkillId);

        if (ownedIndex < 0)
            return;

        UIPauseSkillInfoData selectedOwnedSkill = currentOwnedSkills[ownedIndex];
        UIPauseSkillInfoData previousEquippedSkill = currentActiveSkills[eventData.TargetSlotIndex];

        currentActiveSkills[eventData.TargetSlotIndex] = SetEquipped(selectedOwnedSkill, true);

        List<UIPauseSkillInfoData> nextOwnedSkills = currentOwnedSkills.ToList();

        nextOwnedSkills.RemoveAt(ownedIndex);

        if (previousEquippedSkill.SkillId >= 0)
        {
            nextOwnedSkills.Add(SetEquipped(previousEquippedSkill, false));
        }

        currentOwnedSkills = nextOwnedSkills.ToArray();

        ResetSkillCooldown(eventData.TargetSlotIndex);
        PublishCurrentPauseData();
    }

    private void HandlePauseSkillSwapRequested(UIPauseSkillSwapRequestedEvent eventData)
    {
        EnsurePauseTestData();

        if (!IsValidSlotIndex(eventData.SourceSlotIndex) || !IsValidSlotIndex(eventData.TargetSlotIndex))
            return;

        if (eventData.SourceSlotIndex == eventData.TargetSlotIndex)
            return;

        UIPauseSkillInfoData sourceSkill = currentActiveSkills[eventData.SourceSlotIndex];
        UIPauseSkillInfoData targetSkill = currentActiveSkills[eventData.TargetSlotIndex];

        currentActiveSkills[eventData.SourceSlotIndex] = SetEquipped(targetSkill, true);
        currentActiveSkills[eventData.TargetSlotIndex] = SetEquipped(sourceSkill, true);

        ResetSkillCooldown(eventData.SourceSlotIndex);
        ResetSkillCooldown(eventData.TargetSlotIndex);
        PublishCurrentPauseData();
    }

    private void HandlePauseSkillUnequipRequsted(UIPauseSkillUnequipRequestedEvent eventData)
    {
        EnsurePauseTestData();

        if (!IsValidSlotIndex(eventData.SourceSlotIndex))
            return;

        UIPauseSkillInfoData unequippedSkill = currentActiveSkills[eventData.SourceSlotIndex];

        if (unequippedSkill.SkillId < 0)
            return;

        currentActiveSkills[eventData.SourceSlotIndex] = CreateEmptySkillData();
        AddOwnedSkill(SetEquipped(unequippedSkill, false));

        ResetSkillCooldown(eventData.SourceSlotIndex);
        PublishCurrentPauseData();
    }

    private void HandleLevelUpSkillSelected(UILevelUpSkillSelectedEvent eventData)
    {
        EnsurePauseTestData();

        if (TryLevelUpSkill(currentActiveSkills, eventData.SkillId) ||
            TryLevelUpSkill(currentOwnedSkills, eventData.SkillId))
        {
            PublishCurrentPauseData();
        }
    }

    private void HandleRestoreSkillCheckpoint(
        TestRestoreSkillCheckpointEvent eventData)
    {
        currentActiveSkills = eventData.EquippedSkills == null
            ? System.Array.Empty<UIPauseSkillInfoData>()
            : eventData.EquippedSkills.ToArray();

        currentOwnedSkills = eventData.OwnedSkills == null
            ? System.Array.Empty<UIPauseSkillInfoData>()
            : eventData.OwnedSkills.ToArray();

        ResetSkillCooldowns();
        PublishCurrentPauseData();
    }

    private void PublishCurrentPlayerSkillSlots()
    {
        EnsurePauseTestData();
        EnsureCooldownArrays();

        UIPlayerSkillSlotData[] skillSlots = new UIPlayerSkillSlotData[currentActiveSkills.Length];

        for (int i = 0; i < currentActiveSkills.Length; i++)
        {
            UIPauseSkillInfoData skill = currentActiveSkills[i];

            string keyText = i < SkillKeyTexts.Length
                ? SkillKeyTexts[i]
                : string.Empty;

            if (skill.SkillId >= 0)
            {
                float remaining = Mathf.Max(0f, skillCooldownRemaining[i]);
                float duration = Mathf.Max(0.01f, skillCooldownDuration[i]);
                float cooldownProgress = Mathf.Clamp01(remaining / duration);
                bool isAvailable = remaining <= 0f;

                skillSlots[i] = new UIPlayerSkillSlotData(
                    skill.Icon,
                    keyText,
                    skill.Level,
                    cooldownProgress,
                    isAvailable);
            }
            else
            {
                skillSlots[i] = new UIPlayerSkillSlotData(
                    null,
                    keyText,
                    0,
                    0f,
                    false);
            }
        }

        EventBus<UISetPlayerSkillSlotsEvent>.Publish(
            new UISetPlayerSkillSlotsEvent(skillSlots));
    }

    private void HandleTestPlayerSkillUsed(TestPlayerSkillUsedEvent eventData)
    {
        EnsurePauseTestData();
        EnsureCooldownArrays();

        if (!IsValidSlotIndex(eventData.SlotIndex)) return;

        UIPauseSkillInfoData skill = currentActiveSkills[eventData.SlotIndex];

        if (skill.SkillId < 0) return;

        if (skillCooldownRemaining[eventData.SlotIndex] > 0f) return;

        skillCooldownDuration[eventData.SlotIndex] = Mathf.Max(0.01f, testSkillCooldownDuration);

        skillCooldownRemaining[eventData.SlotIndex] = skillCooldownDuration[eventData.SlotIndex];

        PublishCurrentPlayerSkillSlots();
    }

    private void TickSkillCooldowns()
    {
        if (skillCooldownRemaining == null) return;

        bool hasChanged = false;

        for (int i = 0; i < skillCooldownRemaining.Length; i++)
        {
            if (skillCooldownRemaining[i] <= 0f) continue;

            skillCooldownRemaining[i] = Mathf.Max(0f, skillCooldownRemaining[i] - Time.deltaTime);

            hasChanged = true;
        }

        if (hasChanged) PublishCurrentPlayerSkillSlots();
    }

    private void EnsureCooldownArrays()
    {
        int slotCount = currentActiveSkills == null
            ? 3
            : currentActiveSkills.Length;

        if (skillCooldownRemaining != null &&
            skillCooldownRemaining.Length == slotCount &&
            skillCooldownDuration != null &&
            skillCooldownDuration.Length == slotCount)
        {
            return;
        }

        skillCooldownRemaining = new float[slotCount];
        skillCooldownDuration = new float[slotCount];

        for (int i = 0; i < skillCooldownDuration.Length; i++)
            skillCooldownDuration[i] = Mathf.Max(0.01f, testSkillCooldownDuration);
    }

    private void ResetSkillCooldowns()
    {
        EnsureCooldownArrays();

        for (int i = 0; i < skillCooldownRemaining.Length; i++)
            ResetSkillCooldown(i);
    }

    private void ResetSkillCooldown(int slotIndex)
    {
        EnsureCooldownArrays();

        if (slotIndex < 0 || slotIndex >= skillCooldownRemaining.Length) return;

        skillCooldownRemaining[slotIndex] = 0f;
        skillCooldownDuration[slotIndex] = Mathf.Max(0.01f, testSkillCooldownDuration);
    }

    private bool IsValidSlotIndex(int slotIndex)
    {
        return currentActiveSkills != null && slotIndex >= 0 && slotIndex < currentActiveSkills.Length;
    }

    private int FindSkillIndex(UIPauseSkillInfoData[] skills, int skillId)
    {
        if (skills == null)
            return -1;

        for (int i = 0; i < skills.Length; i++)
        {
            if (skills[i].SkillId == skillId)
                return i;
        }

        return -1;
    }

    private void AddOwnedSkill(UIPauseSkillInfoData skillData)
    {
        if (currentOwnedSkills == null)
        {
            currentOwnedSkills = new UIPauseSkillInfoData[] { skillData };
            return;
        }

        for (int i = 0; i < currentOwnedSkills.Length; i++)
        {
            if (currentOwnedSkills[i].SkillId < 0)
            {
                currentOwnedSkills[i] = skillData;
                return;
            }
        }

        UIPauseSkillInfoData[] nextOwnedSkills = new UIPauseSkillInfoData[currentOwnedSkills.Length + 1];

        for (int i = 0; i < currentOwnedSkills.Length; i++)
        {
            nextOwnedSkills[i] = currentOwnedSkills[i];
        }

        nextOwnedSkills[nextOwnedSkills.Length - 1] = skillData;
        currentOwnedSkills = nextOwnedSkills;
    }

    private UIPauseSkillInfoData SetEquipped(UIPauseSkillInfoData skillData, bool isEquipped)
    {
        return new UIPauseSkillInfoData(
            skillData.Icon,
            skillData.SkillName,
            skillData.Level,
            skillData.Description,
            isEquipped,
            skillData.SkillId);
    }

    private UIPauseSkillInfoData CreateSkillData(BaseSkillData skillData, bool isEquipped)
    {
        if (skillData == null)
            return CreateEmptySkillData();

        return new UIPauseSkillInfoData(
            skillData.Icon,
            skillData.SkillName,
            1,
            skillData.Desc,
            isEquipped,
            skillData.Id);
    }

    private UIPauseSkillInfoData CreateEmptySkillData()
    {
        return new UIPauseSkillInfoData(
            null,
            string.Empty,
            0,
            string.Empty,
            false,
            -1);
    }
}
