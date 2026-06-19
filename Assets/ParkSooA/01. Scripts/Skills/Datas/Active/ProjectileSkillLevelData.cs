using UnityEngine;
using System;
using System.Collections.Generic;

[Serializable]
// 발사체 액티브 스킬 레벨 정보
public class ProjectileSkillLevelData : ActiveSkillLevelData
{
    [SerializeField] private float damage;              // 데미지
    [SerializeField] private int projectileCount;       // 발사체 수
    [SerializeField] private int penetrationCount;      // 관통 횟수(-1 : 횟수 제한 없음)
    [SerializeField] private float maxChargingTime;     // 차징시간

    public int ProjectileCount => projectileCount;
    public int PenetrationCount => penetrationCount;
    public float MaxChargingTime => maxChargingTime;

    // 액티브 스킬 종류 반환 프로퍼티
    public override ACTIVE_SKILL_TYPE ActiveType => ACTIVE_SKILL_TYPE.Projectile;

    // 데미지 반환 함수
    public override float GetDamage(int stage) => damage;

    // 스킬 효과 적용 함수
    public override void ApplyEffect(GameObject owner, IReadOnlyList<StatAdjustment> prevStats)
    {
        // 소유자가 없다면
        if(owner == null)
        {
            Debug.LogError($"[Error | Skill] 발사체 스킬 실행 실패 => 소유자 : 없음");
            return;
        }
        // 발사체 스킬 인터페이스가 없다면
        else if(owner.GetComponentInChildren<IProjectileSkill>(true) is not IProjectileSkill executer)
        {
            Debug.LogError($"[Error | Skill] 발사체 스킬 실행 실패 => 발사체 스킬 인터페이스 : 없음");
            return;
        }
        // 발사체 스킬 인터페이스가 있다면
        else
            // 스킬 실행
            executer.ExecuteSkill(this);
    }
}