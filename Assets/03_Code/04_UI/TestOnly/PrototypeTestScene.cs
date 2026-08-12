using System.Collections.Generic;
using System.Collections;
using System.Globalization;
using System.Linq;
using UnityEngine;
using UnityEngine.Video;

public class PrototypeTestScene : MonoBehaviour, IInitializable
{
    [SerializeField] private string mainMenuSceneName = "Lobby";

    // 테스트용 Pause 스킬 데이터를 캐싱해서 드래그 교체 결과를 유지
    // 정식 스킬 시스템 연동 전까지 임시 브릿지 데이터 역할
    private UIPauseSkillInfoData[] currentActiveSkills;
    private UIPauseSkillInfoData[] currentPassiveSkills;
    private UIPauseSkillInfoData[] currentOwnedSkills;

    [Header("액티브 스킬 카탈로그")]
    [SerializeField] private ActiveSkillData[] activeSkillCatalog = new ActiveSkillData[21];

    private const int EquippedSkillSlotCount = 3;
    private const int OwnedSkillSlotCount = 18;

    private static readonly string[] SkillKeyTexts = { "A", "S", "D" };

    private float[] skillCooldownRemaining = new float[3];
    private float[] skillCooldownDuration = new float[3];

    public int Priority => (int)InitOrder.UI;

    // 2026.08.10_UI 정리: 비활성화 시 등록한 이벤트와 임시 UI 상태를 정리한다.
    private void OnDisable()
    {
        EventBus<UIOpenOverlayEvent>.action -= HandleOpenOverlay;
        EventBus<UIPauseMainMenuRequestedEvent>.action -= HandlePauseMainMenuRequested;
        EventBus<UIPauseQuitGameRequestedEvent>.action -= HandlePauseQuitGameRequested;
        EventBus<UIPauseSkillEquipRequestedEvent>.action -= HandlePauseSkillEquipRequested;
        EventBus<UIPauseSkillSwapRequestedEvent>.action -= HandlePauseSkillSwapRequested;
        EventBus<UILevelUpSkillSelectedEvent>.action -= HandleLevelUpSkillSelected;
        EventBus<TestRestoreSkillCheckpointEvent>.action -= HandleRestoreSkillCheckpoint;
        EventBus<TestPlayerSkillUsedEvent>.action -= HandleTestPlayerSkillUsed;

        EventBus<RefreshUIEvent>.action -= RefreshSkills;
    }

    // 2026.08.10_UI 정리: 초기 데이터와 화면 상태를 설정한다.
    private void Start()
    {
        PublishInGameHudData();
        PublishPauseTestData();
    }

    // 2026.08.10_UI 정리: 프레임 단위 UI 상태와 입력을 갱신한다.
    private void Update()
    {
        TickSkillCooldowns();
    }

    // 2026.08.10_UI 정리: 활성화 시 필요한 UI 상태와 이벤트 구독을 준비한다.
    public void Init()
    {
        // 보유 스킬을 장착 슬롯에 드롭했을 때 테스트 데이터 교체를 처리하기 위한 이벤트 구독
        EventBus<UIOpenOverlayEvent>.action += HandleOpenOverlay;
        // 장착 슬롯끼리 드래그 했을 때 테스트 데이터 스왑을 처리하기 위한 이벤트 구독
        EventBus<UIPauseMainMenuRequestedEvent>.action += HandlePauseMainMenuRequested;
        EventBus<UIPauseQuitGameRequestedEvent>.action += HandlePauseQuitGameRequested;
        EventBus<UIPauseSkillEquipRequestedEvent>.action += HandlePauseSkillEquipRequested;
        EventBus<UIPauseSkillSwapRequestedEvent>.action += HandlePauseSkillSwapRequested;
        EventBus<UILevelUpSkillSelectedEvent>.action += HandleLevelUpSkillSelected;
        EventBus<TestRestoreSkillCheckpointEvent>.action += HandleRestoreSkillCheckpoint;
        EventBus<TestPlayerSkillUsedEvent>.action += HandleTestPlayerSkillUsed;

        EventBus<RefreshUIEvent>.action += RefreshSkills;
    }

    // 2026.08.10_UI 정리: Open 오버레이 관련 입력 또는 EventBus 요청을 처리한다.
    private void HandleOpenOverlay(UIOpenOverlayEvent eventData)
    {
        if (eventData.OverlayState != UIOverlayState.Pause)
            return;

        StartCoroutine(PublishPauseDataNextFrame());
    }
    // 2026.08.10_UI 정리: 일시정지 메인 메뉴 Requested 관련 입력 또는 EventBus 요청을 처리한다.
    private void HandlePauseMainMenuRequested(UIPauseMainMenuRequestedEvent eventData)
    {
        Time.timeScale = 1f;
        PrototypeGameSession.ReturnToMainMenu();
        UnityEngine.SceneManagement.SceneManager.LoadScene(mainMenuSceneName);
    }

    // 2026.08.07_psb수정
    // 일시정지 옵션 탭의 종료 요청을 에디터와 빌드 환경에 맞춰 처리한다.
    private void HandlePauseQuitGameRequested(UIPauseQuitGameRequestedEvent eventData)
    {
        Time.timeScale = 1f;

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // 2026.08.10_UI 정리: 일시정지 데이터 다음 프레임 요청 또는 상태를 EventBus로 발행한다.
    private IEnumerator PublishPauseDataNextFrame()
    {
        yield return null;

        PublishPauseTestData();
    }

    // 2026.08.10_UI 정리: In 게임 HUD 데이터 요청 또는 상태를 EventBus로 발행한다.
    private void PublishInGameHudData()
    {
        EventBus<UIChangeScreenEvent>.Publish(
            new UIChangeScreenEvent(UIScreenState.InGame));
    }

    // 2026.08.10_UI 정리: 일시정지 Test 데이터 요청 또는 상태를 EventBus로 발행한다.
    private void PublishPauseTestData()
    {
        EnsurePauseTestData();
        PublishCurrentPauseData();
    }

    // 2026.08.10_UI 정리: 일시정지 Test 데이터 처리 상태가 준비되었는지 보장한다.
    private void EnsurePauseTestData()
    {
        if (currentActiveSkills != null && currentOwnedSkills != null)
            return;

        currentActiveSkills = new UIPauseSkillInfoData[3];

        for (int i = 0; i < currentActiveSkills.Length; i++)
            currentActiveSkills[i] = CreateEmptySkillData();
    }

    // 2026.08.10_UI 정리: 현재 데이터로 스킬 표시를 갱신한다.
    private void RefreshSkills(RefreshUIEvent eventData)
    {
        if(eventData.IsActiveSkill)
        {
            currentActiveSkills = eventData.EquippedSkills;
            currentOwnedSkills = eventData.OwnedSkills;
            PublishCurrentPauseData();
        }
    }

    // 2026.08.10_UI 정리: Current 일시정지 데이터 요청 또는 상태를 EventBus로 발행한다.
    private void PublishCurrentPauseData()
    {
        EventBus<UISetPauseSkillPageEvent>.Publish(
            new UISetPauseSkillPageEvent(
                currentActiveSkills,
                currentOwnedSkills,
                isOwnedSkillListUnlocked: PrototypeGameSession.CurrentChapterId >= 2));

        PublishCurrentPlayerSkillSlots();
    }

    // 2026.08.10_UI 정리: 레벨 Up 스킬 동작을 시도한다.
    private bool TryLevelUpSkill(UIPauseSkillInfoData[] skills, int skillId)
    {
        const int maxSkillLevel = 3;

        int skillIndex = FindSkillIndex(skills, skillId);

        if (skillIndex < 0) return false;

        UIPauseSkillInfoData skill = skills[skillIndex];

        if (skill.Level >= maxSkillLevel) return false;

        //BaseSkillData skillData = Resources.LoadAll<BaseSkillData>("Datas/Skills").
        //    FirstOrDefault(data => data != null && data.Id == skill.SkillId);

        int nextLevel = Mathf.Min(skill.Level + 1, maxSkillLevel);

        skills[skillIndex] = new UIPauseSkillInfoData(
            skill.Icon,
            skill.SkillName,
            nextLevel,
            skill.Description,
            skill.IsEquipped,
            skill.SkillId,
            skill.IsUnlocked);

        return true;
    }

    // 2026.08.10_UI 정리: 현재 스킬 쿨타임 Duration 값을 반환한다.
    private float GetSkillCooldownDuration(int slotIndex)
    {
        if (!IsValidSlotIndex(slotIndex)) return 0f;

        UIPauseSkillInfoData skill = currentActiveSkills[slotIndex];

        if (skill.SkillId < 0) return 0f;

        //BaseSkillData[] skillDatas = Resources.LoadAll<BaseSkillData>("Datas/Skills");
        //BaseSkillData skillData = skillDatas.FirstOrDefault(data => data != null && data.Id == skill.SkillId);
        BaseSkillData skillData = SkillDatabase.FindDataById(skill.SkillId);

        if (skillData == null) return 0f;

        return Mathf.Max(0f, skillData.GetMaxCoolTime(skill.Level));
    }

    // 2026.08.10_UI 정리: 일시정지 스킬 장착 Requested 관련 입력 또는 EventBus 요청을 처리한다.
    private void HandlePauseSkillEquipRequested(UIPauseSkillEquipRequestedEvent eventData)
    {
        EnsurePauseTestData();

        if (!IsValidSlotIndex(eventData.TargetSlotIndex))
            return;

        int ownedIndex = eventData.SourceOwnedSlotIndex;

        if (!IsValidSlotIndex(eventData.TargetSlotIndex) || ownedIndex < 0 || ownedIndex >= currentOwnedSkills.Length)
        {
            return;
        }

        UIPauseSkillInfoData selectedSkill = currentOwnedSkills[ownedIndex];

        UIPauseSkillInfoData previousSkill = currentActiveSkills[eventData.TargetSlotIndex];

        if (!selectedSkill.IsUnlocked || selectedSkill.SkillId < 0 || previousSkill.SkillId < 0)
        {
            return;
        }

        if (ownedIndex < 0)
            return;

        currentActiveSkills[eventData.TargetSlotIndex] = SetEquipped(selectedSkill, true);

        currentOwnedSkills[ownedIndex] = SetEquipped(previousSkill, false);

        ResetSkillCooldown(eventData.TargetSlotIndex);
        PublishCurrentPauseData();
    }

    // 2026.08.10_UI 정리: 일시정지 스킬 교체 Requested 관련 입력 또는 EventBus 요청을 처리한다.
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

    // 2026.08.10_UI 정리: 레벨 Up 스킬 선택 관련 입력 또는 EventBus 요청을 처리한다.
    private void HandleLevelUpSkillSelected(UILevelUpSkillSelectedEvent eventData)
    {
        EnsurePauseTestData();

        if (TryLevelUpSkill(currentActiveSkills, eventData.SkillId) ||
            TryLevelUpSkill(currentOwnedSkills, eventData.SkillId))
        {
            PublishCurrentPauseData();
        }
    }

    // 2026.08.10_UI 정리: 복원 스킬 Checkpoint 관련 입력 또는 EventBus 요청을 처리한다.
    private void HandleRestoreSkillCheckpoint(
        TestRestoreSkillCheckpointEvent eventData)
    {
        currentActiveSkills = eventData.EquippedSkills == null
            ? System.Array.Empty<UIPauseSkillInfoData>()
            : eventData.EquippedSkills.ToArray();

        RestoreOwnedSkillSlots(eventData.OwnedSkills, eventData.OwnedSkillOrder);

        ResetSkillCooldowns();
        PublishCurrentPauseData();
    }

    // 2026.08.10_UI 정리: 저장된 보유 스킬 슬롯 상태를 복원한다.
    private void RestoreOwnedSkillSlots(UIPauseSkillInfoData[] restoredOwnedSkills, int[] ownedSkillOrder)
    {
        HashSet<int> unlockedSkillIds = new HashSet<int>();

        if (restoredOwnedSkills != null)
        {
            foreach (UIPauseSkillInfoData skill in restoredOwnedSkills)
            {
                if (skill.SkillId >= 0 && skill.IsUnlocked)
                {
                    unlockedSkillIds.Add(skill.SkillId);
                }
            }
        }

        currentOwnedSkills = new UIPauseSkillInfoData[OwnedSkillSlotCount];

        for (int i = 0; i < OwnedSkillSlotCount; i++)
        {
            int skillId = ownedSkillOrder != null && i < ownedSkillOrder.Length
                            ? ownedSkillOrder[i]
                            : -1;

            ActiveSkillData skillData = FindCatalogSkillData(skillId);

            if (skillData == null)
            {
                currentOwnedSkills[i] = CreateEmptySkillData();
                continue;
            }

            bool isUnlocked = unlockedSkillIds.Contains(skillId);

            currentOwnedSkills[i] = new UIPauseSkillInfoData(
                skillData.Icon,
                skillData.SkillName,
                1,
                skillData.Desc,
                false,
                skillData.Id,
                isUnlocked);
        }
    }

    // 2026.08.10_UI 정리: Current 플레이어 스킬 슬롯 요청 또는 상태를 EventBus로 발행한다.
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

    // 2026.08.10_UI 정리: Test 플레이어 스킬 Used 관련 입력 또는 EventBus 요청을 처리한다.
    private void HandleTestPlayerSkillUsed(TestPlayerSkillUsedEvent eventData)
    {
        EnsurePauseTestData();
        EnsureCooldownArrays();

        if (!IsValidSlotIndex(eventData.SlotIndex)) return;

        UIPauseSkillInfoData skill = currentActiveSkills[eventData.SlotIndex];

        if (skill.SkillId < 0) return;

        float cooldownDuration = GetSkillCooldownDuration(eventData.SlotIndex);

        skillCooldownDuration[eventData.SlotIndex] = cooldownDuration;
        skillCooldownRemaining[eventData.SlotIndex] = cooldownDuration;

        PublishCurrentPlayerSkillSlots();
    }

    // 2026.08.10_UI 정리: 이 메서드의 UI 처리 역할을 수행한다.
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

    // 2026.08.10_UI 정리: 쿨타임 Arrays 처리 상태가 준비되었는지 보장한다.
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
            skillCooldownDuration[i] = GetSkillCooldownDuration(i);
    }

    // 2026.08.10_UI 정리: 스킬 Cooldowns 상태를 기본값으로 초기화한다.
    private void ResetSkillCooldowns()
    {
        EnsureCooldownArrays();

        for (int i = 0; i < skillCooldownRemaining.Length; i++)
            ResetSkillCooldown(i);
    }

    // 2026.08.10_UI 정리: 스킬 쿨타임 상태를 기본값으로 초기화한다.
    private void ResetSkillCooldown(int slotIndex)
    {
        EnsureCooldownArrays();

        if (slotIndex < 0 || slotIndex >= skillCooldownRemaining.Length) return;

        skillCooldownRemaining[slotIndex] = 0f;
        skillCooldownDuration[slotIndex] = GetSkillCooldownDuration(slotIndex);
    }

    // 2026.08.10_UI 정리: 현재 Valid 슬롯 Index 조건을 판별한다.
    private bool IsValidSlotIndex(int slotIndex)
    {
        return currentActiveSkills != null && slotIndex >= 0 && slotIndex < currentActiveSkills.Length;
    }

    // 2026.08.10_UI 정리: 조건에 맞는 스킬 Index 데이터를 찾는다.
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

    // 2026.08.10_UI 정리:  사용할 데이터를 생성한다.
    private void BuildEquippedSkills(PrototypeProgressSnapshot snapshot)
    {
        currentActiveSkills = new UIPauseSkillInfoData[EquippedSkillSlotCount];

        for (int i = 0; i < EquippedSkillSlotCount; i++)
        {
            currentActiveSkills[i] = CreateEmptySkillData();
        }

        PrototypeSkillState[] skillStates = snapshot.Skills ?? System.Array.Empty<PrototypeSkillState>();

        foreach (PrototypeSkillState state in skillStates)
        {
            if (state.SlotIndex < 0 || state.SlotIndex >= EquippedSkillSlotCount) continue;

            ActiveSkillData skillData = FindCatalogSkillData(state.SkillId);

            if (skillData == null) continue;

            currentActiveSkills[state.SlotIndex] =
                new UIPauseSkillInfoData(
                    skillData.Icon,
                    skillData.SkillName,
                    Mathf.Max(1, state.Level),
                    skillData.Desc,
                    true,
                    skillData.Id,
                    true);
        }
    }

    // 2026.08.10_UI 정리: 조건에 맞는 Catalog 스킬 데이터 데이터를 찾는다.
    private ActiveSkillData FindCatalogSkillData(int skillId)
    {
        if (activeSkillCatalog == null) return null;

        foreach (ActiveSkillData skillData in activeSkillCatalog)
        {
            if (skillData != null && skillData.Id == skillId) return skillData;
        }

        return null;
    }

    // 2026.08.10_UI 정리: 필요한 보유 슬롯 데이터 데이터를 생성한다.
    private UIPauseSkillInfoData CreateOwnedSlotData(PrototypeProgressSnapshot snapshot, int skillId)
    {
        if (skillId < 0)
        {
            return new UIPauseSkillInfoData(
                null,
                string.Empty,
                0,
                string.Empty,
                false,
                -1,
                false);
        }

        ActiveSkillData skillData = FindCatalogSkillData(skillId);

        if (skillData == null)
        {
            return new UIPauseSkillInfoData(
                null,
                string.Empty,
                0,
                string.Empty,
                false,
                -1,
                false);
        }

        PrototypeSkillState[] skillStates = snapshot.Skills ?? System.Array.Empty<PrototypeSkillState>();

        PrototypeSkillState? unlockedState = null;

        foreach (PrototypeSkillState state in skillStates)
        {
            if (state.SkillId == skillId)
            {
                unlockedState = state;
                break;
            }
        }

        bool isUnlocked = unlockedState.HasValue;

        return new UIPauseSkillInfoData(
            skillData.Icon,
            skillData.SkillName,
            isUnlocked
                ? Mathf.Max(1, unlockedState.Value.Level)
                : 1,
            skillData.Desc,
            false,
            skillData.Id,
            isUnlocked);
    }

    // 2026.08.10_UI 정리:  사용할 데이터를 생성한다.
    private void BuildOwnedSkillSlots(PrototypeProgressSnapshot snapshot)
    {
        currentOwnedSkills = new UIPauseSkillInfoData[OwnedSkillSlotCount];

        int[] order = snapshot.OwnedSkillOrder;

        for (int i = 0; i < OwnedSkillSlotCount; i++)
        {
            int skillId = order != null && i < order.Length ? order[i] : -1;

            currentOwnedSkills[i] = CreateOwnedSlotData(snapshot, skillId);
        }
    }

    // 2026.08.10_UI 정리: 장착 표시 값을 반영한다.
    private UIPauseSkillInfoData SetEquipped(UIPauseSkillInfoData skillData, bool isEquipped)
    {
        return new UIPauseSkillInfoData(
            skillData.Icon,
            skillData.SkillName,
            skillData.Level,
            skillData.Description,
            isEquipped,
            skillData.SkillId,
            skillData.IsUnlocked);
    }

    // 2026.08.10_UI 정리: 필요한 Empty 스킬 데이터 데이터를 생성한다.
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
