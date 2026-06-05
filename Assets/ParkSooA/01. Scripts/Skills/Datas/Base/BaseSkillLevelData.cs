using UnityEngine;
using System;
using System.Collections.Generic;

// 스킬 레벨 기본 정보
public abstract class BaseSkillLevelData
{
    // 바꿀 스탯 정보들 반환 함수
    public virtual IReadOnlyList<StatAdjustment> GetAppliedStats() => Array.Empty<StatAdjustment>();

    // 스킬 효과 적용 함수
    public abstract void ApplyEffect(GameObject target);

    // 스킬 효과 적용 해제 함수
    public virtual void RemoveEffect(GameObject target) { }
}