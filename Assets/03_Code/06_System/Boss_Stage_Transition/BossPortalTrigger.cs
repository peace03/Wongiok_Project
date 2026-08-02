using UnityEngine;

[RequireComponent(typeof(Collider))]
public class BossPortalTrigger : MonoBehaviour
{
    [Header("Portal")]
    [SerializeField] private GameObject portalVisual;
    [SerializeField] private BossStageTransitionCoordinator transitionCoordinator;

    private Collider portalCollider;
    private bool isUnlocked;
    private bool hasTriggered;

    public bool IsUnlocked => isUnlocked;

    // 포탈 Collider를 Trigger로 설정하고 잠긴 상태를 적용합니다.
    private void Awake()
    {
        portalCollider = GetComponent<Collider>();
        portalCollider.isTrigger = true;
        SetUnlocked(false);
    }

    // 포탈의 시각 오브젝트와 Trigger 활성 상태를 변경합니다.
    public void SetUnlocked(bool unlocked)
    {
        isUnlocked = unlocked;

        if (!unlocked)
        {
            hasTriggered = false;
        }

        if (portalCollider == null)
        {
            portalCollider = GetComponent<Collider>();
        }

        if (portalCollider != null)
        {
            portalCollider.enabled = unlocked;
        }

        if (portalVisual != null && portalVisual != gameObject)
        {
            portalVisual.SetActive(unlocked);
        }
    }

    // 플레이어가 열린 포탈에 들어오면 보스 스테이지 전환을 요청합니다.
    private void OnTriggerEnter(Collider other)
    {
        if (!isUnlocked || hasTriggered)
        {
            return;
        }

        PlayerStatus playerStatus =
            other.GetComponentInParent<PlayerStatus>();

        if (playerStatus == null)
        {
            return;
        }

        PlayerController playerController =
            playerStatus.GetComponentInParent<PlayerController>();

        if (playerController == null)
        {
            Debug.LogWarning(
                "포탈에 진입한 플레이어에서 PlayerController를 찾지 못했습니다.",
                playerStatus);
            return;
        }

        if (transitionCoordinator == null)
        {
            Debug.LogWarning(
                "BossStageTransitionCoordinator 참조가 비어 있습니다.",
                this);
            return;
        }

        if (!transitionCoordinator.TryBeginTransition(playerController))
        {
            return;
        }

        hasTriggered = true;
        portalCollider.enabled = false;
    }

    // Inspector에서 포탈 시각 오브젝트가 자기 자신으로 지정되지 않았는지 확인합니다.
    private void OnValidate()
    {
        if (portalVisual == gameObject)
        {
            Debug.LogWarning(
                "Portal Visual에는 포탈의 자식 오브젝트를 지정해야 합니다.",
                this);
        }
    }
}
