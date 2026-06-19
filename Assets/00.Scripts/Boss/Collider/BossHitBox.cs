using UnityEngine;

public class BossHitBox : MonoBehaviour, IInitializable
{
    public int Priority => (int)InitOrder.Boss + 4;

    [SerializeField] private BossAttackType attackType;

    private BossStatus attackPower;
    private bool isTriggered;
    private BoxCollider box;

    public void Init()
    {
        // 보스 공격력과 히트박스 콜라이더 참조를 초기화합니다.
        attackPower = ServiceLocator.Get<BossStatus>();
        box = GetComponent<BoxCollider>();
    }

    private void OnEnable()
    {
        EventBus<AttackFinish>.action += SetIsTriggered;
    }

    private void OnDisable()
    {
        EventBus<AttackFinish>.action -= SetIsTriggered;
    }

    private void SetIsTriggered(AttackFinish data)
    {
        isTriggered = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (isTriggered) return;

        PlayerStatus playerStatus = other.GetComponentInParent<PlayerStatus>();
        if (playerStatus == null) return;

        isTriggered = true;

        Vector3 hitPoint = other.ClosestPoint(transform.position);
        Vector3 hitDirection = (playerStatus.transform.position - transform.position).normalized;

        playerStatus.TakeDamage(
            new DamageInfo(
                playerStatus.gameObject,
                other,
                gameObject,
                hitPoint,
                hitDirection,
                attackPower.GetAtkPower(attackType)
            )
        );
    }

    private void OnDrawGizmos()
    {
        if (box == null || !box.enabled) return;

        Gizmos.color = new Color(0.5f, 0f, 0f, 0.3f);
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawCube(box.center, box.size);
    }
}
