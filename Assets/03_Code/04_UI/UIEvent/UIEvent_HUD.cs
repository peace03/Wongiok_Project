using UnityEngine;

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

//보스 HUD에 표시할 보스 이름 및 HP
public struct UISetBossHudDataEvent
{
    public string BossName {  get; private set; }
    public float CurrentHp { get; private set; }
    public float MaxHp { get; private set; }

    public UISetBossHudDataEvent(string bossName, float currentHp, float maxHp)
    {
        BossName = bossName;
        CurrentHp = currentHp;
        MaxHp = maxHp;
    }
}