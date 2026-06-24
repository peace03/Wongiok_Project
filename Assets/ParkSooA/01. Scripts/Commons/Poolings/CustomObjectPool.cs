using UnityEngine;
using UnityEngine.Pool;

public static class CustomObjectPool
{
    // 오브젝트 풀 생성 함수
    public static IObjectPool<GameObject> CreatePool(GameObject prefab, int maxCount = 1000,
                                                                        Transform parent = null)
    {
        // 오브젝트 풀 반환
        return new ObjectPool<GameObject>(() => CreateObject(prefab, parent),
                                            (obj) => obj.SetActive(true),
                                            (obj) => obj.SetActive(false),
                                            (obj) => MonoBehaviour.Destroy(obj),
                                            maxSize : maxCount);
    }

    // 오브젝트 생성 함수
    private static GameObject CreateObject(GameObject prefab, Transform parent)
    {
        // 오브젝트 생성
        GameObject obj = MonoBehaviour.Instantiate(prefab);
        // 오브젝트 모와둘 주소 설정
        obj.transform.parent = parent;
        // 오브젝트 반환
        return obj;
    }
}