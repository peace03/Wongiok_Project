using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

public class MonsterFactory : MonoBehaviour, IInitializable
{
    [Serializable]
    private class MonsterPoolEntry
    {
        [Header("Monster")]
        [SerializeField] private GameObject monsterPrefab;

        [Header("Pool")]
        [SerializeField]
        [Min(1)]
        private int maxCount = 16;

        [SerializeField] private Transform containerOverride;

        public GameObject MonsterPrefab
        {
            get { return monsterPrefab; }
        }

        public int MaxCount
        {
            get { return Mathf.Max(1, maxCount); }
        }

        public Transform ContainerOverride
        {
            get { return containerOverride; }
        }
    }

    [Header("Monster Pool")]
    [SerializeField] private Transform defaultContainer;
    [SerializeField]
    private List<MonsterPoolEntry> monsterPools =
        new List<MonsterPoolEntry>();

    private readonly
        Dictionary<GameObject, IObjectPool<GameObject>>
        poolByPrefab =
            new Dictionary<
                GameObject,
                IObjectPool<GameObject>>();

    private bool initialized;

    public int Priority
    {
        get { return (int)InitOrder.System + 4; }
    }

    // 김연호 : 등록된 몬스터 프리팹마다 전용 오브젝트 풀을 생성합니다
    public void Init()
    {
        if (initialized)
        {
            return;
        }

        poolByPrefab.Clear();

        for (int i = 0;
             i < monsterPools.Count;
             i++)
        {
            MonsterPoolEntry entry =
                monsterPools[i];

            if (!ValidateEntry(entry))
            {
                continue;
            }

            GameObject monsterPrefab =
                entry.MonsterPrefab;

            if (poolByPrefab.ContainsKey(
                    monsterPrefab))
            {
                Debug.LogWarning(
                    "MonsterFactory에 같은 몬스터 프리팹이 " +
                    "중복 등록되어 있습니다.",
                    monsterPrefab
                );

                continue;
            }

            Transform container =
                entry.ContainerOverride != null
                    ? entry.ContainerOverride
                    : GetDefaultContainer();

            IObjectPool<GameObject> pool =
                CustomObjectPool.CreatePool(
                    monsterPrefab,
                    entry.MaxCount,
                    container
                );

            if (pool == null)
            {
                Debug.LogError(
                    "몬스터 오브젝트 풀 생성에 실패했습니다.",
                    monsterPrefab
                );

                continue;
            }

            poolByPrefab.Add(
                monsterPrefab,
                pool
            );
        }

        initialized = true;
    }

    // 김연호 : 지정한 프리팹의 몬스터를 풀에서 가져와 Spawn 위치에 준비합니다
    public GameObject GetMonster(
        GameObject monsterPrefab,
        Vector3 spawnPosition,
        Quaternion spawnRotation)
    {
        if (!TryGetPool(
                monsterPrefab,
                out IObjectPool<GameObject> pool))
        {
            return null;
        }

        GameObject monsterObject =
            pool.Get();

        if (monsterObject == null)
        {
            Debug.LogWarning(
                "몬스터 풀에서 오브젝트를 가져오지 못했습니다.",
                this
            );

            return null;
        }

        MonsterPoolMember poolMember =
            monsterObject.GetComponent<
                MonsterPoolMember>();

        MonsterBase monsterBase =
            monsterObject.GetComponent<
                MonsterBase>();

        if (poolMember == null ||
            monsterBase == null)
        {
            Debug.LogError(
                "풀링 몬스터에는 MonsterPoolMember와 " +
                "MonsterBase가 필요합니다.",
                monsterObject
            );

            pool.Release(monsterObject);
            return null;
        }

        poolMember.SetPoolRef(pool);

        monsterBase.PrepareForSpawn(
            spawnPosition,
            spawnRotation
        );

        return monsterObject;
    }

    // 김연호 : 지정한 프리팹에 해당하는 몬스터 풀을 가져옵니다
    private bool TryGetPool(
        GameObject monsterPrefab,
        out IObjectPool<GameObject> pool)
    {
        pool = null;

        if (monsterPrefab == null)
        {
            Debug.LogWarning(
                "MonsterFactory에 몬스터 프리팹이 " +
                "전달되지 않았습니다.",
                this
            );

            return false;
        }

        if (!initialized)
        {
            Init();
        }

        if (poolByPrefab.TryGetValue(
                monsterPrefab,
                out pool))
        {
            return true;
        }

        Debug.LogWarning(
            "MonsterFactory에 등록되지 않은 " +
            $"몬스터 프리팹입니다: {monsterPrefab.name}",
            this
        );

        return false;
    }

    // 김연호 : 풀 등록 정보와 몬스터 프리팹 구성이 올바른지 확인합니다
    private bool ValidateEntry(
        MonsterPoolEntry entry)
    {
        if (entry == null)
        {
            return false;
        }

        GameObject monsterPrefab =
            entry.MonsterPrefab;

        if (monsterPrefab == null)
        {
            Debug.LogWarning(
                "MonsterFactory의 Pool 항목에 " +
                "몬스터 프리팹이 없습니다.",
                this
            );

            return false;
        }

        if (monsterPrefab.GetComponent<
                MonsterPoolMember>() == null)
        {
            Debug.LogError(
                $"{monsterPrefab.name} 루트에 " +
                "MonsterPoolMember가 없습니다.",
                monsterPrefab
            );

            return false;
        }

        if (monsterPrefab.GetComponent<
                MonsterBase>() == null)
        {
            Debug.LogError(
                $"{monsterPrefab.name} 루트에 " +
                "MonsterBase 계열 AI가 없습니다.",
                monsterPrefab
            );

            return false;
        }

        return true;
    }

    // 김연호 : 별도 Container가 없을 때 사용할 기본 부모 Transform을 반환합니다
    private Transform GetDefaultContainer()
    {
        if (defaultContainer != null)
        {
            return defaultContainer;
        }

        return transform;
    }
}