using UnityEngine;
using UnityEngine.Video;

// UI 오버레이 열림 이벤트입니다.
// 컷씬, 레벨업, 일시정지처럼 기본 화면 위에 올라오며 게임플레이 입력을 막는 UI를 열 때 발행합니다.
public struct UIOpenOverlayEvent
{
    // 열고 싶은 Overlay 상태입니다.
    public UIOverlayState OverlayState { get; private set; }

    // 이벤트 발행 시 열 Overlay 상태를 함께 넘깁니다.
    public UIOpenOverlayEvent(UIOverlayState overlayState)
    {
        OverlayState = overlayState;
    }
}

// UI 오버레이 닫힘 이벤트입니다.
// 현재 구현에서는 전달된 OverlayState보다 "현재 열려 있는 Overlay를 닫는다"는 요청에 가깝습니다.
public struct UICloseOverlayEvent
{
    // 닫기를 요청한 Overlay 상태입니다.
    // 후속 구현에서 특정 Overlay만 닫는 정책이 필요해질 때 사용할 수 있습니다.
    public UIOverlayState OverlayState { get; private set; }

    // 이벤트 발행 시 닫기를 요청한 Overlay 상태를 함께 넘깁니다.
    public UICloseOverlayEvent(UIOverlayState overlayState)
    {
        OverlayState = overlayState;
    }
}

// 컷씬 View
public enum CutscenePlaybackType
{
    Standard,
    BossEncounter,
    BossClear
}

public enum CutsceneSkipInput
{
    Escape,
    BackQuote
}

public struct UISetCutsceneEvent
{
    public string CutsceneId { get; private set; }
    public VideoClip VideoClip { get; private set; }
    public string SkipSummary { get; private set; }
    public CutscenePlaybackType PlaybackType { get; private set; }

    // 2026.08.10_UI 정리: UI 이벤트 또는 표시 데이터를 초기화한다.
    public UISetCutsceneEvent(
        string cutsceneId,
        VideoClip videoClip,
        string skipSummary,
        CutscenePlaybackType playbackType = CutscenePlaybackType.Standard)
    {
        CutsceneId = cutsceneId;
        VideoClip = videoClip;
        SkipSummary = skipSummary;
        PlaybackType = playbackType;
    }
}

// 컷씬 스킵 요청 이벤트
public struct UICutsceneSkipRequestedEvent
{
    public CutsceneSkipInput Input { get; private set; }

    // 2026.08.10_UI 정리: UI 이벤트 또는 표시 데이터를 초기화한다.
    public UICutsceneSkipRequestedEvent(
        CutsceneSkipInput input = CutsceneSkipInput.Escape)
    {
        Input = input;
    }

}

public struct UICutsceneProceedRequestedEvent
{
}

public struct UIBossEncounterRequestedEvent
{
}

public struct UIBossEncounterLoadingReadyEvent
{
}

public struct UIBossEncounterActivateSceneRequestedEvent
{
}

public struct UIBossClearVideoFinishedEvent
{
}

// 컷씬 종료 이벤트
public struct UICutsceneFinishedEvent
{
    public string CutsceneId { get; private set; }
    public bool WasSkipped { get; private set; }

    // 2026.08.10_UI 정리: UI 이벤트 또는 표시 데이터를 초기화한다.
    public UICutsceneFinishedEvent(string cutsceneId, bool wasSkipped)
    {
        CutsceneId = cutsceneId;
        WasSkipped = wasSkipped;
    }
}

// 일시정지 메뉴 탭을 좌우로 이동하라는 입력 요청 이벤트
public struct UIPauseTabMoveRequestedEvent
{
    public int Direction { get; private set; }

    // 2026.08.10_UI 정리: UI 이벤트 또는 표시 데이터를 초기화한다.
    public UIPauseTabMoveRequestedEvent(int direction)
    {
        Direction = direction;
    }
}

// 일시정지 메뉴에서 메인 메뉴로 돌아가기를 요청할 때 발행
public struct UIPauseMainMenuRequestedEvent
{

}

// 일시정지 메뉴에서 게임 종료를 요청할 때 발생
public struct UIPauseQuitGameRequestedEvent
{

}

// 일시정지 - 전체 현황 및 스킬 요약 뎅티ㅓ
public struct UIPauseSkillInfoData
{
    public int SkillId { get; private set; }
    public Sprite Icon { get; private set; }
    public string SkillName { get; private set; }
    public int Level { get; private set; }
    public string Description { get; private set; }
    public bool IsEquipped { get; private set; }
    public bool IsUnlocked { get; private set;  }

    // 2026.08.10_UI 정리: UI 이벤트 또는 표시 데이터를 초기화한다.
    public UIPauseSkillInfoData(
        Sprite icon,
        string skillName,
        int level,
        string description,
        bool isEquipped,
        int skillId = -1,
        bool isUnlocked = true)
    {
        SkillId = skillId;
        Icon = icon;
        SkillName = skillName;
        Level = level;
        Description = description;
        IsEquipped = isEquipped;
        IsUnlocked = isUnlocked;
    }
}

// 일시정지 - 전체 현황에서의 플레이어 상태
// 2026.08.10_초기 Pause 상태를 현재 캐시 값으로 다시 요청한다.
public struct UIRequestPauseStatusEvent
{
}

public struct UISetPauseStatusEvent
{
    public int Level { get; private set; }
    public float CurrentExp { get; private set; }
    public float RequiredExp { get; private set; }
    public float CurrentHp { get; private set; }
    public float MaxHp { get; private set; }
    public int CurrentLife { get; private set; }
    public int MaxLife { get; private set; }
    public UIPauseSkillInfoData[] ActiveSkills { get; private set; }
    public UIPauseSkillInfoData[] PassiveSkills { get; private set; }

    // 2026.08.10_UI 정리: UI 이벤트 또는 표시 데이터를 초기화한다.
    public UISetPauseStatusEvent(
        int level,
        float currentExp,
        float requiredExp,
        float currentHp,
        float maxHp,
        int currentLife,
        int maxLife,
        UIPauseSkillInfoData[] activeSkills,
        UIPauseSkillInfoData[] passiveSkills)
    {
        Level = level;
        CurrentExp = currentExp;
        RequiredExp = requiredExp;
        CurrentHp = currentHp;
        MaxHp = maxHp;
        CurrentLife = currentLife;
        MaxLife = maxLife;
        ActiveSkills = activeSkills;
        PassiveSkills = passiveSkills;
    }
}

public struct RefreshUIEvent
{
    public UIPauseSkillInfoData[] EquippedSkills { get; private set; }
    public UIPauseSkillInfoData[] OwnedSkills { get; private set; }
    public int[] OwnedSkillOrder { get; private set; }
    public bool IsActiveSkill { get; private set; }

    // 2026.08.10_UI 정리: 이 타입의 초기 상태를 설정한다.
    public RefreshUIEvent(UIPauseSkillInfoData[] equippedSkills, UIPauseSkillInfoData[] ownedSkills,
                                                int[] ownedSkillOrder = null, bool isActiveSkill = true)
    {
        EquippedSkills = equippedSkills;
        OwnedSkills = ownedSkills;
        OwnedSkillOrder = ownedSkillOrder ?? System.Array.Empty<int>();
        IsActiveSkill = isActiveSkill;
    }
}

// 일시정지 - 스킬 페이지에 표시할 스킬 목록 데이터
public struct UISetPauseSkillPageEvent
{
    public UIPauseSkillInfoData[] EquippedActiveSkills { get; private set; }
    public UIPauseSkillInfoData[] OwnedSkills { get; private set; }
    public bool HasSelectedSkills { get; private set; }
    public UIPauseSkillInfoData SelectedSkill { get; private set; }
    public bool IsOwnedSkillListUnlocked { get; private set; }

    // 2026.08.10_UI 정리: UI 이벤트 또는 표시 데이터를 초기화한다.
    public UISetPauseSkillPageEvent(
        UIPauseSkillInfoData[] equippedActiveSkills,
        UIPauseSkillInfoData[] ownedSkills,
        bool hasSelectedSkill = false,
        UIPauseSkillInfoData selectedSkill = default,
        bool isOwnedSkillListUnlocked = false)
    {
        EquippedActiveSkills = equippedActiveSkills;
        OwnedSkills = ownedSkills;
        HasSelectedSkills = hasSelectedSkill;
        SelectedSkill = selectedSkill;
        IsOwnedSkillListUnlocked = isOwnedSkillListUnlocked;
    }
}

// 레벨업 스킬 선택 카드
public struct UILevelUpSkillOptionData
{
    public int SkillId { get; private set; }
    public Sprite Icon { get; private set; }
    public string SkillName { get; private set; }
    public int CurrentLevel { get; private set; }
    public int NextLevel { get; private set; }
    public string Description { get; private set; }
    public string EffectText { get; private set; }
    public bool IsMaxLevel { get; private set; }

    // 2026.08.10_UI 정리: UI 이벤트 또는 표시 데이터를 초기화한다.
    public UILevelUpSkillOptionData(
        int skillId,
        Sprite icon,
        string skillName,
        int currentLevel,
        int nextLevel,
        string description,
        string effectText,
        bool isMaxLevel)
    {
        SkillId = skillId;
        Icon = icon;
        SkillName = skillName;
        CurrentLevel = currentLevel;
        NextLevel = nextLevel;
        Description = description;
        EffectText = effectText;
        IsMaxLevel = isMaxLevel;
    }
}

// 레벨업 스킬 선택 Overlay에 표시할 목록
public struct UISetLevelUpOptionsEvent
{
    public UILevelUpSkillOptionData[] Options { get; private set; }

    // 2026.08.10_UI 정리: UI 이벤트 또는 표시 데이터를 초기화한다.
    public UISetLevelUpOptionsEvent(UILevelUpSkillOptionData[] options)
    {
        Options = options;
    }
}

// 레벨업 스킬 선택 카드 클릭 시 발행 이벤트
public struct UILevelUpSkillSelectedEvent
{
    public int OptionIndex { get; private set; }
    public int SkillId { get; private set; }

    // 2026.08.10_UI 정리: UI 이벤트 또는 표시 데이터를 초기화한다.
    public UILevelUpSkillSelectedEvent(int optionIndex, int skillId)
    {
        OptionIndex = optionIndex;
        SkillId = skillId;
    }
}

// 보유 스킬을 특정 액티브 슬롯에 장착 요청 이벤트
public struct UIPauseSkillEquipRequestedEvent
{
    public int SkillId { get; private set; }
    public int TargetSlotIndex { get; private set; }
    public int SourceOwnedSlotIndex { get; private set; }

    // 2026.08.10_UI 정리: UI 이벤트 또는 표시 데이터를 초기화한다.
    public UIPauseSkillEquipRequestedEvent(int skillId, int targetSlotIndex, int sourceOwnedSlotIndex)
    {
        SkillId = skillId;
        TargetSlotIndex = targetSlotIndex;
        SourceOwnedSlotIndex = sourceOwnedSlotIndex;
    }
}

// 장착 슬롯끼리 스왑 요청
public struct UIPauseSkillSwapRequestedEvent
{
    public int SourceSlotIndex { get; private set; }
    public int TargetSlotIndex { get; private set; }

    // 2026.08.10_UI 정리: UI 이벤트 또는 표시 데이터를 초기화한다.
    public UIPauseSkillSwapRequestedEvent(int sourceSlotIndex, int targetSlotIndex)
    {
        SourceSlotIndex = sourceSlotIndex;
        TargetSlotIndex = targetSlotIndex;
    }
}
