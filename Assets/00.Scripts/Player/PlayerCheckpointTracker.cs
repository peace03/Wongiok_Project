using UnityEngine;

public class PlayerCheckpointTracker : MonoBehaviour
{
    private bool hasActiveCheckpoint;

    private int activeCheckpointNumber;

    private Vector3 activeRespawnPosition;

    private Vector3 startPosition;

    private bool isInitialized;

    public bool HasActiveCheckpoint
    {
        get
        {
            EnsureInitialized();
            return hasActiveCheckpoint;
        }
    }

    public int ActiveCheckpointNumber
    {
        get
        {
            EnsureInitialized();
            return activeCheckpointNumber;
        }
    }

    public Vector3 RespawnPosition
    {
        get
        {
            EnsureInitialized();
            return hasActiveCheckpoint ? activeRespawnPosition : startPosition;
        }
    }

    private void Awake()
    {
        EnsureInitialized();
    }

    public bool TryActivateCheckpoint(Checkpoint checkpoint)
    {
        // 잘못된 체크포인트 요청은 무시합니다.
        if (checkpoint == null) return false;

        EnsureInitialized();

        // 이미 더 높은 번호를 밟았다면 낮거나 같은 번호는 진행도를 되돌리지 않습니다.
        if (hasActiveCheckpoint && checkpoint.CheckpointNumber <= activeCheckpointNumber)
            return false;

        hasActiveCheckpoint = true;
        activeCheckpointNumber = checkpoint.CheckpointNumber;
        activeRespawnPosition = checkpoint.RespawnPosition;

        EventBus<CheckpointActivatedEvent>.Publish(
            new CheckpointActivatedEvent(
                gameObject,
                checkpoint.gameObject,
                activeCheckpointNumber,
                activeRespawnPosition
            )
        );

        return true;
    }

    private void EnsureInitialized()
    {
        if (isInitialized) return;

        // 플레이어 배치 위치를 기본 부활 위치로 저장합니다.
        startPosition = transform.position;
        activeCheckpointNumber = -1;
        isInitialized = true;
    }
}
