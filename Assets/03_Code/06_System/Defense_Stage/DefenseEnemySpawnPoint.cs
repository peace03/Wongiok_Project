using System.Collections.Generic;
using UnityEngine;

public class DefenseEnemySpawnPoint : MonoBehaviour
{
    [Header("Gizmo")]
    [SerializeField] private bool drawGizmo = true;
    [SerializeField] private float gizmoRadius = 0.35f;

    [Header("Rally")]
    [SerializeField] private Transform[] rallyPoints;
    [SerializeField]
    private MonsterRallyMovementMode rallyMovementMode =
        MonsterRallyMovementMode.Auto;
    [SerializeField] private float rallyMoveSpeed = 4f;
    [SerializeField] private float rallyArrivalDistance = 0.15f;
    [SerializeField] private float waitAtEachRallyPoint;
    [SerializeField] private bool disableCombatAIUntilRallyComplete = true;
    [SerializeField] private bool disableCharacterControllerDuringDirectMove = true;

    // 인스펙터에 입력된 랠리 이동 값을 유효한 범위로 보정합니다
    private void OnValidate()
    {
        gizmoRadius = Mathf.Max(0.01f, gizmoRadius);
        rallyMoveSpeed = Mathf.Max(0.01f, rallyMoveSpeed);
        rallyArrivalDistance = Mathf.Max(0.01f, rallyArrivalDistance);
        waitAtEachRallyPoint = Mathf.Max(0f, waitAtEachRallyPoint);
    }

    // 지정된 몬스터 프리팹을 생성하고 랠리 경로 이동을 시작합니다
    public GameObject Spawn(GameObject enemyPrefab, Transform parent)
    {
        if (enemyPrefab == null)
        {
            Debug.LogWarning("Enemy prefab is missing", this);
            return null;
        }

        GameObject enemy = Instantiate(
            enemyPrefab,
            transform.position,
            transform.rotation
        );

        if (parent != null)
        {
            enemy.transform.SetParent(parent);
        }

        StartRallyMovement(enemy);

        return enemy;
    }

    // 생성된 몬스터에 유효한 랠리 경로를 전달합니다
    private void StartRallyMovement(GameObject enemy)
    {
        if (enemy == null)
        {
            return;
        }

        Vector3[] rallyPositions = BuildRallyPositions();

        if (rallyPositions.Length <= 0)
        {
            return;
        }

        MonsterSpawnRallyMover rallyMover =
            enemy.GetComponent<MonsterSpawnRallyMover>();

        if (rallyMover == null)
        {
            rallyMover =
                enemy.AddComponent<MonsterSpawnRallyMover>();
        }

        rallyMover.BeginRally(
            rallyPositions,
            rallyMovementMode,
            rallyMoveSpeed,
            rallyArrivalDistance,
            waitAtEachRallyPoint,
            disableCombatAIUntilRallyComplete,
            disableCharacterControllerDuringDirectMove
        );
    }

    // 비어 있지 않은 랠리 포인트의 현재 월드 좌표를 배열로 생성합니다
    private Vector3[] BuildRallyPositions()
    {
        if (rallyPoints == null || rallyPoints.Length <= 0)
        {
            return new Vector3[0];
        }

        List<Vector3> validPositions =
            new List<Vector3>();

        for (int i = 0; i < rallyPoints.Length; i++)
        {
            if (rallyPoints[i] == null)
            {
                continue;
            }

            validPositions.Add(
                rallyPoints[i].position
            );
        }

        return validPositions.ToArray();
    }

    // Scene 뷰에서 스폰 위치와 랠리 이동 경로를 표시합니다
    private void OnDrawGizmos()
    {
        if (!drawGizmo)
        {
            return;
        }

        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(
            transform.position,
            gizmoRadius
        );

        if (rallyPoints == null)
        {
            return;
        }

        Vector3 previousPosition = transform.position;

        for (int i = 0; i < rallyPoints.Length; i++)
        {
            Transform rallyPoint = rallyPoints[i];

            if (rallyPoint == null)
            {
                continue;
            }

            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(
                previousPosition,
                rallyPoint.position
            );

            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(
                rallyPoint.position,
                gizmoRadius * 0.8f
            );

            previousPosition = rallyPoint.position;
        }
    }
}