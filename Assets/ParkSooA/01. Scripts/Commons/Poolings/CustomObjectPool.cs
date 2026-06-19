using UnityEngine;
using UnityEngine.Pool;

public static class CustomObjectPool
{
    // 오브젝트 풀 생성 함수
    public static IObjectPool<GameObject> CreatePool(GameObject prefab, int maxCount = 1000, Transform parent = null)
    {
        // 오브젝트 풀 반환
        return new ObjectPool<GameObject>(() => CreatePrefab(prefab, parent),
                                            (obj) => obj.SetActive(true),
                                            (obj) => obj.SetActive(false),
                                            (obj) => MonoBehaviour.Destroy(obj),
                                            maxSize : maxCount);
    }

    // 프리팹 생성 함수
    private static GameObject CreatePrefab(GameObject prefab, Transform parent)
    {
        // 프리팹 생성
        GameObject obj = MonoBehaviour.Instantiate(prefab);
        // 프리팹 모와둘 주소 설정
        obj.transform.parent = parent;
        // 프리팹 반환
        return obj;
    }
}