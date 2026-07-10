using UnityEngine;

public class PlayerCheckpointTracker : MonoBehaviour
{
    private bool hasActiveCheckpoint;

    private int activeCheckpointNumber;

    private Vector3 activeRespawnPosition;

    private Vector3 startPosition;

    private float savedHP;

    private int savedHealItemCount;

    private PlayerStatus playerStatus;

    private PlayerHealItemInventory healItemInventory;

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

    private void Awake()
    {
        // 시작 스냅샷 저장은 PlayerInitializer에서 스탯/아이템 초기화 이후 처리합니다.
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

    private void SaveCurrentSnapshot(Vector3 respawnPosition)
    {
        // 체크포인트 활성화 순간의 체력과 회복 아이템 보유량을 부활용 데이터로 고정합니다.
        activeRespawnPosition = respawnPosition;
        savedHP = playerStatus != null ? playerStatus.GetCurrentHP() : 0f;
        savedHealItemCount = healItemInventory != null ? healItemInventory.CurrentCount : 0;
    }

    private void EnsureInitialized()
    {
        Initialize();
    }
}
