using UnityEngine;
using UnityEngine.Pool;

public static class CustomObjectPool
{
    /// <summary>
    /// 오브젝트 풀 생성 함수
    /// </summary>
    /// <param name="prefab">생성할 오브젝트 프리팹</param>
    /// <param name="maxCount">최대 생성 개수(생략 가능, 기본 값 : 1000)</param>
    /// <param name="parent">생성된 오브젝트를 모와둘 곳(생략 가능)</param>
    /// <returns></returns>
    public static IObjectPool<GameObject> CreatePool(GameObject prefab, int maxCount = 1000,
                                                                        Transform parent = null)
    {
        // 오브젝트 풀 반환
        return new ObjectPool<GameObject>(() => MonoBehaviour.Instantiate(prefab, parent),
                                            (obj) => obj.SetActive(true),
                                            (obj) => obj.SetActive(false),
                                            (obj) => MonoBehaviour.Destroy(obj),
                                            maxSize : maxCount);
    }
}