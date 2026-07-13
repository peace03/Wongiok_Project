using UnityEngine;

public class PlayerCheckpointTracker : MonoBehaviour
{
    #region 체크포인트에 사용될 정보들 ( 저장될 스텟, 위치정보등 )
    private bool hasActiveCheckpoint;

    private int activeCheckpointNumber;

    private Vector3 activeRespawnPosition;

    private Vector3 startPosition;

    private float savedHP;

    private int savedHealItemCount;

    private PlayerStatus playerStatus;

    private PlayerHealItemInventory healItemInventory;

    private bool isInitialized;
    #endregion

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

    public float SavedHP
    {
        get
        {
            EnsureInitialized();
            return savedHP;
        }
    }

    public int SavedHealItemCount
    {
        get
        {
            EnsureInitialized();
            return savedHealItemCount;
        }
    }

    public void Initialize(PlayerStatus status, PlayerHealItemInventory inventory)
    {
        if (isInitialized) return;

        playerStatus = status != null ? status : GetComponent<PlayerStatus>();
        healItemInventory = inventory != null ? inventory : GetComponent<PlayerHealItemInventory>();

        // 체크포인트를 밟기 전 사망에 대비해 시작 위치와 시작 상태를 기본 스냅샷으로 저장합니다.
        startPosition = transform.position;
        activeRespawnPosition = startPosition;
        activeCheckpointNumber = -1;
        SaveCurrentSnapshot(activeRespawnPosition);
        isInitialized = true;
    }

    public void Initialize()
    {
        Initialize(GetComponent<PlayerStatus>(), GetComponent<PlayerHealItemInventory>());
    }

    public bool TryActivateCheckpoint(Checkpoint checkpoint)
    {
        if (checkpoint == null) return false;

        EnsureInitialized();

        // 같은 번호와 낮은 번호는 재접촉해도 위치/체력/회복 아이템 스냅샷을 갱신하지 않습니다.
        if (hasActiveCheckpoint && checkpoint.CheckpointNumber <= activeCheckpointNumber)
            return false;

        hasActiveCheckpoint = true;
        activeCheckpointNumber = checkpoint.CheckpointNumber;
        activeRespawnPosition = checkpoint.RespawnPosition;
        SaveCurrentSnapshot(activeRespawnPosition);

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

    // 현재 위치와 플레이어 상태를 부활용 스냅샷으로 저장합니다
    private void SaveCurrentSnapshot(Vector3 respawnPosition)
    {
        activeRespawnPosition = respawnPosition;

        savedHP =
            playerStatus != null && playerStatus.Status != null
                ? playerStatus.Status.CurrentHP
                : 0f;

        savedHealItemCount =
            healItemInventory != null
                ? healItemInventory.CurrentCount
                : 0;
    }

    private void EnsureInitialized()
    {
        Initialize();
    }

    // 현재 플레이어 상태를 활성 체크포인트의 부활 스냅샷으로 다시 저장합니다
    public void RefreshCurrentSnapshot()
    {
        EnsureInitialized();

        Vector3 snapshotPosition = hasActiveCheckpoint
            ? activeRespawnPosition
            : startPosition;

        SaveCurrentSnapshot(snapshotPosition);
    }
} 