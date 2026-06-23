using UnityEngine;
using UnityEngine.Pool;

// 풀링 대상임을 표시하기 위한 마커 인터페이스입니다.
public interface IPoolable
{
}

// 기존 BulletFactory가 기대하는 풀 생성 API를 현재 프로젝트에서 사용할 수 있게 보강합니다.
public static class CustomObjectPool
{
    public static IObjectPool<GameObject> CreatePool(GameObject prefab, int maxCount, Transform container)
    {
        int capacity = Mathf.Max(1, maxCount);

        return new ObjectPool<GameObject>(
            () => CreateObject(prefab, container),
            OnGetObject,
            OnReleaseObject,
            OnDestroyObject,
            true,
            Mathf.Min(10, capacity),
            capacity
        );
    }

    private static GameObject CreateObject(GameObject prefab, Transform container)
    {
        // 프리팹이 비어 있으면 BulletFactory의 GetBullet 단계에서 null 처리를 하도록 넘깁니다.
        if (prefab == null) return null;

        GameObject instance = Object.Instantiate(prefab, container);
        instance.SetActive(false);
        return instance;
    }

    private static void OnGetObject(GameObject obj)
    {
        if (obj == null) return;

        obj.SetActive(true);
    }

    private static void OnReleaseObject(GameObject obj)
    {
        if (obj == null) return;

        obj.SetActive(false);
    }

    private static void OnDestroyObject(GameObject obj)
    {
        if (obj == null) return;

        Object.Destroy(obj);
    }
}
