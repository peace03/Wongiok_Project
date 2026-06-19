using UnityEngine;
using UnityEngine.Pool;

// 발사체 스킬 인터페이스
public interface IProjectileSkill
{
    // 스킬 실행 함수
    public void ExecuteSkill(ProjectileSkillLevelData skillData);
}

// 영역 스킬 인터페이스
public interface IAreaSkill
{
    // 스킬 실행 함수
    public void ExecuteSkill(AreaSkillLevelData skillData);
}

// 오브젝트 풀 인터페이스
public interface IPoolable
{
    // 오브젝트 풀 주소 설정 함수
    public void SetPoolRef(IObjectPool<GameObject> poolRef);
}