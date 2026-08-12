using UnityEngine;

public class BossHitBox : MonoBehaviour, IInitializable
{
    private enum HitBoxPhase
    {
        Inactive,   // 콜라이더를 사용하지 않는 평상시
        RangeCheck, // 패링 가능한 거리인지 플레이어 위치만 확인하는 단계
        Damage      // 실제 피해를 줄 수 있는 공격 프레임
    }

    public int Priority => (int)InitOrder.Boss+4;

    [SerializeField] private AttackType AtkType;
    private BossStatus AtkPower;    //공격력 참조
    private bool isTriggered = false;       //중복 공격 방어
    private BoxCollider box;            //gizmos용
    private HitBoxPhase phase = HitBoxPhase.Inactive; // 현재 이 콜라이더가 맡은 역할
    private Collider currentPlayerCollider;           // 범위 안에 들어온 플레이어 콜라이더
    private IDamageable currentTarget;                // 피해를 적용할 대상
    private PlayerStatus currentPlayerStatus;         // 보스 피해 출처를 전달할 플레이어 상태 참조
    private PlayerParry currentPlayerParry;           // 플레이어에게 범위 안/밖 상태를 전달하기 위한 참조

    public void Init()
    {
        AtkPower = ServiceLocator.Get<BossStatus>();
        box = GetComponent<BoxCollider>();
    }

    private void OnEnable()
    {
        EventBus<AttackFinishEvent>.action += HandleAttackFinished;
    }

    private void OnDisable()
    {
        EventBus<AttackFinishEvent>.action -= HandleAttackFinished;
        ClearRangeState();
    }

    public void BeginRangeCheck()
    {
        // 새 패링 창이 시작될 때 이전 공격에서 남을 수 있는 플레이어 참조를 비운다.
        ClearTrackedPlayer();
        phase = HitBoxPhase.RangeCheck;
    }

    public void EnterDamagePhase()
    {
        // 이미 범위 안에 있던 플레이어도 즉시 맞을 수 있도록 상태 전환 직후 피해를 시도한다.
        phase = HitBoxPhase.Damage;
        TryApplyDamage();
    }

    public void ClearRangeState()
    {
        // 콜라이더를 끄기 전에 플레이어에게 "이 공격 범위에서 벗어남"을 알려준다.
        ClearTrackedPlayer();
        phase = HitBoxPhase.Inactive;
    }

    private void HandleAttackFinished(AttackFinishEvent data)
    {
        // 한 공격이 완전히 끝난 시점에만 중복 피격 방어를 풀어 다음 공격이 다시 피해를 줄 수 있게 한다.
        ClearRangeState();
        isTriggered = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        //플레이어 콜라이더 인

        // RangeCheck와 Damage가 같은 콜라이더를 쓰므로, 먼저 플레이어 정보를 저장한다.
        currentPlayerCollider = other;
        currentTarget = other.GetComponent<IDamageable>();
        currentPlayerStatus = other.GetComponent<PlayerStatus>();
        currentPlayerParry = other.GetComponent<PlayerParry>();
        currentPlayerParry?.SetInBossAttackRange(true);

        // 공격이 이미 피해 프레임이라면 늦게 들어온 플레이어에게도 피해를 적용한다.
        if (phase == HitBoxPhase.Damage)
            TryApplyDamage();
    }

    private void OnTriggerExit(Collider other)
    {
        if (other != currentPlayerCollider) return;
        //플레이어 콜라이더 아웃

        ClearTrackedPlayer();
    }

    private void TryApplyDamage()
    {
        // 같은 공격에서 플레이어가 다시 들어와도 한 번만 맞는다.
        if (isTriggered) return;
        if (currentTarget == null || AtkPower == null) return;

        // TakeDamage 전에 먼저 잠가서, 피해 처리 중 이벤트가 다시 와도 중복 피해가 생기지 않는다.
        isTriggered = true;
        float damage = AtkPower.GetAtkPower(AtkType);

        // 플레이어 상태를 찾았으면 보스 피격 전용 후처리가 구분되도록 출처를 함께 전달한다.
        if (currentPlayerStatus != null)
        {
            currentPlayerStatus.TakeDamage(damage, PlayerDamageSource.Boss);
            return;
        }

        // 플레이어 상태를 찾지 못한 예외 상황에서도 기존 IDamageable 피해 처리는 유지한다.
        currentTarget.TakeDamage(damage);
    }

    private void ClearTrackedPlayer()
    {
        // 콜라이더 비활성화 시 OnTriggerExit가 오지 않을 수 있으므로 직접 범위 밖 상태를 전달한다.
        currentPlayerParry?.SetInBossAttackRange(false);
        currentPlayerCollider = null;
        currentTarget = null;
        currentPlayerStatus = null;
        currentPlayerParry = null;
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
