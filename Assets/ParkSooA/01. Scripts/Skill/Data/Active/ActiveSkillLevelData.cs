using UnityEngine;

// 액티브 스킬 레벨 정보
public abstract class ActiveSkillLevelData
{
    [SerializeField] private float maxCoolTime;     // 쿨타임
    [SerializeField] private float maxDuration;     // 지속시간

    public float MaxCoolTime => maxCoolTime;
    public float MaxDuration => maxDuration;

    //public abstract float 
}