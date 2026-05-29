using System;
using UnityEngine;

[Serializable]
public class PlayerStatusData : LivingStatus
{
    // 플레이어 전용 점프 힘 스탯입니다.
    public Stat JumpPower = new Stat();

    // 플레이어가 착지 전까지 사용할 수 있는 최대 점프 횟수입니다.
    public Stat MaxJumpCount = new Stat();

    public override void ResetAllModifiers()
    {
        // 공통 생명체 스탯의 보정값을 먼저 초기화합니다.
        base.ResetAllModifiers();

        // 플레이어 전용 스탯 보정값도 함께 초기화합니다.
        JumpPower.ResetModifiers();
        MaxJumpCount.ResetModifiers();
    }
}

// 플레이어는 실제 데미지를 받은 뒤 색상 피드백을 보여야 하므로 HitFlashFeedback을 요구합니다.
[RequireComponent(typeof(HitFlashFeedback))]
public class PlayerStatus : MonoBehaviour
{
    [Header("Base Status")]
    // 인스펙터에서 조절하는 기본 체력입니다.
    [SerializeField] private float baseMaxHP = 100f;

    // 인스펙터에서 조절하는 기본 공격력입니다.
    [SerializeField] private float baseAttackPower = 10f;

    // 인스펙터에서 조절하는 기본 이동 속도입니다.
    [SerializeField] private float baseMoveSpeed = 6f;

    // 인스펙터에서 조절하는 기본 공격 속도입니다.
    [SerializeField] private float baseAttackSpeed = 1f;

    // 인스펙터에서 조절하는 기본 쿨타임 값입니다.
    [SerializeField] private float baseCooldown = 1f;

    // 인스펙터에서 조절하는 기본 점프 힘입니다.
    [SerializeField] private float baseJumpPower = 10f;

    // 인스펙터에서 조절하는 기본 최대 점프 횟수입니다.
    [SerializeField] private float baseMaxJumpCount = 2f;

    // 런타임에서 실제로 사용하는 플레이어 스탯 데이터입니다.
    [SerializeField]
    private PlayerStatusData status = new PlayerStatusData();

    // 외부에서 현재 스탯 데이터 전체를 읽을 수 있게 제공합니다.
    public PlayerStatusData Status => status;

    // 현재 플레이어 상태가 피해를 받을 수 있는지 확인하기 위한 컨트롤러 참조입니다.
    private PlayerController playerController;

    private void Awake()
    {
        // 기존 씬 오브젝트에 PlayerStatus만 붙어 있는 경우도 피격 피드백을 보장합니다.
        EnsureHitFlashFeedback();
        playerController = GetComponent<PlayerController>();

        // 기본값을 스탯 객체에 반영한 뒤 현재 체력을 초기화합니다.
        SetupBaseStatus();
        Init();
    }

    private void OnEnable()
    {
        // 공격 판정 스크립트가 발행한 데미지 요청을 받습니다.
        EventBus<DamageRequestEvent>.action += OnDamageRequested;
    }

    private void OnDisable()
    {
        // 비활성화될 때 구독을 해제해 중복 호출과 참조 누수를 막습니다.
        EventBus<DamageRequestEvent>.action -= OnDamageRequested;
    }

    private void SetupBaseStatus()
    {
        // 인스펙터에 노출된 기본값을 각 Stat의 BaseValue로 복사합니다.
        status.MaxHP.SetBaseValue(baseMaxHP);
        status.AttackPower.SetBaseValue(baseAttackPower);
        status.MoveSpeed.SetBaseValue(baseMoveSpeed);
        status.AttackSpeed.SetBaseValue(baseAttackSpeed);
        status.Cooldown.SetBaseValue(baseCooldown);
        status.JumpPower.SetBaseValue(baseJumpPower);
        status.MaxJumpCount.SetBaseValue(baseMaxJumpCount);
    }

    public void Init()
    {
        // 현재 체력을 최대 체력으로 채우고 UI 등에 변경 이벤트를 알립니다.
        status.Init();
        PublishHealthChanged();
    }

    public void TakeDamage(float damage)
    {
        // 이미 죽은 상태라면 추가 피해를 무시합니다.
        if (status.IsDead) return;

        // 현재 상태가 회피 무적 상태라면 HP 감소와 피격 피드백을 모두 막습니다.
        if (playerController != null && !playerController.CanTakeDamage)
        {
            Debug.Log("회피 성공: 데미지 무시");
            return;
        }

        status.CurrentHP -= damage;

        if (status.CurrentHP < 0f)
            status.CurrentHP = 0f;

        PublishHealthChanged();

        // 직접 TakeDamage가 호출된 경우에도 피격 피드백이 동작하도록 데미지 적용 이벤트를 발행합니다.
        PublishDamageApplied(damage);

        // 피해 적용 후 사망 상태가 되었다면 사망 이벤트를 발행합니다.
        if (status.IsDead)
            EventBus<PlayerDeadEvent>.Publish(new PlayerDeadEvent());
    }

    public void Heal(float amount)
    {
        // 죽은 상태에서는 회복을 적용하지 않습니다.
        if (status.IsDead) return;

        status.CurrentHP += amount;

        // 현재 체력이 최대 체력을 넘지 않도록 제한합니다.
        if (status.CurrentHP > status.MaxHP.FinalValue)
            status.CurrentHP = status.MaxHP.FinalValue;

        PublishHealthChanged();
    }

    public void ResetStatus()
    {
        // 모든 임시 보정값을 제거하고 기본 스탯을 다시 적용합니다.
        status.ResetAllModifiers();
        SetupBaseStatus();
        status.Init();

        PublishHealthChanged();
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

    public float GetAttackSpeed()
    {
        return status.AttackSpeed.FinalValue;
    }

    public float GetCooldown()
    {
        return status.Cooldown.FinalValue;
    }

    public float GetJumpPower()
    {
        return status.JumpPower.FinalValue;
    }

    public int GetMaxJumpCount()
    {
        // 점프 횟수는 정수로 사용해야 하므로 최종 스탯 값을 반올림합니다.
        return Mathf.RoundToInt(status.MaxJumpCount.FinalValue);
    }

    public void AddMaxHPValue(float value)
    {
        // 최대 체력이 바뀌면 현재 체력이 새 최대 체력을 넘지 않도록 보정합니다.
        status.MaxHP.AddValue(value);
        ClampCurrentHP();
        PublishHealthChanged();
    }

    public void AddMaxHPMultiplier(float value)
    {
        // 최대 체력 비율 보정 후 현재 체력 상한도 다시 확인합니다.
        status.MaxHP.AddMultiplier(value);
        ClampCurrentHP();
        PublishHealthChanged();
    }

    public void AddAttackPowerValue(float value)
    {
        status.AttackPower.AddValue(value);
    }

    public void AddAttackPowerMultiplier(float value)
    {
        status.AttackPower.AddMultiplier(value);
    }

    public void AddMoveSpeedValue(float value)
    {
        status.MoveSpeed.AddValue(value);
    }

    public void AddMoveSpeedMultiplier(float value)
    {
        status.MoveSpeed.AddMultiplier(value);
    }

    public void AddAttackSpeedValue(float value)
    {
        status.AttackSpeed.AddValue(value);
    }

    public void AddAttackSpeedMultiplier(float value)
    {
        status.AttackSpeed.AddMultiplier(value);
    }

    public void AddCooldownValue(float value)
    {
        status.Cooldown.AddValue(value);
    }

    public void AddCooldownMultiplier(float value)
    {
        status.Cooldown.AddMultiplier(value);
    }

    public void AddJumpPowerValue(float value)
    {
        status.JumpPower.AddValue(value);
    }

    public void AddJumpPowerMultiplier(float value)
    {
        status.JumpPower.AddMultiplier(value);
    }

    public void AddMaxJumpCountValue(float value)
    {
        status.MaxJumpCount.AddValue(value);
    }

    private void ClampCurrentHP()
    {
        // 최대 체력이 줄어든 상황에서 현재 체력이 새 최대값보다 높게 남지 않도록 합니다.
        if (status.CurrentHP > status.MaxHP.FinalValue)
            status.CurrentHP = status.MaxHP.FinalValue;
    }

    private void OnDamageRequested(DamageRequestEvent eventData)
    {
        // 이벤트는 전역으로 발행되므로, 자기 자신을 대상으로 한 요청만 처리합니다.
        if (!IsTargetSelf(eventData.TargetObject)) return;

        ApplyDamage(eventData);
    }

    private void ApplyDamage(DamageRequestEvent eventData)
    {
        // 이미 죽은 플레이어는 추가 피해를 무시합니다.
        if (status.IsDead) return;

        // 음수 데미지나 0 데미지는 적용하지 않습니다.
        // 현재 상태가 회피 무적 상태라면 HP 감소와 피격 피드백을 모두 막습니다.
        if (playerController != null && !playerController.CanTakeDamage)
        {
            Debug.Log("회피 성공: 데미지 무시");
            return;
        }

        float damage = Mathf.Max(0f, eventData.Damage);
        if (Mathf.Approximately(damage, 0f)) return;

        status.CurrentHP -= damage;

        if (status.CurrentHP < 0f)
            status.CurrentHP = 0f;

        PublishHealthChanged();
        PublishDamageApplied(eventData, damage);

        // HP가 0이 되면 사망 이벤트를 발행합니다.
        if (status.IsDead)
            EventBus<PlayerDeadEvent>.Publish(new PlayerDeadEvent());
    }

    private bool IsTargetSelf(GameObject targetObject)
    {
        // 자식 콜라이더가 맞아도 부모 플레이어가 맞은 것으로 처리합니다.
        if (targetObject == null) return false;

        return targetObject == gameObject || targetObject.transform.IsChildOf(transform);
    }

    private void PublishDamageApplied(float damage)
    {
        // 외부에서 직접 TakeDamage를 호출한 경우를 위한 간단한 피격 완료 이벤트입니다.
        EventBus<DamageAppliedEvent>.Publish(
            new DamageAppliedEvent(
                gameObject,
                null,
                null,
                transform.position,
                Vector3.zero,
                damage,
                status.CurrentHP,
                status.MaxHP.FinalValue
            )
        );
    }

    private void PublishDamageApplied(DamageRequestEvent eventData, float damage)
    {
        // DamageRequestEvent에서 받은 피격 정보를 유지한 채 피격 완료 이벤트를 발행합니다.
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

    private void PublishHealthChanged()
    {
        // 체력 변경 사실을 이벤트로 알려 UI나 다른 시스템이 결합 없이 반응하게 합니다.
        EventBus<PlayerHealthChangedEvent>.Publish(
            new PlayerHealthChangedEvent(
                status.CurrentHP,
                status.MaxHP.FinalValue
            )
        );
    }
}
