using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FieldOffscreenMonsterSpawner : MonoBehaviour
{
    [System.Serializable]
    public class MonsterSpawnEntry
    {
        public GameObject enemyPrefab;
        public FieldSpawnLaneType laneType =
            FieldSpawnLaneType.Ground;
        [Min(0f)] public float weight = 1f;
    }

    [Header("References")]
    [SerializeField] private Camera targetCamera;
    [SerializeField] private Transform player;
    [SerializeField] private Transform enemyContainer;
    [SerializeField] private FieldSpawnLane[] spawnLanes;

    [Header("Enemy Table")]
    [SerializeField] private MonsterSpawnEntry[] spawnEntries;

    [Header("Timing")]
    [SerializeField] private bool spawnOnEnable = true;
    [SerializeField] private float initialDelay = 1f;
    [SerializeField] private float spawnIntervalMin = 3f;
    [SerializeField] private float spawnIntervalMax = 6f;

    [Header("Limits")]
    [SerializeField] private int maxAliveMonsters = 4;
    [SerializeField] private int maxTotalSpawns = 10;
    [SerializeField] private int positionAttemptsPerCycle = 12;

    [Header("Offscreen Placement")]
    [SerializeField, Range(0f, 1f)]
    private float rightSideChance = 0.7f;
    [SerializeField] private float outsideMinDistance = 1.5f;
    [SerializeField] private float outsideMaxDistance = 4f;
    [SerializeField] private float viewportPadding = 0.08f;
    [SerializeField] private float minimumPlayerDistance = 8f;

    [Header("Collision Check")]
    [SerializeField] private LayerMask spawnBlockedMask;
    [SerializeField] private float overlapRadius = 0.45f;
    [SerializeField]
    private Vector3 overlapCheckOffset =
        new Vector3(0f, 0.8f, 0f);

    [Header("Debug")]
    [SerializeField] private bool logSpawns;

    private readonly List<GameObject> aliveMonsters =
        new List<GameObject>();
    private readonly List<FieldSpawnLane> matchingLaneBuffer =
        new List<FieldSpawnLane>();

    private Coroutine spawnRoutine;
    private bool spawningEnabled;
    private int totalSpawned;

    public int AliveMonsterCount
    {
        get { return aliveMonsters.Count; }
    }

    public int TotalSpawned
    {
        get { return totalSpawned; }
    }

    // 인스펙터에 입력된 스폰 설정을 유효한 범위로 보정합니다
    private void OnValidate()
    {
        initialDelay = Mathf.Max(0f, initialDelay);

        spawnIntervalMin =
            Mathf.Max(0.1f, spawnIntervalMin);

        spawnIntervalMax =
            Mathf.Max(spawnIntervalMin, spawnIntervalMax);

        maxAliveMonsters =
            Mathf.Max(1, maxAliveMonsters);

        maxTotalSpawns =
            Mathf.Max(0, maxTotalSpawns);

        positionAttemptsPerCycle =
            Mathf.Max(1, positionAttemptsPerCycle);

        outsideMinDistance =
            Mathf.Max(0f, outsideMinDistance);

        outsideMaxDistance =
            Mathf.Max(
                outsideMinDistance,
                outsideMaxDistance
            );

        viewportPadding =
            Mathf.Max(0f, viewportPadding);

        minimumPlayerDistance =
            Mathf.Max(0f, minimumPlayerDistance);

        overlapRadius =
            Mathf.Max(0f, overlapRadius);
    }

    // 시작할 때 카메라와 플레이어 및 SpawnLane 참조를 준비합니다
    private void Awake()
    {
        EnsureReferences();
        spawningEnabled = spawnOnEnable;
    }

    // 몬스터 사망 이벤트를 구독하고 스폰 반복을 시작합니다
    private void OnEnable()
    {
        EventBus<MonsterDeadEvent>.action +=
            HandleMonsterDead;

        if (spawnRoutine == null)
        {
            spawnRoutine =
                StartCoroutine(SpawnLoopRoutine());
        }
    }

    // 몬스터 사망 이벤트를 해제하고 스폰 반복을 중단합니다
    private void OnDisable()
    {
        EventBus<MonsterDeadEvent>.action -=
            HandleMonsterDead;

        if (spawnRoutine != null)
        {
            StopCoroutine(spawnRoutine);
            spawnRoutine = null;
        }
    }

    // 설정된 간격마다 필드 몬스터 생성을 시도합니다
    private IEnumerator SpawnLoopRoutine()
    {
        if (initialDelay > 0f)
        {
            yield return new WaitForSeconds(
                initialDelay
            );
        }

        while (true)
        {
            if (spawningEnabled)
            {
                TrySpawnOne();
            }

            float nextInterval = Random.Range(
                spawnIntervalMin,
                spawnIntervalMax
            );

            yield return new WaitForSeconds(
                nextInterval
            );
        }
    }

    // 새로운 필드 몬스터 생성을 일시 중지합니다
    public void PauseSpawning()
    {
        spawningEnabled = false;
    }

    // 일시 중지된 필드 몬스터 생성을 다시 시작합니다
    public void ResumeSpawning()
    {
        spawningEnabled = true;
    }

    // 외부 시스템에서 필드 스폰 활성 상태를 직접 설정합니다
    public void SetSpawningEnabled(bool enabled)
    {
        spawningEnabled = enabled;
    }

    // 현재 설정을 사용해 몬스터 한 마리의 즉시 생성을 시도합니다
    public bool SpawnOneNow()
    {
        return TrySpawnOne();
    }

    // 생존 수와 예산을 확인한 뒤 몬스터 한 마리 생성을 시도합니다
    private bool TrySpawnOne()
    {
        EnsureReferences();
        CleanupAliveMonsters();

        if (!CanAttemptSpawn())
        {
            return false;
        }

        MonsterSpawnEntry selectedEntry =
            SelectSpawnEntry();

        if (selectedEntry == null)
        {
            return false;
        }

        bool foundPosition =
            TryFindOffscreenSpawnPosition(
                selectedEntry.laneType,
                out Vector3 spawnPosition
            );

        if (!foundPosition)
        {
            if (logSpawns)
            {
                Debug.Log(
                    "No valid offscreen spawn position was found.",
                    this
                );
            }

            return false;
        }

        return SpawnEnemy(
            selectedEntry,
            spawnPosition
        );
    }

    // 현재 참조와 스폰 제한 조건이 몬스터 생성을 허용하는지 확인합니다
    private bool CanAttemptSpawn()
    {
        if (targetCamera == null ||
            player == null)
        {
            return false;
        }

        if (spawnEntries == null ||
            spawnEntries.Length <= 0)
        {
            return false;
        }

        if (spawnLanes == null ||
            spawnLanes.Length <= 0)
        {
            return false;
        }

        if (aliveMonsters.Count >=
            maxAliveMonsters)
        {
            return false;
        }

        if (maxTotalSpawns > 0 &&
            totalSpawned >= maxTotalSpawns)
        {
            return false;
        }

        return true;
    }

    // 가중치 설정에 따라 생성할 몬스터 항목을 선택합니다
    private MonsterSpawnEntry SelectSpawnEntry()
    {
        float totalWeight = 0f;
        MonsterSpawnEntry lastValidEntry = null;

        for (int i = 0;
             i < spawnEntries.Length;
             i++)
        {
            MonsterSpawnEntry entry =
                spawnEntries[i];

            if (entry == null ||
                entry.enemyPrefab == null ||
                entry.weight <= 0f)
            {
                continue;
            }

            totalWeight += entry.weight;
            lastValidEntry = entry;
        }

        if (totalWeight <= 0f)
        {
            return null;
        }

        float randomWeight =
            Random.value * totalWeight;

        for (int i = 0;
             i < spawnEntries.Length;
             i++)
        {
            MonsterSpawnEntry entry =
                spawnEntries[i];

            if (entry == null ||
                entry.enemyPrefab == null ||
                entry.weight <= 0f)
            {
                continue;
            }

            randomWeight -= entry.weight;

            if (randomWeight <= 0f)
            {
                return entry;
            }
        }

        return lastValidEntry;
    }

    // 선호 방향부터 검사하여 화면 밖의 유효한 위치를 찾습니다
    private bool TryFindOffscreenSpawnPosition(
        FieldSpawnLaneType laneType,
        out Vector3 spawnPosition)
    {
        bool tryRightFirst =
            Random.value <= rightSideChance;

        if (TryFindPositionOnSide(
                laneType,
                tryRightFirst,
                out spawnPosition))
        {
            return true;
        }

        return TryFindPositionOnSide(
            laneType,
            !tryRightFirst,
            out spawnPosition
        );
    }

    // 지정된 화면 방향과 호환되는 SpawnLane에서 위치를 탐색합니다
    private bool TryFindPositionOnSide(
        FieldSpawnLaneType laneType,
        bool rightSide,
        out Vector3 spawnPosition)
    {
        spawnPosition = default;

        BuildMatchingLaneBuffer(laneType);

        if (matchingLaneBuffer.Count <= 0)
        {
            return false;
        }

        for (int attempt = 0;
             attempt < positionAttemptsPerCycle;
             attempt++)
        {
            FieldSpawnLane lane =
                matchingLaneBuffer[
                    Random.Range(
                        0,
                        matchingLaneBuffer.Count
                    )
                ];

            if (!TryGetViewportEdgeWorldX(
                    lane.SpawnZ,
                    rightSide,
                    out float edgeWorldX))
            {
                continue;
            }

            float requestedMinX;
            float requestedMaxX;

            if (rightSide)
            {
                requestedMinX =
                    edgeWorldX + outsideMinDistance;

                requestedMaxX =
                    edgeWorldX + outsideMaxDistance;
            }
            else
            {
                requestedMinX =
                    edgeWorldX - outsideMaxDistance;

                requestedMaxX =
                    edgeWorldX - outsideMinDistance;
            }

            bool laneFoundPosition =
                lane.TryGetSpawnPosition(
                    requestedMinX,
                    requestedMaxX,
                    2,
                    out Vector3 candidatePosition
                );

            if (!laneFoundPosition)
            {
                continue;
            }

            if (!IsCandidateValid(
                    candidatePosition))
            {
                continue;
            }

            spawnPosition = candidatePosition;
            return true;
        }

        return false;
    }

    // 요청한 몬스터 종류와 일치하는 SpawnLane 목록을 준비합니다
    private void BuildMatchingLaneBuffer(
        FieldSpawnLaneType laneType)
    {
        matchingLaneBuffer.Clear();

        for (int i = 0;
             i < spawnLanes.Length;
             i++)
        {
            FieldSpawnLane lane = spawnLanes[i];

            if (lane == null ||
                lane.LaneType != laneType)
            {
                continue;
            }

            matchingLaneBuffer.Add(lane);
        }
    }

    // 지정된 Z 평면에서 카메라 화면 좌우 경계의 월드 X값을 계산합니다
    private bool TryGetViewportEdgeWorldX(
        float worldZ,
        bool rightSide,
        out float edgeWorldX)
    {
        edgeWorldX = 0f;

        float viewportX =
            rightSide ? 1f : 0f;

        Ray viewportRay =
            targetCamera.ViewportPointToRay(
                new Vector3(
                    viewportX,
                    0.5f,
                    0f
                )
            );

        Plane spawnPlane = new Plane(
            Vector3.forward,
            new Vector3(0f, 0f, worldZ)
        );

        bool hitPlane =
            spawnPlane.Raycast(
                viewportRay,
                out float enterDistance
            );

        if (!hitPlane ||
            enterDistance < 0f)
        {
            return false;
        }

        edgeWorldX =
            viewportRay.GetPoint(
                enterDistance
            ).x;

        return true;
    }

    // 후보 위치가 화면 밖이고 플레이어와 장애물에서 충분히 떨어졌는지 확인합니다
    private bool IsCandidateValid(
        Vector3 candidatePosition)
    {
        if (!IsOutsideCameraView(
                candidatePosition))
        {
            return false;
        }

        float minimumDistanceSquared =
            minimumPlayerDistance *
            minimumPlayerDistance;

        if ((candidatePosition -
             player.position).sqrMagnitude <
            minimumDistanceSquared)
        {
            return false;
        }

        if (overlapRadius <= 0f ||
            spawnBlockedMask.value == 0)
        {
            return true;
        }

        Vector3 overlapCenter =
            candidatePosition +
            overlapCheckOffset;

        bool isBlocked = Physics.CheckSphere(
            overlapCenter,
            overlapRadius,
            spawnBlockedMask,
            QueryTriggerInteraction.Ignore
        );

        return !isBlocked;
    }

    // 후보 위치가 안전 여백을 포함한 카메라 화면 밖인지 확인합니다
    private bool IsOutsideCameraView(
        Vector3 candidatePosition)
    {
        Vector3 viewportPosition =
            targetCamera.WorldToViewportPoint(
                candidatePosition
            );

        if (viewportPosition.z <= 0f)
        {
            return false;
        }

        bool insideExpandedView =
            viewportPosition.x >=
                -viewportPadding &&
            viewportPosition.x <=
                1f + viewportPadding &&
            viewportPosition.y >=
                -viewportPadding &&
            viewportPosition.y <=
                1f + viewportPadding;

        return !insideExpandedView;
    }

    // 선택된 몬스터를 지정 위치에 생성하고 생존 목록에 등록합니다
    private bool SpawnEnemy(
        MonsterSpawnEntry entry,
        Vector3 spawnPosition)
    {
        if (entry == null ||
            entry.enemyPrefab == null)
        {
            return false;
        }

        GameObject enemy = Instantiate(
            entry.enemyPrefab,
            spawnPosition,
            entry.enemyPrefab.transform.rotation
        );

        if (enemyContainer != null)
        {
            enemy.transform.SetParent(
                enemyContainer
            );
        }

        aliveMonsters.Add(enemy);
        totalSpawned++;

        if (logSpawns)
        {
            Debug.Log(
                $"Field monster spawned: {enemy.name} at {spawnPosition}",
                enemy
            );
        }

        return true;
    }

    // 파괴되었거나 사망한 몬스터를 생존 목록에서 제거합니다
    private void CleanupAliveMonsters()
    {
        for (int i =
                 aliveMonsters.Count - 1;
             i >= 0;
             i--)
        {
            GameObject enemy =
                aliveMonsters[i];

            if (enemy == null ||
                IsEnemyDead(enemy))
            {
                aliveMonsters.RemoveAt(i);
            }
        }
    }

    // 몬스터가 현재 사망 상태인지 확인합니다
    private bool IsEnemyDead(
        GameObject enemy)
    {
        if (enemy == null)
        {
            return true;
        }

        IDeadState deadState =
            enemy.GetComponentInChildren<IDeadState>();

        if (deadState == null)
        {
            return false;
        }

        return deadState.IsDead;
    }

    // 몬스터 사망 이벤트를 받아 생존 목록의 해당 몬스터를 제거합니다
    private void HandleMonsterDead(
        MonsterDeadEvent deadEvent)
    {
        if (deadEvent.MonsterObject == null)
        {
            return;
        }

        RemoveAliveMonster(
            deadEvent.MonsterObject
        );
    }

    // 사망 오브젝트와 동일한 생성 몬스터를 생존 목록에서 제거합니다
    private void RemoveAliveMonster(
        GameObject deadMonsterObject)
    {
        for (int i =
                 aliveMonsters.Count - 1;
             i >= 0;
             i--)
        {
            GameObject enemy =
                aliveMonsters[i];

            if (enemy == null ||
                IsSameObjectOrChild(
                    deadMonsterObject,
                    enemy
                ))
            {
                aliveMonsters.RemoveAt(i);
            }
        }
    }

    // 두 오브젝트가 같거나 부모 자식 관계인지 확인합니다
    private bool IsSameObjectOrChild(
        GameObject candidate,
        GameObject root)
    {
        if (candidate == null ||
            root == null)
        {
            return false;
        }

        if (candidate == root)
        {
            return true;
        }

        return
            candidate.transform.IsChildOf(
                root.transform
            ) ||
            root.transform.IsChildOf(
                candidate.transform
            );
    }

    // 비어 있는 카메라와 플레이어 및 SpawnLane 참조를 자동으로 찾습니다
    private void EnsureReferences()
    {
        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }

        if (player == null)
        {
            PlayerStatus playerStatus =
                FindFirstObjectByType<PlayerStatus>();

            if (playerStatus != null)
            {
                player =
                    playerStatus.transform;
            }
        }

        if (spawnLanes == null ||
            spawnLanes.Length <= 0)
        {
            spawnLanes =
                FindObjectsByType<FieldSpawnLane>(
                    FindObjectsInactive.Exclude,
                    FindObjectsSortMode.None
                );
        }
    }
}