using UnityEngine;

public enum FieldSpawnLaneType
{
    Ground,
    Flying
}

public class FieldSpawnLane : MonoBehaviour
{
    [Header("Lane")]
    [SerializeField]
    private FieldSpawnLaneType laneType =
        FieldSpawnLaneType.Ground;
    [SerializeField]
    private Vector2 size =
        new Vector2(24f, 8f);
    [SerializeField] private bool useTransformZ = true;
    [SerializeField] private float fixedZ;

    [Header("Ground")]
    [SerializeField] private LayerMask groundMask = ~0;
    [SerializeField] private float groundSpawnOffset = 0.05f;
    [SerializeField] private float extraGroundRaycastDistance = 2f;

    [Header("Gizmo")]
    [SerializeField]
    private Color gizmoColor =
        new Color(0.2f, 0.8f, 1f, 0.25f);

    public FieldSpawnLaneType LaneType
    {
        get { return laneType; }
    }

    public float LeftX
    {
        get { return transform.position.x - size.x * 0.5f; }
    }

    public float RightX
    {
        get { return transform.position.x + size.x * 0.5f; }
    }

    public float BottomY
    {
        get { return transform.position.y - size.y * 0.5f; }
    }

    public float TopY
    {
        get { return transform.position.y + size.y * 0.5f; }
    }

    public float SpawnZ
    {
        get
        {
            return useTransformZ
                ? transform.position.z
                : fixedZ;
        }
    }

    // 인스펙터에 입력된 Lane 값을 유효한 범위로 보정합니다
    private void OnValidate()
    {
        size.x = Mathf.Max(0.1f, size.x);
        size.y = Mathf.Max(0.1f, size.y);
        groundSpawnOffset = Mathf.Max(0f, groundSpawnOffset);
        extraGroundRaycastDistance =
            Mathf.Max(0f, extraGroundRaycastDistance);
    }

    // 요청된 X 범위와 Lane이 겹치는 영역에서 유효한 스폰 위치를 찾습니다
    public bool TryGetSpawnPosition(
        float requestedMinX,
        float requestedMaxX,
        int attempts,
        out Vector3 spawnPosition)
    {
        spawnPosition = default;

        float orderedMinX =
            Mathf.Min(requestedMinX, requestedMaxX);
        float orderedMaxX =
            Mathf.Max(requestedMinX, requestedMaxX);

        float availableMinX =
            Mathf.Max(orderedMinX, LeftX);
        float availableMaxX =
            Mathf.Min(orderedMaxX, RightX);

        if (availableMinX > availableMaxX)
        {
            return false;
        }

        int validAttempts = Mathf.Max(1, attempts);

        for (int i = 0; i < validAttempts; i++)
        {
            float candidateX = Random.Range(
                availableMinX,
                availableMaxX
            );

            if (laneType == FieldSpawnLaneType.Flying)
            {
                spawnPosition = new Vector3(
                    candidateX,
                    Random.Range(BottomY, TopY),
                    SpawnZ
                );

                return true;
            }

            if (TryFindGroundPosition(
                    candidateX,
                    out spawnPosition))
            {
                return true;
            }
        }

        return false;
    }

    // 지정된 X 위치 위에서 아래로 Raycast하여 지면 스폰 위치를 찾습니다
    private bool TryFindGroundPosition(
        float candidateX,
        out Vector3 spawnPosition)
    {
        spawnPosition = default;

        Vector3 rayOrigin = new Vector3(
            candidateX,
            TopY,
            SpawnZ
        );

        float raycastDistance =
            size.y + extraGroundRaycastDistance;

        bool hitGround = Physics.Raycast(
            rayOrigin,
            Vector3.down,
            out RaycastHit hit,
            raycastDistance,
            groundMask,
            QueryTriggerInteraction.Ignore
        );

        if (!hitGround)
        {
            return false;
        }

        float lowestAllowedY =
            BottomY - extraGroundRaycastDistance;

        if (hit.point.y < lowestAllowedY ||
            hit.point.y > TopY)
        {
            return false;
        }

        spawnPosition =
            hit.point + Vector3.up * groundSpawnOffset;

        spawnPosition.z = SpawnZ;

        return true;
    }

    // Scene 뷰에서 현재 SpawnLane의 유효 범위를 표시합니다
    private void OnDrawGizmosSelected()
    {
        Vector3 center = new Vector3(
            transform.position.x,
            transform.position.y,
            SpawnZ
        );

        Vector3 worldSize = new Vector3(
            size.x,
            size.y,
            0.2f
        );

        Gizmos.color = gizmoColor;
        Gizmos.DrawCube(center, worldSize);

        Gizmos.color = new Color(
            gizmoColor.r,
            gizmoColor.g,
            gizmoColor.b,
            1f
        );

        Gizmos.DrawWireCube(center, worldSize);
    }
}