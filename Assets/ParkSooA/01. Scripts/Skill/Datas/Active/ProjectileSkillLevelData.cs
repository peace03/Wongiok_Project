using UnityEngine;
using System;

[Serializable]
// 발사체 액티브 스킬 레벨 정보
public class ProjectileSkillLevelData : ActiveSkillLevelData
{
    [SerializeField] private float damage;              // 데미지
    [SerializeField] private int projectileCount;       // 발사체 수
    [SerializeField] private int penetrationCount;      // 관통 횟수(-1 : 횟수 제한 없음)
    [SerializeField] private float maxChargingTime;     // 차징시간

    public float Damage => damage;
    public int ProjectileCount => projectileCount;
    public int PenetrationCount => penetrationCount;

    public override float MaxChargingTime => maxChargingTime;

    // 액티브 스킬 종류 반환 프로퍼티
    public override ACTIVE_SKILL_TYPE ActiveType => ACTIVE_SKILL_TYPE.Projectile;

    // 데미지 반환 함수
    public override float GetDamage(int stage) => Damage;
}