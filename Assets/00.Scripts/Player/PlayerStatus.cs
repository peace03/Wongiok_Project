using System;
using System.Collections;
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

// 플레이어는 피격 피드백과 체크포인트 부활 기록을 함께 사용합니다.
public class PlayerStatus : MonoBehaviour, IDamageable
{
    #region 플레이어가 가지는 스텟, 정보, 참조값
    // 피격 직후 입력을 막는 경직 시간입니다.
    public const float HitStunDuration = 0.2f;

    // 경직이 끝난 뒤 추가로 유지되는 무적 시간입니다.
    public const float HitInvincibleDuration = 0.5f;

    // 피격 경직 동안 뒤로 밀려나는 거리입니다.
    public const float HitKnockbackDistance = 0.3f;

    // 1차 구현에서 사용하는 사망 후 자동 부활 대기 시간입니다.
    private const float ReviveDelay = 2f;

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

    // 사망 후 부활 위치를 가져오기 위한 체크포인트 기록 참조입니다.
    private PlayerCheckpointTracker checkpointTracker;

    // 체크포인트 부활 시 회복 아이템 수량을 복원하기 위한 참조입니다.
    private PlayerHealItemInventory healItemInventory;

    // 부활할 때마다 목숨을 차감하기 위한 참조입니다.
    private PlayerLifeTracker lifeTracker;

    // 보스 근접 공격이 들어오는 순간 패링 성공 여부를 확인하기 위한 참조입니다.
    private PlayerParry playerParry;

    // 피격 후 무적이 끝나는 시각입니다.
    private float invincibleEndTime;

    // 사망 처리 중인지 확인해 회복, 추가 피격, 중복 부활을 막습니다.
    private bool isDeathProcessing;

    // 사망 후 부활을 기다리는 코루틴 핸들입니다.
    private Coroutine reviveRoutine;
    #endregion
    private void Awake()
    {
        // 실제 체력 초기화와 체력 이벤트 발행은 PlayerInitializer에서 순서를 보장해 처리합니다.
        CacheRequiredReferences();
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
        Initialize(playerController, checkpointTracker);
        PublishInitialHealth();
    }

    public void Initialize(PlayerController controller, PlayerCheckpointTracker tracker)
    {
        // PlayerInitializer가 넘겨준 참조를 우선 사용하고, 비어 있으면 같은 오브젝트에서 보강합니다.
        EnsureHitFlashFeedback();
        EnsurePlayerCheckpointTracker();
        playerController = controller != null ? controller : GetComponent<PlayerController>();
        checkpointTracker = tracker != null ? tracker : GetComponent<PlayerCheckpointTracker>();
        healItemInventory = GetComponent<PlayerHealItemInventory>();
        lifeTracker = GetComponent<PlayerLifeTracker>();
        playerParry = GetComponent<PlayerParry>();

        // 인스펙터 기본값을 Stat에 반영한 뒤 현재 체력을 최대 체력으로 맞춥니다.
        SetupBaseStatus();
        status.Init();
        isDeathProcessing = false;
        invincibleEndTime = 0f;
    }

    public void PublishInitialHealth()
    {
        // 모든 플레이어 초기화가 끝난 뒤 UI/사운드가 읽을 수 있도록 마지막에 체력 이벤트를 발행합니다.
        PublishHealthChanged();
    }

    public void TakeDamage(float damage)
    {
        // 보스 히트박스가 float 데미지로 들어오는 순간 패링 성공 여부를 먼저 확인합니다.
        //if (TryConsumeBossParry()) return;

        // 디버그나 테스트 코드에서 숫자만 넘겨도 같은 데미지 흐름을 타도록 감쌉니다.
        TakeDamage(
            new DamageInfo(
                gameObject,
                null,
                null,
                transform.position,
                Vector3.zero,
                damage
            )
        );
    }
    // 플레이 hp 100 -> 패시브 1레벨 -> hp 120 -> 패시브 2레벨 -> 패시브 1레벨 제거 -> hp 100 -> 패시브 2렙 추가
    // hp 100/110

    public void TakeDamage(DamageInfo damageInfo)
    {
        #region 데미지 처리
        // 다른 대상용 DamageInfo가 잘못 전달된 경우에는 처리하지 않습니다.
        if (!IsTargetSelf(damageInfo.TargetObject)) return;

        // 사망 처리 중에는 추가 피해와 피격 상태 진입을 모두 무시합니다.
        if (isDeathProcessing) return;

        // 이미 죽은 플레이어는 추가 피해를 무시합니다.
        if (status.IsDead) return;

        // 현재 상태가 회피 무적 상태라면 HP 감소와 피격 피드백을 모두 막습니다.
        if (playerController != null && !playerController.CanTakeDamage)
        {
            Debug.Log("회피 성공: 데미지 무시");
            return;
        }

        // 피격 후 무적 시간 동안에는 추가 데미지와 넉백을 막습니다.
        if (Time.time < invincibleEndTime)
        {
            Debug.Log("피격 무적: 데미지 무시");
            return;
        }
        #endregion
        // 음수 데미지나 0 데미지는 적용하지 않습니다.
        float damage = Mathf.Max(0f, damageInfo.Damage);
        if (Mathf.Approximately(damage, 0f)) return;

        status.CurrentHP -= damage;

        if (status.CurrentHP < 0f)
        {
            status.CurrentHP = 0f;
        }

        PublishHealthChanged();
        PublishDamaged(damageInfo, damage);

        // 피해 적용 후 사망 상태가 되었다면 사망 이벤트를 발행합니다.
        if (status.IsDead)
        {
            HandleDeath(damageInfo);
            return;
        }

        StartHitInvincibility();

        // 살아 있다면 피격 상태로 진입해 경직과 넉백을 처리합니다.
        if (playerController != null)
        {
            playerController.EnterHitState(damageInfo);
        }
    }

    public void Heal(float amount)
    {
        // 죽었거나 사망 처리 중일 때는 일반 회복을 적용하지 않습니다.
        if (status.IsDead || isDeathProcessing) return;

        status.CurrentHP += amount;

        // 현재 체력이 최대 체력을 넘지 않도록 제한합니다.
        if (status.CurrentHP > status.MaxHP.FinalValue)
        {
            status.CurrentHP = status.MaxHP.FinalValue;
        }

        PublishHealthChanged();
    }

    public void ResetStatus()
    {
        // 모든 임시 보정값을 제거하고 기본 스탯을 다시 적용합니다.
        StopReviveRoutine();
        isDeathProcessing = false;
        invincibleEndTime = 0f;
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
        {
            status.CurrentHP = status.MaxHP.FinalValue;
        }
    }

    private bool IsTargetSelf(GameObject targetObject)
    {
        // 대상 정보가 비어 있으면 직접 호출로 보고 현재 플레이어에게 적용합니다.
        if (targetObject == null) return true;

        // 자식 콜라이더가 맞아도 부모 플레이어가 맞은 것으로 처리합니다.
        return targetObject == gameObject || targetObject.transform.IsChildOf(transform);
    }

    private void PublishDamaged(DamageInfo damageInfo, float appliedDamage)
    {
        // 후처리 시스템이 실제 적용된 데미지와 피격 정보를 함께 받을 수 있게 알립니다.
        EventBus<PlayerDamagedEvent>.Publish(
            new PlayerDamagedEvent(
                gameObject,
                new DamageInfo(
                    gameObject,
                    damageInfo.HitCollider,
                    damageInfo.AttackerObject,
                    damageInfo.HitPoint,
                    damageInfo.HitDirection,
                    appliedDamage
                ),
                status.CurrentHP,
                status.MaxHP.FinalValue
            )
        );
    }

    private void StartHitInvincibility()
    {
        // 경직 0.2초와 이후 무적 0.5초를 합산해 재피격을 막습니다.
        invincibleEndTime = Time.time + HitStunDuration + HitInvincibleDuration;
    }

    private void HandleDeath(DamageInfo lastDamageInfo)
    {
        // 사망 이벤트와 상태 진입, 부활 대기를 한 번만 실행합니다.
        if (isDeathProcessing) return;

        isDeathProcessing = true;
        invincibleEndTime = 0f;

        DeathInfo deathInfo = CreateDeathInfo(lastDamageInfo);

        EventBus<PlayerDeadEvent>.Publish(new PlayerDeadEvent(deathInfo));

        if (playerController != null)
            playerController.EnterDeathState(deathInfo);

        StopReviveRoutine();
        reviveRoutine = StartCoroutine(ReviveAfterDelay());
    }

    private DeathInfo CreateDeathInfo(DamageInfo lastDamageInfo)
    {
        // 현재는 데미지로 인한 사망만 연결되어 있으며, 낙하/함정은 추후 별도 진입점에서 Cause를 바꿉니다.
        return new DeathInfo(
            gameObject,
            transform.position,
            lastDamageInfo,
            DeathCause.Damage,
            status.CurrentHP,
            status.MaxHP.FinalValue
        );
    }

    private IEnumerator ReviveAfterDelay()
    {
        // 사망 연출과 UI가 반응할 시간을 확보한 뒤 체크포인트 위치에서 부활합니다.
        yield return new WaitForSeconds(ReviveDelay);

        ReviveAtCheckpoint();
        reviveRoutine = null;
    }

    private void ReviveAtCheckpoint()
    {
        // 체크포인트를 밟지 않았다면 트래커가 시작 위치를 반환합니다.
        Vector3 revivePosition = GetRevivePosition();

        if (playerController != null)
        {
            playerController.TeleportTo(revivePosition);
        }
        else
        {
            transform.position = revivePosition;
        }

        // 부활 위치 이동 후 최대 체력으로 회복합니다.
        // 체크포인트가 저장한 체력/회복 아이템 스냅샷으로 복원하고 목숨을 차감합니다.
        status.CurrentHP = GetReviveHP();
        RestoreHealItemCount();
        ConsumeLifeOnRevive();
        isDeathProcessing = false;
        invincibleEndTime = 0f;

        PublishHealthChanged();
        PublishRevived(revivePosition);

        if (playerController != null)
            playerController.ExitDeathStateAfterRevive();
    }

    private Vector3 GetRevivePosition()
    {
        // 체크포인트 기록이 없으면 현재 위치를 안전한 대체값으로 사용합니다.
        if (checkpointTracker == null)
            checkpointTracker = GetComponent<PlayerCheckpointTracker>();

        if (checkpointTracker == null) return transform.position;

        return checkpointTracker.RespawnPosition;
    }

    private float GetReviveHP()
    {
        // 체크포인트가 저장한 체력을 현재 최대 체력 범위 안으로 보정해 복원합니다.
        if (checkpointTracker == null)
            checkpointTracker = GetComponent<PlayerCheckpointTracker>();

        float reviveHP = checkpointTracker != null ? checkpointTracker.SavedHP : status.MaxHP.FinalValue;
        return Mathf.Clamp(reviveHP, 0f, status.MaxHP.FinalValue);
    }

    private void RestoreHealItemCount()
    {
        // 체크포인트가 저장한 회복 아이템 보유량을 인벤토리에 복원합니다.
        if (healItemInventory == null)
            healItemInventory = GetComponent<PlayerHealItemInventory>();

        if (checkpointTracker == null)
            checkpointTracker = GetComponent<PlayerCheckpointTracker>();

        if (healItemInventory == null || checkpointTracker == null) return;

        healItemInventory.RestoreCount(checkpointTracker.SavedHealItemCount);
    }

    private void ConsumeLifeOnRevive()
    {
        // 부활이 실제로 진행되는 시점에 목숨을 1 차감합니다.
        if (lifeTracker == null)
            lifeTracker = GetComponent<PlayerLifeTracker>();

        if (lifeTracker == null) return;

        lifeTracker.ConsumeLifeOnRevive();
    }

    private void PublishRevived(Vector3 revivePosition)
    {
        // UI, 사운드, 이펙트가 부활 시점을 구독할 수 있게 알립니다.
        EventBus<PlayerRevivedEvent>.Publish(
            new PlayerRevivedEvent(
                gameObject,
                revivePosition,
                status.CurrentHP,
                status.MaxHP.FinalValue
            )
        );
    }

    private void StopReviveRoutine()
    {
        // 상태 초기화나 재설정 시 기존 부활 대기가 남지 않도록 정리합니다.
        if (reviveRoutine == null) return;

        StopCoroutine(reviveRoutine);
        reviveRoutine = null;
    }

    private void CacheRequiredReferences()
    {
        // PlayerInitializer 호출 전에도 같은 오브젝트의 필수 참조만 미리 잡아둡니다.
        EnsureHitFlashFeedback();
        EnsurePlayerCheckpointTracker();
        playerController = GetComponent<PlayerController>();
        checkpointTracker = GetComponent<PlayerCheckpointTracker>();
        healItemInventory = GetComponent<PlayerHealItemInventory>();
        lifeTracker = GetComponent<PlayerLifeTracker>();
        playerParry = GetComponent<PlayerParry>();
    }

    private bool TryConsumeBossParry()
    {
        // 테스트 씬처럼 초기화 순서가 어긋난 경우에도 같은 오브젝트에서 한 번 더 보강합니다.
        if (playerParry == null)
        {
            playerParry = GetComponent<PlayerParry>();
        }

        return playerParry != null && playerParry.TryConsumeBossParry();
    }

    private void EnsureHitFlashFeedback()
    {
        // RequireComponent는 새로 붙일 때만 보장되므로 기존 오브젝트를 위해 한 번 더 확인합니다.
        if (GetComponent<HitFlashFeedback>() != null) return;

        gameObject.AddComponent<HitFlashFeedback>();
    }

    private void EnsurePlayerCheckpointTracker()
    {
        // 기존 플레이어 오브젝트도 체크포인트 부활 기록을 가질 수 있게 보강합니다.
        if (GetComponent<PlayerCheckpointTracker>() != null) return;

        gameObject.AddComponent<PlayerCheckpointTracker>();
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
