using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class DefenseStageController : MonoBehaviour
{
    [System.Serializable]
    public class DefenseWave
    {
        public string waveName = "Wave";
        public float startDelay = 0.5f;
        public DefenseWaveEntry[] entries;
        public float afterClearDelay = 1f;
    }

    [System.Serializable]
    public class DefenseWaveEntry
    {
        public GameObject enemyPrefab;
        public DefenseEnemySpawnPoint spawnPoint;
        public int count = 1;
        public float spawnInterval = 0.5f;
    }

    [Header("Runtime State")]
    [SerializeField] private string zoneId;

    [Header("Completed State")]
    [SerializeField] private Collider entryTrigger;
    [SerializeField] private GameObject[] objectsToDisableWhenCleared;

    [Header("Camera")]
    [SerializeField] private SideViewCameraFollow cameraFollow;
    [SerializeField] private Transform cameraLockPoint;

    [Header("Arena")]
    [SerializeField] private DefenseArenaBounds arenaBounds;
    [SerializeField] private GameObject[] blockingObjects;
    [SerializeField] private bool disableBlockingObjectsOnAwake = true;

    [Header("Spawn")]
    [SerializeField] private Transform enemyContainer;
    [SerializeField] private DefenseWave[] waves;
    [SerializeField] private float clearCheckInterval = 0.25f;

    [Header("Events")]
    [SerializeField] private UnityEvent onStageStarted;
    [SerializeField] private UnityEvent onStageCleared;

    [Header("Checkpoint State")]
    [SerializeField] private string stageId;

    private readonly List<GameObject> aliveEnemies = new List<GameObject>();

    private Coroutine stageRoutine;
    private bool isRunning;
    private bool isCleared;

    public bool IsRunning
    {
        get { return isRunning; }
    }

    public bool IsCleared
    {
        get { return isCleared; }
    }

    public string ZoneId
    {
        get { return zoneId; }
    }

    // 시작 시 저장된 클리어 상태와 차단 벽의 초기 상태를 적용합니다.
    private void Awake()
    {
        ValidateStageId();

        if (DefenseStageRuntimeSession.IsStageCleared(stageId))
        {
            ApplyRestoredClearedState();
            return;
        }

        if (disableBlockingObjectsOnAwake)
        {
            SetBlockingObjectsActive(false);
        }
    }

    // Inspector에서 디펜스존 식별자 설정 상태를 확인합니다.
    private void OnValidate()
    {
        if (string.IsNullOrWhiteSpace(zoneId))
        {
            return;
        }

        zoneId = zoneId.Trim();
    }

    // 몬스터 사망 이벤트를 구독합니다.
    private void OnEnable()
    {
        EventBus<MonsterDeadEvent>.action += HandleMonsterDead;
    }

    // 몬스터 사망 이벤트 구독을 해제합니다.
    private void OnDisable()
    {
        EventBus<MonsterDeadEvent>.action -= HandleMonsterDead;
    }

    // 완료되지 않은 방어 스테이지를 시작합니다.
    public void StartStage()
    {
        if (isRunning || isCleared)
        {
            return;
        }

        if (DefenseStageRuntimeSession.IsStageCleared(stageId))
        {
            ApplyRestoredClearedState();
            return;
        }

        stageRoutine = StartCoroutine(RunStageRoutine());
    }

    // 실행 중인 방어 스테이지를 강제로 중단합니다.
    public void StopStage()
    {
        if (stageRoutine != null)
        {
            StopCoroutine(stageRoutine);
            stageRoutine = null;
        }

        isRunning = false;
        aliveEnemies.Clear();

        SetBlockingObjectsActive(false);
        UnlockCamera();
    }

    // 전달된 위치를 현재 아레나 범위 안으로 제한합니다.
    public Vector3 ClampPositionToArena(Vector3 position)
    {
        if (arenaBounds == null)
        {
            return position;
        }

        return arenaBounds.ClampPosition(position);
    }

    // 전달된 위치가 현재 아레나 안에 있는지 확인합니다.
    public bool IsPositionInsideArena(Vector3 position)
    {
        if (arenaBounds == null)
        {
            return true;
        }

        return arenaBounds.ContainsPosition(position);
    }

    // 방어 스테이지 전체 흐름을 순서대로 실행합니다.
    private IEnumerator RunStageRoutine()
    {
        isRunning = true;

        LockCamera();
        SetBlockingObjectsActive(true);
        onStageStarted.Invoke();

        for (int i = 0; i < waves.Length; i++)
        {
            yield return StartCoroutine(
                PlayWaveRoutine(waves[i]));

            yield return StartCoroutine(
                WaitUntilSpawnedEnemiesCleared());

            if (waves[i] != null &&
                waves[i].afterClearDelay > 0f)
            {
                yield return new WaitForSeconds(
                    waves[i].afterClearDelay);
            }
        }

        CompleteStage();
    }

    // 방어 스테이지 클리어를 기록하고 종료 처리를 실행합니다.
    private void CompleteStage()
    {
        DefenseStageRuntimeSession.MarkStageCleared(stageId);

        isCleared = true;
        isRunning = false;
        stageRoutine = null;

        SetBlockingObjectsActive(false);
        UnlockCamera();
        onStageCleared.Invoke();
    }

    // 완료된 디펜스존의 재진입과 재실행을 차단합니다.
    private void ApplyClearedState(bool invokeClearEvent)
    {
        isCleared = true;
        isRunning = false;
        stageRoutine = null;

        aliveEnemies.Clear();

        SetBlockingObjectsActive(false);
        SetCompletedObjectsActive(false);
        SetEntryTriggerEnabled(false);
        UnlockCamera();

        if (invokeClearEvent)
        {
            onStageCleared?.Invoke();
        }
    }

    // 체크포인트에 저장된 완료 상태를 전투 재실행 없이 적용합니다.
    private void ApplyRestoredClearedState()
    {
        isCleared = true;
        isRunning = false;
        stageRoutine = null;

        SetBlockingObjectsActive(false);
    }

    // 디펜스 스테이지 식별자가 설정되었는지 확인합니다.
    private void ValidateStageId()
    {
        if (!string.IsNullOrWhiteSpace(stageId))
        {
            stageId = stageId.Trim();
            return;
        }

        Debug.LogError(
            "DefenseStageController의 Stage ID가 비어 있습니다.",
            this);
    }


    // 하나의 웨이브에 포함된 스폰 항목들을 실행합니다.
    private IEnumerator PlayWaveRoutine(DefenseWave wave)
    {
        if (wave == null)
        {
            yield break;
        }

        if (wave.startDelay > 0f)
        {
            yield return new WaitForSeconds(
                wave.startDelay);
        }

        if (wave.entries == null)
        {
            yield break;
        }

        for (int i = 0; i < wave.entries.Length; i++)
        {
            yield return StartCoroutine(
                SpawnEntryRoutine(wave.entries[i]));
        }
    }

    // 하나의 스폰 항목에 따라 몬스터를 여러 마리 생성합니다.
    private IEnumerator SpawnEntryRoutine(
        DefenseWaveEntry entry)
    {
        if (entry == null)
        {
            yield break;
        }

        int spawnCount = Mathf.Max(0, entry.count);

        for (int i = 0; i < spawnCount; i++)
        {
            if (isCleared)
            {
                yield break;
            }

            SpawnEnemy(entry);

            if (entry.spawnInterval > 0f)
            {
                yield return new WaitForSeconds(
                    entry.spawnInterval);
            }
        }
    }

    // 스폰 포인트 또는 기본 위치에 몬스터를 생성합니다.
    private void SpawnEnemy(DefenseWaveEntry entry)
    {
        if (entry.enemyPrefab == null)
        {
            Debug.LogWarning(
                "Enemy prefab is missing.",
                this);

            return;
        }

        GameObject enemy;

        if (entry.spawnPoint != null)
        {
            enemy = entry.spawnPoint.Spawn(
                entry.enemyPrefab,
                enemyContainer);
        }
        else
        {
            enemy = Instantiate(
                entry.enemyPrefab,
                transform.position,
                Quaternion.identity);

            if (enemyContainer != null)
            {
                enemy.transform.SetParent(
                    enemyContainer);
            }
        }

        RegisterEnemy(enemy);
    }

    // 생성된 몬스터를 생존 목록에 등록합니다.
    private void RegisterEnemy(GameObject enemy)
    {
        if (enemy == null)
        {
            return;
        }

        aliveEnemies.Add(enemy);

        IDeadState deadState =
            enemy.GetComponentInChildren<IDeadState>();

        if (deadState == null)
        {
            Debug.LogWarning(
                "Spawned enemy has no IDeadState. " +
                "It may not clear correctly.",
                enemy);
        }
    }

    // 현재 스폰된 몬스터가 모두 죽거나 사라질 때까지 기다립니다.
    private IEnumerator WaitUntilSpawnedEnemiesCleared()
    {
        while (!isCleared)
        {
            CleanupClearedEnemies();

            if (aliveEnemies.Count <= 0)
            {
                yield break;
            }

            yield return new WaitForSeconds(
                Mathf.Max(0.01f, clearCheckInterval));
        }
    }

    // 이미 죽었거나 파괴된 몬스터 참조를 목록에서 제거합니다.
    private void CleanupClearedEnemies()
    {
        for (int i = aliveEnemies.Count - 1;
             i >= 0;
             i--)
        {
            GameObject enemy = aliveEnemies[i];

            if (enemy == null || IsEnemyDead(enemy))
            {
                aliveEnemies.RemoveAt(i);
            }
        }
    }

    // 몬스터가 사망 상태인지 확인합니다.
    private bool IsEnemyDead(GameObject enemy)
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

    // 몬스터 사망 이벤트를 받아 생존 목록에서 제거합니다.
    private void HandleMonsterDead(
        MonsterDeadEvent deadEvent)
    {
        if (deadEvent.MonsterObject == null)
        {
            return;
        }

        RemoveAliveEnemy(
            deadEvent.MonsterObject);
    }

    // 사망한 몬스터와 일치하는 생존 목록 항목을 제거합니다.
    private void RemoveAliveEnemy(
        GameObject deadMonsterObject)
    {
        for (int i = aliveEnemies.Count - 1;
             i >= 0;
             i--)
        {
            GameObject enemy = aliveEnemies[i];

            if (enemy == null ||
                IsSameObjectOrChild(
                    deadMonsterObject,
                    enemy))
            {
                aliveEnemies.RemoveAt(i);
            }
        }
    }

    // 두 오브젝트가 같거나 부모 자식 관계인지 확인합니다.
    private bool IsSameObjectOrChild(
        GameObject candidate,
        GameObject root)
    {
        if (candidate == null || root == null)
        {
            return false;
        }

        if (candidate == root)
        {
            return true;
        }

        return candidate.transform.IsChildOf(
                   root.transform) ||
               root.transform.IsChildOf(
                   candidate.transform);
    }

    // 방어 구간용 차단 벽을 켜거나 끕니다.
    private void SetBlockingObjectsActive(
        bool active)
    {
        if (blockingObjects == null)
        {
            return;
        }

        for (int i = 0;
             i < blockingObjects.Length;
             i++)
        {
            GameObject blockingObject =
                blockingObjects[i];

            if (blockingObject == null)
            {
                continue;
            }

            blockingObject.SetActive(active);
        }
    }

    // 디펜스존 완료 시 비활성화할 오브젝트를 켜거나 끕니다.
    private void SetCompletedObjectsActive(
        bool active)
    {
        if (objectsToDisableWhenCleared == null)
        {
            return;
        }

        for (int i = 0;
             i < objectsToDisableWhenCleared.Length;
             i++)
        {
            GameObject target =
                objectsToDisableWhenCleared[i];

            if (target == null)
            {
                continue;
            }

            target.SetActive(active);
        }
    }

    // 디펜스존 진입 트리거의 활성 상태를 변경합니다.
    private void SetEntryTriggerEnabled(
        bool enabled)
    {
        if (entryTrigger == null)
        {
            return;
        }

        entryTrigger.enabled = enabled;
    }

    // 카메라를 방어 구간 중심에 고정합니다.
    private void LockCamera()
    {
        if (cameraFollow == null ||
            cameraLockPoint == null)
        {
            return;
        }

        cameraFollow.LockTo(
            cameraLockPoint);
    }

    // 카메라를 기존 플레이어 추적 상태로 되돌립니다.
    private void UnlockCamera()
    {
        if (cameraFollow == null)
        {
            return;
        }

        cameraFollow.Unlock();
    }

    // 디펜스존 식별자가 올바르게 설정되었는지 확인합니다.
    private void ValidateZoneId()
    {
        if (!string.IsNullOrWhiteSpace(zoneId))
        {
            zoneId = zoneId.Trim();
            return;
        }

        Debug.LogError(
            "DefenseStageController의 Zone ID가 비어 있습니다.",
            this);
    }
}