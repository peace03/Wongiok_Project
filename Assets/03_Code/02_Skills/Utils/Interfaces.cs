using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 스킬 시스템 제공 인터페이스
/// </summary>
public interface ISkillSystemProvider
{
    /// <summary>
    /// UI용 강화 가능한 스킬 정보들 반환 함수
    /// </summary>
    /// <param name="results">결과를 담을 리스트</param>
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
    /// <param name="id">실행할 스킬 ID</param>
    /// <param name="skillData">실행할 스킬 레벨별 정보</param>
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
    /// <param name="id">실행할 스킬 ID</param>
    /// <param name="skillData">실행할 스킬 레벨별 정보</param>
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
    /// <param name="time">이펙트 종료 시간</param>
    public void ExecuteEffect(float time = 0f);

    /// <summary>
    /// 이펙트 종료 함수
    /// </summary>
    /// <param name="immediately">즉시 종료 여부(생략 가능, 기본값 : 즉시 종료 안함)</param>
    /// <param name="waitTime">이펙트 종료 대기 시간(생략 가능, 기본값 : 0초)</param>
    public void StopEffect(bool immediately = false, float waitTime = 0f);

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
public interface ITargetEffect
{
    /// <summary>
    /// 정보 설정 함수
    /// </summary>
    /// <param name="target">따라다닐 대상(생략 가능, 기본값 : 비어있음)</param>
    public void SetInfo(Transform target = null);
}