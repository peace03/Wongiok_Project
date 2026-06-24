using UnityEngine;
using System;

[Serializable]
// 액티브 스킬 레벨 정보
public abstract class ActiveSkillLevelData : BaseSkillLevelData
{
    [Header("최대 쿨타임")]
    [Tooltip("0으로 두면 쿨타임이 없는 스킬이 됨")]
    [SerializeField] private float maxCoolTime;     // 최대 쿨타임
    [Header("최대 지속 시간")]
    [Tooltip("0으로 두면 즉시 효과가 끝나는 스킬이 됨")]
    [SerializeField] private float maxDuration;     // 최대 지속 시간

    public float MaxCoolTime => maxCoolTime;
    public float MaxDuration => maxDuration;

    // 액티브 스킬 종류 반환 프로퍼티
    public abstract ACTIVE_SKILL_TYPE ActiveType { get; }

    // 데미지 반환 함수
    public abstract float GetDamage(int stage);
}