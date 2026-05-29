using System;
using UnityEngine;

[Serializable]
public class MonsterStatusData : LivingStatus
{
}

// 몬스터의 HP와 기본 스탯을 관리하는 컴포넌트입니다.
// 공격 판정이 발행한 DamageRequestEvent를 받아 자기 자신이 대상이면 실제 HP를 깎습니다.
[RequireComponent(typeof(HitFlashFeedback))]
public class MonsterStatus : MonoBehaviour
{
    [Header("Base Status")]
    // 테스트 몬스터의 기본 체력입니다.
    [SerializeField] private float baseMaxHP = 30f;

    // 이후 몬스터 공격력이 스탯 기반으로 필요할 때 사용할 기본 공격력입니다.
    [SerializeField] private float baseAttackPower = 10f;

    // 이동 속도 스탯입니다. 현재 ParryTestMonster의 돌진은 별도 값으로 움직입니다.
    [SerializeField] private float baseMoveSpeed = 3f;

    // 공격 속도 스탯입니다. 이후 공격 주기 보정에 사용할 수 있습니다.
    [SerializeField] private float baseAttackSpeed = 1f;

    // 쿨다운 스탯입니다. 이후 스킬 쿨타임 보정에 사용할 수 있습니다.
    [SerializeField] private float baseCooldown = 1f;

    // 실제 전투 중 사용되는 몬스터 스탯 데이터입니다.
    [SerializeField]
    private MonsterStatusData status = new MonsterStatusData();

    public MonsterStatusData Status => status;

    private void Awake()
    {
        // 피격 색상 피드백이 빠져 있으면 런타임에서 붙여 테스트가 막히지 않게 합니다.
        EnsureHitFlashFeedback();

        // 인스펙터 기본값을 Stat 객체에 반영하고 현재 체력을 채웁니다.
        SetupBaseStatus();
        Init();
    }

    private void OnEnable()
    {
        // 공격 판정이 발행한 데미지 요청을 받습니다.
        EventBus<DamageRequestEvent>.action += OnDamageRequested;
    }

    private void OnDisable()
    {
        EventBus<DamageRequestEvent>.action -= OnDamageRequested;
    }

    private void SetupBaseStatus()
    {
        // 인스펙터에서 조절한 값을 LivingStatus의 공통 Stat에 복사합니다.
        status.MaxHP.SetBaseValue(baseMaxHP);
        status.AttackPower.SetBaseValue(baseAttackPower);
        status.MoveSpeed.SetBaseValue(baseMoveSpeed);
        status.AttackSpeed.SetBaseValue(baseAttackSpeed);
        status.Cooldown.SetBaseValue(baseCooldown);
    }

    public void Init()
    {
        // 현재 체력을 최대 체력으로 채우고 체력 변경 이벤트를 발행합니다.
        status.Init();
        PublishHealthChanged();
    }

    public void TakeDamage(float damage)
    {
        // 디버그나 테스트 코드에서 직접 데미지를 줄 수 있도록 남겨둔 진입점입니다.
        ApplyDamage(
            new DamageRequestEvent(
                gameObject,
                null,
                null,
                transform.position,
                Vector3.zero,
                damage
            )
        );
    }

    public float GetMaxHP()
    {
        return status.MaxHP.FinalValue;
    }

    public float GetCurrentHP()
    {
        return status.CurrentHP;
    }

    public float GetAttackPower()
    {
        return status.AttackPower.FinalValue;
    }

    public float GetMoveSpeed()
    {
        return status.MoveSpeed.FinalValue;
    }

    private void OnDamageRequested(DamageRequestEvent eventData)
    {
        // 이벤트가 전역으로 발행되므로, 자기 자신을 대상으로 한 요청만 처리합니다.
        if (!IsTargetSelf(eventData.TargetObject)) return;

        ApplyDamage(eventData);
    }

    private void ApplyDamage(DamageRequestEvent eventData)
    {
        // 이미 죽은 몬스터는 추가 피해를 무시합니다.
        if (status.IsDead) return;

        // 음수 데미지나 0 데미지는 적용하지 않습니다.
        float damage = Mathf.Max(0f, eventData.Damage);
        if (Mathf.Approximately(damage, 0f)) return;

        status.CurrentHP -= damage;

        if (status.CurrentHP < 0f)
            status.CurrentHP = 0f;

        PublishHealthChanged();
        PublishDamageApplied(eventData, damage);

        // HP가 0이 되면 사망 이벤트를 발행합니다.
        if (status.IsDead)
            EventBus<MonsterDeadEvent>.Publish(new MonsterDeadEvent(gameObject));
    }

    private bool IsTargetSelf(GameObject targetObject)
    {
        // 자식 콜라이더가 맞아도 부모 몬스터가 맞은 것으로 처리합니다.
        if (targetObject == null) return false;

        return targetObject == gameObject || targetObject.transform.IsChildOf(transform);
    }

    private void PublishHealthChanged()
    {
        // UI나 디버그 표시가 몬스터 체력을 구독할 수 있게 알립니다.
        EventBus<MonsterHealthChangedEvent>.Publish(
            new MonsterHealthChangedEvent(
                gameObject,
                status.CurrentHP,
                status.MaxHP.FinalValue
            )
        );
    }

    private void PublishDamageApplied(DamageRequestEvent eventData, float damage)
    {
        // 실제 HP가 감소한 뒤 피격 피드백용 이벤트를 발행합니다.
        EventBus<DamageAppliedEvent>.Publish(
            new DamageAppliedEvent(
                gameObject,
                eventData.HitCollider,
                eventData.AttackerObject,
                eventData.HitPoint,
                eventData.AttackDirection,
                damage,
                status.CurrentHP,
                status.MaxHP.FinalValue
            )
        );
    }

    private void EnsureHitFlashFeedback()
    {
        // RequireComponent는 새로 붙일 때만 보장되므로 기존 오브젝트를 위해 한 번 더 확인합니다.
        if (GetComponent<HitFlashFeedback>() != null) return;

        gameObject.AddComponent<HitFlashFeedback>();
    }
}
