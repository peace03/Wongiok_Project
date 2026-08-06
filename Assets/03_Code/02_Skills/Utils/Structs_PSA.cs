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
// 액티브 스킬 사운드 정보
public struct ActiveSkillSound
{
    [Header("사운드 중요도")]
    public int priority;                                            // 사운드 중요도
    [Header("사운드 파일")]
    public AudioClip clip;                                          // 사운드 파일
    [Header("사운드 볼륨 크기")]
    public float volume;                                            // 사운드 볼륨 크기
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
// 스킬 단계 정보
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
/// 추가할 무기 외형 정보
/// </summary>
public readonly struct WeaponVisualAddData
{
    public readonly int id;                                         // 스킬 ID
    public readonly GameObject weapon;                              // 무기 프리팹

    /// <summary>
    /// 추가할 무기 외형 정보 생성자
    /// </summary>
    /// <param name="id">스킬 ID</param>
    /// <param name="weapon">무기 외형</param>
    public WeaponVisualAddData(int id, GameObject weapon)
    {
        this.id = id;
        this.weapon = weapon;
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
    /// <param name="id">스킬 ID</param>
    /// <param name="isActiveWeapon">무기 외형 활성화 여부</param>
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
    public readonly List<Transform> positions;                      // 실행 위치들

    /// <summary>
    /// 변경할 액티브 스킬 실행 위치들 정보 생성자
    /// </summary>
    /// <param name="positions">실행 위치들</param>
    public ChangeActiveSkillExecutePositions(List<Transform> positions) => this.positions = positions;
}

/// <summary>
/// 취소할 스킬 정보
/// </summary>
public readonly struct CancelSkill { }

/// <summary>
/// UI용 강화 가능한 스킬 정보들
/// </summary>
public readonly struct UICanEnhanceSkills
{
    public readonly List<UIPauseSkillInfoData> skills;              // 강화 가능한 스킬 정보들

    /// <summary>
    /// UI용 강화 가능한 스킬 정보들
    /// </summary>
    /// <param name="skills">강화 가능한 스킬 정보들</param>
    public UICanEnhanceSkills(List<UIPauseSkillInfoData> skills) => this.skills = skills;
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
/// 추가할 이펙트 정보
/// </summary>
public readonly struct EffectAddData
{
    public readonly GameObject prefab;                              // 이펙트 프리팹
    public readonly int size;                                       // 이펙트 최대 생성 개수

    /// <summary>
    /// 추가할 이펙트 정보 생성자
    /// </summary>
    /// <param name="prefab">추가할 이펙트 프리팹</param>
    /// <param name="size">이펙트 최대 생성 개수(생략 가능, 기본값 : 100)</param>
    public EffectAddData(GameObject prefab, int size = 100)
    {
        this.prefab = prefab;
        this.size = size;
    }
}

/// <summary>
/// 추가할 이펙트 정보들
/// </summary>
public readonly struct EffectAddDatas
{
    public readonly List<EffectAddData> datas;                      // 추가할 이펙트 정보들

    /// <summary>
    /// 추가할 이펙트 정보들 생성자
    /// </summary>
    /// <param name="datas">추가할 이펙트 정보들</param>
    public EffectAddDatas(List<EffectAddData> datas) => this.datas = datas;
}

/// <summary>
/// 실행할 이펙트 정보
/// </summary>
public readonly struct EffectPlayData
{
    public readonly GameObject prefab;                              // 이펙트 프리팹
    public readonly Vector3 position;                               // 이펙트 위치
    public readonly Quaternion rotation;                            // 이펙트 각도
    public readonly float? duration;                                // 이펙트 지속 시간
    public readonly Transform parent;                               // 따라다닐 대상

    /// <summary>
    /// 실행할 이펙트 정보 생성자
    /// </summary>
    /// <param name="prefab">실행할 이펙트 프리팹</param>
    /// <param name="worldPosition">실행할 이펙트의 월드 좌표(World Position)<br/>
    /// ※ 따라다닐 대상(parent)의 상대 좌표(Local Position)로 넣지 말것 ※</param>
    /// <param name="worldRotation">실행할 이펙트의 월드 각도(World Rotation)<br/>
    /// ※ 따라다닐 대상(parent)의 상대 각도(Local Rotation)로 넣지 말것 ※</param>
    /// <param name="duration">이펙트 지속 시간(생략 가능, 기본값 : 무한 or 이펙트 재생 시간)</param>
    /// <param name="parent">따라다닐 대상(생략 가능, 기본값 : 없음)</param>
    public EffectPlayData(GameObject prefab, Vector3 worldPosition, Quaternion worldRotation,
                                                float? duration = null, Transform parent = null)
    {
        this.prefab = prefab;
        position = worldPosition;
        rotation = worldRotation;
        this.duration = duration;
        this.parent = parent;
    }
}

/// <summary>
/// 종료할 이펙트 정보
/// </summary>
public readonly struct EffectStopData
{
    public readonly Effect effect;                                  // 종료할 이펙트
    public readonly bool immediately;                               // 즉시 종료 여부

    /// <summary>
    /// 종료할 이펙트 정보 생성자
    /// </summary>
    /// <param name="effect">종료할 이펙트</param>
    /// <param name="immediately">즉시 종료 여부(생략 가능, 기본값 : 즉시 종료 안함)</param>
    public EffectStopData(Effect effect, bool immediately = false)
    {
        this.effect = effect;
        this.immediately = immediately;
    }
}

/// <summary>
/// 초기화할 이펙트 정보
/// </summary>
public readonly struct EffectResetData
{
    public readonly Effect effect;                                  // 초기화할 이펙트

    /// <summary>
    /// 초기화할 이펙트 정보 생성자
    /// </summary>
    /// <param name="effect">초기화할 이펙트</param>
    public EffectResetData(Effect effect) => this.effect = effect;
}