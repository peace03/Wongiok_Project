using UnityEngine;
using UnityEngine.Pool;

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
/// 영역 액티브 스킬 인터페이스
/// </summary>
public interface IAreaSkill
{
    /// <summary>
    /// 영역 액티브 스킬 실행 함수
    /// </summary>
    public void ExecuteSkill(int id, AreaSkillLevelData skillData);
}

/// <summary>
/// 오브젝트 풀 인터페이스
/// </summary>
public interface IPoolable
{
    /// <summary>
    /// 오브젝트 풀 주소 설정 함수
    /// </summary>
    public void SetPoolRef(IObjectPool<GameObject> poolRef);
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
    /// 이펙트 실행 함수(time 초 이후 종료)
    /// </summary>
    public void ExecuteEffect(float time);

    /// <summary>
    /// 이펙트 종료 함수
    /// </summary>
    public void StopEffect();

    /// <summary>
    /// 이펙트 초기화 함수
    /// </summary>
    public void ResetEffect();
}