using UnityEngine;

public class BossHitBox : MonoBehaviour, IInitializable
{
    public int Priority => (int)InitOrder.Boss+4;

    [SerializeField] private AttackType AtkType;
    private BossStatus AtkPower;    //공격력 참조
    private bool isTriggered = false;       //중복 공격 방어
    private BoxCollider box;            //gizmos용

    public void Init()
    {
        AtkPower = ServiceLocator.Get<BossStatus>();
        box = GetComponent<BoxCollider>();
    }

    private void OnEnable()
    {
        EventBus<AttackFinishEvent>.action += SetIsTriggered;
    }

    private void OnDisable()
    {
        EventBus<AttackFinishEvent>.action -= SetIsTriggered;
    }

    private void SetIsTriggered(AttackFinishEvent data) { isTriggered = false; }

    private void OnTriggerEnter(Collider other) //공격력 플레이어에게 넘겨주기
    {
        if (other.CompareTag("Player") && !isTriggered)
        {
            isTriggered = true;
            other.GetComponent<IDamageable>().
                TakeDamage(AtkPower.GetAtkPower(AtkType));
        }
    }

    private void OnDrawGizmos()
    {
        if (box != null && box.enabled)
        {
            Gizmos.color = new Color(0.5f, 0f, 0f, 0.3f);
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawCube(box.center, box.size);
        }
    }
}
