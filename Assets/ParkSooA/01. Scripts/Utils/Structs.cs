using System;

[Serializable]
// 바꿀 스탯 정보
public readonly struct StatAdjustment
{
    public readonly STAT_TYPE statType;                 // 스탯 종류
    public readonly MODIFY_TYPE modifyType;             // 수식 종류
    public readonly float amount;                       // 변화량
}

[Serializable]
// 영역 스킬 단계 정보
public readonly struct AreaSkillStageData
{
    public readonly float damage;                       // 데미지
    public readonly float distance;                     // 사거리
    public readonly float angle;                        // 각도
}

// 누른 스킬 슬롯
public readonly struct PressedSkillSlot
{
    public readonly ACTIVE_SKILL_SLOT_TYPE slot;        // 슬롯 종류
}