using UnityEngine;

public class MonsterHealth : MonoBehaviour, IDamageable, IDeadState
{
    [Header("Health")]
    [SerializeField] private float maxHp = 30f;
    [SerializeField] private bool resetHpOnEnable = true;
    [SerializeField] private bool invulnerable;
    [Header("타격/피격 이펙트 실행할 위치")]
    [SerializeField] private Transform hitEffectPlace;

    private float currentHp;
    private bool isDead;

    public Transform HitEffectPlace => hitEffectPlace != null ? hitEffectPlace : null;

    public float MaxHp
    {
        get { return maxHp; }
    }

    public float CurrentHp
    {
        get { return currentHp; }
    }

    public bool IsDead
    {
        get { return isDead; }
    }

    public bool CanTakeDamage
    {
        get { return !isDead && !invulnerable; }
    }

    // 몬스터가 처음 생성될 때 체력을 초기화합니다
    private void Awake()
    {
        ResetHealth();
    }

    // 몬스터가 활성화될 때 설정에 따라 체력을 초기화합니다
    private void OnEnable()
    {
        if (resetHpOnEnable)
        {
            ResetHealth();
        }
    }

    // 인스펙터에 잘못된 체력이 입력되지 않도록 최소 체력을 보장합니다
    private void OnValidate()
    {
        maxHp = Mathf.Max(1f, maxHp);
    }

    // 김연호 : 피해를 적용하고 체력 변경 및 몬스터 피격 이벤트를 발행합니다
    public void TakeDamage(float amount)
    {
        if (!CanTakeDamage)
        {
            return;
        }

        if (amount <= 0f)
        {
            return;
        }

        currentHp = Mathf.Max(0f, currentHp - amount);

        PublishHealthChangedEvent();
        PublishDamagedEvent();

        if (currentHp <= 0f)
        {
            Die(MonsterDeathCause.PlayerAttack);
        }
    }

    // 몬스터 체력을 최대 체력으로 되돌립니다
    public void ResetHealth()
    {
        maxHp = Mathf.Max(1f, maxHp);
        currentHp = maxHp;
        isDead = false;

        PublishHealthChangedEvent();
    }

    // 몬스터를 즉시 사망 상태로 전환합니다
    public void Kill(
        MonsterDeathCause cause = MonsterDeathCause.PlayerAttack)
    {
        if (isDead)
        {
            return;
        }

        currentHp = 0f;

        PublishHealthChangedEvent();
        Die(cause);
    }

    // 몬스터의 무적 상태를 설정합니다
    public void SetInvulnerable(bool value)
    {
        invulnerable = value;
    }

    // 체력이 0이 된 몬스터를 사망 처리하고 사망 이벤트를 발행합니다
    private void Die(MonsterDeathCause cause)
    {
        if (isDead)
        {
            return;
        }

        isDead = true;

        EventBus<MonsterDeadEvent>.Publish(
            new MonsterDeadEvent(gameObject, cause));
    }

    // 체력 변경 사실과 현재 체력을 이벤트로 전달합니다
    private void PublishHealthChangedEvent()
    {
        EventBus<HealthChangedEvent>.Publish(
            new HealthChangedEvent(
                gameObject,
                currentHp,
                maxHp));
    }

    // 김연호 : 기존 MonsterDamagedEvent를 사용해 피격 대상과 피해 적용 후 체력을 전달합니다
    private void PublishDamagedEvent()
    {
        EventBus<MonsterDamagedEvent>.Publish(
            new MonsterDamagedEvent(
                gameObject,
                currentHp,
                maxHp));
    }
}
