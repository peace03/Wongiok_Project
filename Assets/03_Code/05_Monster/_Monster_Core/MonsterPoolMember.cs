using UnityEngine;
using UnityEngine.Pool;

[DisallowMultipleComponent]
public sealed class MonsterPoolMember : MonoBehaviour, IPoolable
{
    private IObjectPool<GameObject> returnPool;
    private bool isReturnedToPool;

    public bool HasPool
    {
        get { return returnPool != null; }
    }

    // 김연호 : 풀에서 다시 활성화될 때 반환 완료 상태를 초기화합니다
    private void OnEnable()
    {
        isReturnedToPool = false;
    }

    // 김연호 : 이 몬스터가 반환될 오브젝트 풀 주소를 저장합니다
    public void SetPoolRef(
        IObjectPool<GameObject> poolRef)
    {
        returnPool = poolRef;
        isReturnedToPool = false;
    }

    // 김연호 : 현재 몬스터를 자신이 속한 오브젝트 풀로 반환합니다
    public bool TryReleaseToPool()
    {
        if (returnPool == null)
        {
            return false;
        }

        if (isReturnedToPool)
        {
            return false;
        }

        isReturnedToPool = true;
        returnPool.Release(gameObject);

        return true;
    }
}