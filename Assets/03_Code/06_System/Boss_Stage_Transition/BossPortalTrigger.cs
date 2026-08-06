using UnityEngine;

[RequireComponent(typeof(Collider))]
public class BossPortalTrigger : MonoBehaviour
{
    [Header("Portal")]
    [SerializeField] private GameObject portalVisual;

    private Collider portalCollider;
    private bool isUnlocked;
    private bool hasTriggered;

    public bool IsUnlocked => isUnlocked;

    private void Awake()
    {
        portalCollider = GetComponent<Collider>();
        portalCollider.isTrigger = true;

        SetUnlocked(false);
    }

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

        // 포탈 스크립트가 붙은 자기 자신이 아니라
        // 별도의 자식 이펙트 오브젝트만 활성화합니다.
        if (portalVisual != null && portalVisual != gameObject)
        {
            portalVisual.SetActive(unlocked);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // 아직 잠겨 있거나 이미 사용한 포탈이면 실행하지 않습니다.
        if (!isUnlocked || hasTriggered)
        {
            return;
        }

        // 플레이어 계층에 PlayerStatus가 있는지 확인합니다.
        PlayerStatus playerStatus =
            other.GetComponentInParent<PlayerStatus>();

        if (playerStatus == null)
        {
            return;
        }

        // 여러 플레이어 Collider가 동시에 들어와도 한 번만 실행되게 합니다.
        hasTriggered = true;
        portalCollider.enabled = false;

        // UI 시스템에 인트로 재생과 보스 Scene 로드를 요청합니다.
        EventBus<UIBossEncounterRequestedEvent>.Publish(default);
    }

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