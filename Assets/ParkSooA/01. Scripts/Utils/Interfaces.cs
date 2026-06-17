using UnityEngine;

// 초기화가 필요한 스크립트에 붙이는 인터페이스
public interface IInitializable
{
    // 중요도
    public int Priority { get; }

    // 초기화 함수
    public void Init();
}

public interface IProjectileActive
{
    public GameObject Bullet { get; }

    public void ExecuteProjectileActiveSkill(ProjectileSkillLevelData levelData);
}