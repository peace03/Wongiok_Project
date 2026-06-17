using System;
using UnityEngine;

// 타이틀 화면의 저장 파일 존재 여부 갱신 이벤트
public struct UISetTitleSaveStateEvent
{
    public bool HasSaveFile { get; private set; }

    public UISetTitleSaveStateEvent(bool hasSaveFile)
    {
        HasSaveFile = hasSaveFile;
    }
}

// 타이틀 화면에서 새 게임 시작 요청 시 발행 이벤트
public struct UITitleNewGameRequestedEvent
{

}

// 타이틀 화면에서 이어하기 요청 시 발행 이벤트
public struct UITitleContinueRequestedEvent
{

}

// 타이틀 화면에서 게임 종료 요청 시 발행 이벤트
public struct UITitleExitRequestedEvent
{

}

// 로딩 화면의 진행 및 메시지 갱신 이벤트
public struct UISetLoadingProgressEvent
{
    public float Progress { get; private set; }
    public string Message { get; private set; }

    public UISetLoadingProgressEvent(float progress, string message = null)
    {
        Progress = progress;
        Message = message;
    }
}

// 챕터 선택 화면에서 챕터 입장 요청 시 발행 이벤트
// 실제 챕터 로딩, 저장 처리, 썸네일 화면 이동 x
public struct UIChapterEnterRequestedEvent
{
    public int ChapterId { get; private set; }

    public UIChapterEnterRequestedEvent(int chapterId)
    {
        ChapterId = chapterId;
    }
}

// 챕터 선택 화면에서 뒤로가기 시 발행 이벤트
public struct UIChapterBackRequestedEvent
{

}

// 챕터 카드 화면에 표시할 데이터 전달 전용 이벤트
public struct UISetChapterTitleCardEvent
{
    public int ChapterId { get; private set; }
    public string Title { get; private set; }
    public string Subtitle { get; private set; }
    public string Description { get; private set; }
    public Sprite Thumbnail { get; private set; }

    public UISetChapterTitleCardEvent(
        int chapterId,
        string title,
        string subtitle,
        string description,
        Sprite thumbnail)
    {
        ChapterId = chapterId;
        Title = title;
        Subtitle = subtitle;
        Description = description;
        Thumbnail = thumbnail;
    }
}

// 챕터 화면에서 다음 진행을 요청 시 발행 이벤트
public struct UIChapterTitleCardContinueRequestedEvent
{
    public int ChapterId { get; private set; }

    public UIChapterTitleCardContinueRequestedEvent(int chapterId)
    {
        ChapterId = chapterId;
    }
}

// UI 화면 변경 이벤트입니다.
// 외부 시스템이 타이틀, 로딩, 인게임 같은 기본 Screen 전환을 요청할 때 발행합니다.
public struct UIChangeScreenEvent
{
    // 전환할 목표 Screen 상태입니다.
    public UIScreenState ScreenState { get; private set; }

    // 이벤트 발행 시 목표 Screen 상태를 함께 넘깁니다.
    public UIChangeScreenEvent(UIScreenState screenState)
    {
        ScreenState = screenState;
    }
}

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

// UI 팝업 상태 이벤트입니다.
// PopupManager가 팝업을 열거나 닫을 때 UIManager의 입력 차단 정책을 갱신하기 위해 발행하는 용도입니다.
public struct UISetPopupStateEvent
{
    // 현재 최상단에 떠 있는 Popup 종류입니다.
    // 팝업이 닫힌 상태라면 UIPopupType.None을 사용합니다.
    public UIPopupType PopupType { get; private set; }

    // 이벤트 발행 시 현재 Popup 타입을 함께 넘깁니다.
    public UISetPopupStateEvent(UIPopupType popupType)
    {
        PopupType = popupType;
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
    public Sprite Icon { get; private set; }
    public string SkillName { get; private set; }
    public int Level { get; private set; }
    public string Description { get; private set; }
    public bool IsEquipped { get; private set; }

    public UIPauseSkillInfoData(
        Sprite icon,
        string skillName,
        int level,
        string description,
        bool isEquipped)
    {
        Icon = icon;
        SkillName = skillName;
        Level = level;
        Description = description;
        IsEquipped = isEquipped;
    }
}

// 일시정지 - 전체 현황에서의 플레이어 상태
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

// 일시정지 - 스킬 페이지에 표시할 스킬 목록 데이터
public struct UISetPauseSkillPageEvent
{
    public UIPauseSkillInfoData[] EquippedActiveSkills { get; private set; }
    public UIPauseSkillInfoData[] OwnedSkills { get; private set; }
    public bool HasSelectedSkills { get; private set; }
    public UIPauseSkillInfoData SelectedSkill { get; private set; }

    public UISetPauseSkillPageEvent(
        UIPauseSkillInfoData[] equippedActiveSkills,
        UIPauseSkillInfoData[] ownedSkills,
        bool hasSelectedSkill = false,
        UIPauseSkillInfoData selectedSkill = default)
    {
        EquippedActiveSkills = equippedActiveSkills;
        OwnedSkills = ownedSkills;
        HasSelectedSkills = hasSelectedSkill;
        SelectedSkill = selectedSkill;
    }
}

// 일시정지 - 스킬 페이지에서 특정 스킬 선택 요청 시 사용할 이벤트
public struct UIPauseSkillSelectedEvent
{
    public int SkillIndex { get; private set; }
    public bool IsEquippedSkill { get; private set; }

    public UIPauseSkillSelectedEvent(int skillIndex, bool isEquippedSkill)
    {
        SkillIndex = skillIndex;
        IsEquippedSkill = isEquippedSkill;
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

    public UILevelUpSkillSelectedEvent(int optionIndex, int skillId)
    {
        OptionIndex = optionIndex;
        SkillId= skillId;
    }
}

// UI 확인 팝업 표시 이벤트입니다.
public struct UIShowConfirmPopupEvent
{
    public string Title { get; private set; }
    public string Message { get; private set; }
    public string ConfirmText { get; private set; }
    public string CancelText{ get; private set; }
    public Action OnConfirm { get; private set; }
    public Action OnCancel { get; private set; }

    public UIShowConfirmPopupEvent(
        string title,
        string message,
        Action onConfirm,
        Action onCancel = null,
        string confirmText = "확인",
        string cancelText = "취소")
    {
        Title = title;
        Message = message;
        OnConfirm = onConfirm;
        OnCancel = onCancel;
        ConfirmText = confirmText;
        CancelText = cancelText;
    }
}

public struct UIShowAlertPopupEvent
{
    public string Title { get; private set; }
    public string Message { get; private set; }
    public string ConfirmText { get; private set; }
    public Action OnConfirm { get; private set; }

    public UIShowAlertPopupEvent(
        string title,
        string message,
        Action onConfirm = null,
        string confirmText = "확인")
    {
        Title = title;
        Message = message;
        OnConfirm = onConfirm; 
        ConfirmText = confirmText;
    }
}

// UI 플레이어 HUD 표시 이벤트 구역
// 플레이어 스킬
public struct UIPlayerSkillSlotData
{
    public Sprite Icon { get; private set; }
    public string KeyText { get; private set; }
    public int Level { get; private set; }
    public float CooldownProgress { get; private set; }
    public bool IsAvailable { get; private set; }

    public UIPlayerSkillSlotData(
        Sprite icon,
        string keyText,
        int level,
        float cooldwonProgress,
        bool isAvailable)
    {
        Icon = icon;
        KeyText = keyText;
        Level = level;
        CooldownProgress = cooldwonProgress;
        IsAvailable = isAvailable;
    }
}

// 플레이어 레벨
public struct UISetPlayerLevelEvent
{
    public int Level { get; private set; }
    public UISetPlayerLevelEvent(int level)
    {
        Level = level;
    }
}

// 플레이어 경험치 게이지 갱신
public struct UISetPlayerExpEvent
{
    public float CurrentExp { get; private set; }
    public float RequiredExp { get; private set; }

    public UISetPlayerExpEvent(float currentExp, float requiredExp)
    {
        CurrentExp = currentExp;
        RequiredExp = requiredExp;
    }
}

// 플레이어 HP 게이지 갱신 이벤트
public struct UISetPlayerHpEvent
{
    public float CurrentHp { get; private set; }
    public float MaxHp { get; private set; }

    public UISetPlayerHpEvent(float currentHp, float maxHp)
    {
        CurrentHp = currentHp;
        MaxHp = maxHp;
    }
}

// 플레이어 목숨 표시 갱신 이벤트
public struct UISetPlayerLifeEvent
{
    public int CurrentLife { get; private set; }
    public int MaxLife { get; private set; }

    public UISetPlayerLifeEvent(int currentLife, int maxLife)
    {
        CurrentLife = currentLife;
        MaxLife = maxLife;
    }
}

// 플레이어 스킬 슬롯 표시 갱신 이벤트
public struct UISetPlayerSkillSlotsEvent
{
    public UIPlayerSkillSlotData[] SkillSlots { get; private set; }

    public UISetPlayerSkillSlotsEvent(UIPlayerSkillSlotData[] skillSlots)
    {
        SkillSlots = skillSlots;
    }
}

// 플레이어 회복 아이템 표시 갱신 이벤트
public struct UISetPlayerHealItemEvent
{
    public int Count { get; private set; }
    public int MaxCount { get; private set; }

    public UISetPlayerHealItemEvent(int count, int maxCount)
    {
        Count = count;
        MaxCount = maxCount;
    }
}

// UI 보스 HUD 표시 이벤트
// 보스전 시스템이 Boss HUD를 켜거나 끌 때 발행합니다.
public struct UISetBossHudVisibleEvent
{
    // true면 Boss HUD를 표시하고, false면 숨깁니다.
    public bool IsVisible { get; private set; }

    // 이벤트 발행 시 표시 여부를 함께 넘깁니다.
    public UISetBossHudVisibleEvent(bool isVisible)
    {
        IsVisible = isVisible;
    }
}

// UI 초기화 이벤트입니다.
// 게임 전체 Reset의 Single Entry Point에서 발행해 UIManager.ResetUI()를 호출하게 만드는 용도입니다.
public struct UIResetEvent
{

}

public struct UIFadeEvent
{
    public float FromAlpha { get; private set; }
    public float ToAlpha { get; private set; }
    public float Duration { get; private set; }
    public Action OnComplete { get; private set; }

    public UIFadeEvent(
        float fromAlpha,
        float toAlpha,
        float duration,
        Action onComplete = null)
    {
        FromAlpha = fromAlpha;
        ToAlpha = toAlpha;
        Duration = duration;
        OnComplete = onComplete;
    }
}
