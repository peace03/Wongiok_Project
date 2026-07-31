using UnityEngine;

//보스가 죽었을 때 보스 콜라이더 off
//보스 활성화 시 콜라이더 on
[RequireComponent(typeof(CapsuleCollider))]
[RequireComponent(typeof(Rigidbody))]
public class BossBodyColliderDisabler : MonoBehaviour
{
    // 보스 루트의 물리 충돌체를 한 번만 찾아 캐시한다.
    private CapsuleCollider bodyCollider;
    // 사망 중 중력과 충돌 반응을 멈추기 위한 Rigidbody 참조를 캐시한다.
    private Rigidbody bodyRigidbody;

    // 이벤트 구독 전에 같은 오브젝트의 충돌체와 Rigidbody 참조를 준비한다.
    private void Awake()
    {
        bodyCollider = GetComponent<CapsuleCollider>();
        bodyRigidbody = GetComponent<Rigidbody>();
    }

    // 보스가 활성화될 때 사망 이벤트 수신을 시작하고, 재사용 시 몸체 충돌체를 복구한다.
    private void OnEnable()
    {
        EventBus<BossDeadEvent>.action += DisableBodyCollider;

        // 이전 사망 연출로 꺼진 충돌체를 다음 보스 활성화 시점에 다시 켠다.
        if (bodyCollider != null)
            bodyCollider.enabled = true;

        // 다음 전투에서는 중력과 이동 물리가 정상적으로 동작하도록 복구한다.
        if (bodyRigidbody != null)
            bodyRigidbody.isKinematic = false;
    }

    // 보스가 비활성화될 때 이벤트 구독을 해제해 중복 콜백을 방지한다.
    private void OnDisable()
    {
        EventBus<BossDeadEvent>.action -= DisableBodyCollider;
    }

    // 보스 사망 이벤트를 받으면 몸체 충돌을 끄고 사망 연출 위치를 물리적으로 고정한다.
    private void DisableBodyCollider(BossDeadEvent data)
    {
        if (bodyCollider != null)
            bodyCollider.enabled = false;

        if (bodyRigidbody != null)
        {
            // 남아 있는 속도를 제거해 사망 직전 이동 관성이 연출에 남지 않게 한다.
            bodyRigidbody.linearVelocity = Vector3.zero;
            // 콜라이더가 꺼진 뒤에도 중력으로 지면 아래로 떨어지지 않게 물리 시뮬레이션을 멈춘다.
            bodyRigidbody.isKinematic = true;
        }
    }
}
