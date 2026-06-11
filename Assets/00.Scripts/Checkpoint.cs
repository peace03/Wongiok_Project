using UnityEngine;

[RequireComponent(typeof(Collider))]
public class Checkpoint : MonoBehaviour
{
    [Header("Checkpoint")]
    [SerializeField] private int checkpointNumber;

    [SerializeField] private Transform spawnPoint;

    public int CheckpointNumber => checkpointNumber;
    public Vector3 RespawnPosition => GetRespawnTransform().position;

    private void Reset()
    {
        Collider checkpointCollider = GetComponent<Collider>();
        if (checkpointCollider != null)
        {
            checkpointCollider.isTrigger = true;
        }
    }

    private void Awake()
    {
        Collider checkpointCollider = GetComponent<Collider>();
        if (checkpointCollider != null && !checkpointCollider.isTrigger)
        {
            Debug.LogWarning($"{name} 체크포인트 Collider의 Is Trigger가 꺼져 있습니다.");
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        PlayerCheckpointTracker tracker = other.GetComponentInParent<PlayerCheckpointTracker>();
        if (tracker == null) return;

        tracker.TryActivateCheckpoint(this);
    }

    private Transform GetRespawnTransform()
    {
        if (spawnPoint != null) return spawnPoint;

        Transform childSpawnPoint = transform.Find("SpawnPoint");
        if (childSpawnPoint != null) return childSpawnPoint;

        return transform;
    }
}
