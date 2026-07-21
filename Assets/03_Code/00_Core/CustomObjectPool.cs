using UnityEngine;
using UnityEngine.Pool;

/// <summary>
/// 오브젝트 풀 인터페이스
/// 풀링 대상임을 표시하기 위한 마커 인터페이스입니다.
/// </summary>
public interface IPoolable
{
    /// <summary>
    /// 오브젝트 풀 주소 설정 함수
    /// </summary>
    public void SetPoolRef(IObjectPool<GameObject> poolRef);
}

public static class CustomObjectPool
{
    /// <summary>
    /// 오브젝트 풀 생성 함수
    /// </summary>
    /// <param name="prefab">생성할 오브젝트 프리팹</param>
    /// <param name="maxCount">최대 생성 개수(생략 가능, 기본값 : 1000)</param>
    /// <param name="container">생성된 오브젝트를 모와둘 곳(생략 가능, 기본값 : 하이어라키 최상위)</param>
    public static IObjectPool<GameObject> CreatePool(GameObject prefab, int maxCount = 1000,
                                                                            Transform container = null)
    {
        // 1과 최대 생성 개수 중 큰 값 받아오기
        int capacity = Mathf.Max(1, maxCount);

        return new ObjectPool<GameObject>(
            () => CreateObject(prefab, container),
            OnGetObject,
            OnReleaseObject,
            OnDestroyObject,
            true,                           // 중복 체크 여부
            Mathf.Min(10, capacity),        // 초기 생성 개수
            capacity
        );
    }

    private static GameObject CreateObject(GameObject prefab, Transform container)
    {
        // 프리팹이 비어있다면
        if (prefab == null)
            return null;

        GameObject instance = Object.Instantiate(prefab, container);
        instance.SetActive(false);
        return instance;
    }

    private static void OnGetObject(GameObject obj)
    {
        if (obj == null)
            return;

        obj.SetActive(true);
    }

    private static void OnReleaseObject(GameObject obj)
    {
        if (obj == null)
            return;

        obj.SetActive(false);
    }

    private static void OnDestroyObject(GameObject obj)
    {
        if (obj == null)
            return;

        Object.Destroy(obj);
    }
}