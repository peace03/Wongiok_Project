using System;
using UnityEngine;
using System.Collections.Generic;

[Serializable]
// 액티브 스킬 이펙트 정보
public struct ActiveSkillEffect
{
    [Header("이펙트 종류")]
    [Tooltip("차징 / 총구 / 궤적 / 메인(총알, 범위) / 타겟(과녁) / 타격(피격)")]
    public ACTIVE_SKILL_EFFECT_TYPE type;                           // 이펙트 종류
    [Header("이펙트 중요도")]
    [Tooltip("숫자가 높을수록 같은 이펙트 종류에서 먼저 실행됨")]
    public int priority;                                            // 이펙트 중요도
    [Header("이펙트 프리팹")]
    public GameObject prefab;                                       // 이펙트 프리팹
}

[Serializable]
// 적용할 스탯 정보
public struct StatAdjustment
{
    [Header("스탯 종류")]
    [Tooltip("체력, 공격력, 이동 속도, 공격 속도")]
    public STAT_TYPE stat;                                          // 스탯 종류
    [Header("수식 종류")]
    [Tooltip("더하기, 빼기")]
    public MODIFY_TYPE modify;                                      // 수식 종류
    [Header("변화량")]
    [Tooltip("추후, 퍼센트도 추가할 확률 높음")]
    public float amount;                                            // 변화량
}

[Serializable]
// 범위 스킬 단계 정보
public struct AreaSkillStageData
{
    [Header("데미지")]
    public float damage;                                            // 데미지
    [Header("사거리")]
    [Tooltip("데미지가 들어갈 스킬의 최대 거리")]
    public float distance;                                          // 사거리
    [Header("각도")]
    [Tooltip("데미지가 들어갈 스킬의 최대 각도, 최대 180도")]
    public float angle;                                             // 각도
    [Header("타격 주기")]
    [Tooltip("데미지가 들어가는 주기(간격)\n총 타격 주기의 합이 최대 지속 시간과 같아야 함")]
    public float tickInterval;                                      // 타격 주기
}

/// <summary>
/// 장착한 액티브 스킬 정보
/// </summary>
public readonly struct EquippedActiveSkill
{
    public readonly int id;                                         // 스킬 ID
    public readonly GameObject weapon;                              // 무기 프리팹
    public readonly IReadOnlyList<ActiveSkillEffect> effects;       // 스킬 이펙트 정보들

    /// <summary>
    /// 장착한 액티브 스킬 정보 생성자
    /// </summary>
    public EquippedActiveSkill(int id, GameObject weapon, IReadOnlyList<ActiveSkillEffect> effects)
    {
        this.id = id;
        this.weapon = weapon;
        this.effects = effects;
    }
}

/// <summary>
/// 변경할 액티브 스킬 무기 외형 정보
/// </summary>
public readonly struct ChangeWeaponState
{
    public readonly int id;                                         // 스킬 ID
    public readonly bool isActiveWeapon;                            // 외형 활성화 여부

    /// <summary>
    /// 변경할 액티브 스킬 무기 외형 정보 생성자
    /// </summary>
    public ChangeWeaponState(int id, bool isActiveWeapon = true)
    {
        this.id = id;
        this.isActiveWeapon = isActiveWeapon;
    }
}

/// <summary>
/// 변경할 액티브 스킬 실행 위치들 정보
/// </summary>
public readonly struct ChangeActiveSkillExecutePositions
{
    public readonly List<Transform> positions;               // 실행 위치들

    /// <summary>
    /// 변경할 액티브 스킬 실행 위치들 정보 생성자
    /// </summary>
    public ChangeActiveSkillExecutePositions(List<Transform> positions)
        => this.positions = positions;
}

/// <summary>
/// 실행할 액티브 스킬 이펙트 정보
/// </summary>
public readonly struct ExecuteActiveSkillEffect
{
    public readonly int id;                                         // 스킬 ID
    public readonly ACTIVE_SKILL_EFFECT_TYPE type;                  // 이펙트 종류
    public readonly Vector3? pos;                                   // 실행 위치
    public readonly float? duration;                                // 지속 시간

    /// <summary>
    /// 실행할 액티브 스킬 이펙트 정보 생성자
    /// </summary>
    public ExecuteActiveSkillEffect(int id, ACTIVE_SKILL_EFFECT_TYPE type,
                                    Vector3? pos = null, float? duration = null)
    {
        this.id = id;
        this.type = type;
        this.pos = pos;
        this.duration = duration;
    }
}

/// <summary>
/// 종료할 액티브 스킬 이펙트 정보
/// </summary>
public readonly struct StopActiveSkillEffect
{
    public readonly int id;                                         // 스킬 ID
    public readonly ACTIVE_SKILL_EFFECT_TYPE type;                  // 이펙트 종류

    /// <summary>
    /// 종료할 액티브 스킬 이펙트 정보 생성자
    /// </summary>
    public StopActiveSkillEffect(int id, ACTIVE_SKILL_EFFECT_TYPE type)
    {
        this.id = id;
        this.type = type;
    }
}

/// <summary>
/// 초기화할 액티브 스킬 이펙트 정보
/// </summary>
public readonly struct ResetActiveSkillEffect
{
    public readonly int id;                                         // 스킬 ID
    public readonly ACTIVE_SKILL_EFFECT_TYPE type;                  // 이펙트 종류

    /// <summary>
    /// 초기화할 액티브 스킬 이펙트 정보 생성자
    /// </summary>
    public ResetActiveSkillEffect(int id, ACTIVE_SKILL_EFFECT_TYPE type)
    {
        this.id = id;
        this.type = type;
    }
}

/// <summary>
/// 누른 스킬 슬롯 정보
/// </summary>
public readonly struct StartedPressSkillSlot
{
    public readonly ACTIVE_SKILL_SLOT_TYPE type;                    // 슬롯 종류

    /// <summary>
    /// 누른 스킬 슬롯 정보 생성자
    /// </summary>
    public StartedPressSkillSlot(ACTIVE_SKILL_SLOT_TYPE type) => this.type = type;
}

/// <summary>
/// 눌렀던 스킬 슬롯 정보
/// </summary>
public readonly struct CanceledPressSkillSlot
{
    public readonly ACTIVE_SKILL_SLOT_TYPE type;                    // 슬롯 종류

    /// <summary>
    /// 눌렀던 스킬 슬롯 정보 생성자
    /// </summary>
    public CanceledPressSkillSlot(ACTIVE_SKILL_SLOT_TYPE type) => this.type = type;
}