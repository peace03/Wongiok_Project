using System.Collections.Generic;
using UnityEngine;

public interface ISkillSystemProvider
{
    public void GetCanEnhanceSkillUIDatas(List<UIPauseSkillInfoData> results);
}

/// <summary>
/// 발사체 액티브 스킬 인터페이스
/// </summary>
public interface IProjectileSkill
{
    /// <summary>
    /// 발사체 액티브 스킬 실행 함수
    /// </summary>
    public void ExecuteSkill(int id, ProjectileSkillLevelData skillData);
}

/// <summary>
/// 범위 액티브 스킬 인터페이스
/// </summary>
public interface IAreaSkill
{
    /// <summary>
    /// 범위 액티브 스킬 실행 함수
    /// </summary>
    public void ExecuteSkill(int id, AreaSkillLevelData skillData);
}

/// <summary>
/// 총구 소유 인터페이스
/// </summary>
public interface IHaveFirePoint
{
    /// <summary>
    /// 총구 위치들
    /// </summary>
    public List<Transform> FirePoints { get; }

    /// <summary>
    /// 애니메이션 재생 함수
    /// </summary>
    public void PlayAnimation();

    /// <summary>
    /// 애니메이션 취소 함수
    /// </summary>
    public void CancelAnimation();
}

/// <summary>
/// 이펙트 실행자 인터페이스
/// </summary>
public interface IEffectExecuter
{
    /// <summary>
    /// 이펙트 실행 함수
    /// </summary>
    public void ExecuteEffect();

    /// <summary>
    /// 이펙트 실행 함수
    /// </summary>
    /// <param name="time">이펙트 종료 시간</param>
    public void ExecuteEffect(float time);

    /// <summary>
    /// 이펙트 종료 함수
    /// </summary>
    /// <param name="immediately">즉시 종료 여부(기본값 : 즉시 종료 안함)</param>
    public void StopEffect(bool immediately = false);

    /// <summary>
    /// 이펙트 초기화 함수
    /// </summary>
    public void ResetEffect();
}

/// <summary>
/// 나선 이펙트 인터페이스
/// </summary>
public interface IWaveEffect
{
    /// <summary>
    /// 정보 설정 함수
    /// </summary>
    public void SetInfo();
}

/// <summary>
/// 타격/피격 이펙트 인터페이스
/// </summary>
public interface IHitEffect
{
    /// <summary>
    /// 정보 설정 함수
    /// </summary>
    /// <param name="target">따라다닐 대상(생략 가능, 기본값 : 비어있음)</param>
    public void SetInfo(Transform target = null);
}