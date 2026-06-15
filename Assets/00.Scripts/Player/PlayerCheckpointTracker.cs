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
        // 시작 위치 저장은 PlayerInitializer에서 초기화 순서에 맞춰 처리합니다.
    }

    public void Initialize()
    {
        if (isInitialized) return;

        // 플레이어 배치 위치를 체크포인트가 없을 때의 기본 부활 위치로 저장합니다.
        startPosition = transform.position;
        activeCheckpointNumber = -1;
        isInitialized = true;
    }

    public bool TryActivateCheckpoint(Checkpoint checkpoint)
    {
        // 잘못된 체크포인트 요청은 무시합니다.
        if (checkpoint == null) return false;

        EnsureInitialized();

        // 더 높은 번호를 밟은 뒤에는 낮거나 같은 번호로 진행도를 되돌리지 않습니다.
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
        // 외부에서 초기화 전에 값을 읽는 예외 흐름도 안전하게 처리합니다.
        Initialize();
    }
}
