using UnityEngine;
using UnityEngine.Pool;

//BootStrapper에서 초기화 하려는 스크립트용 인터페이스
public interface IInitializable
{
    public int Priority { get; }    //초기화 순서
    public void Init();
}

//공격받을 수 있는 인터페이스
public interface IDamageable
{
    public void TakeDamage(float amount, Vector3 hitPoint = default);
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