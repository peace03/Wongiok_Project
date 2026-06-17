using UnityEngine;
using System;

[Serializable]
// 액티브 스킬 레벨 정보
public abstract class ActiveSkillLevelData : BaseSkillLevelData
{
    [SerializeField] private GameObject bullet;     // (임시) 총알
    [SerializeField] private float maxCoolTime;     // 쿨타임
    [SerializeField] private float maxDuration;     // 지속시간

    public GameObject Bullet => bullet;
    public float MaxCoolTime => maxCoolTime;
    public float MaxDuration => maxDuration;

    // 최대 차징 시간 반환 프로퍼티
    public virtual float MaxChargingTime => 0f;

    // 액티브 스킬 종류 반환 프로퍼티
    public abstract ACTIVE_SKILL_TYPE ActiveType { get; }

    // 데미지 반환 함수
    public abstract float GetDamage(int stage);
}