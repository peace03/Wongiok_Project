using UnityEngine;

// 플레이어 체력이 바뀔 때 발행되는 이벤트입니다.
// UI 체력바, 피격 이펙트, 사운드 등은 이 이벤트를 구독해서 반응할 수 있습니다.
public readonly struct PlayerHealthChangedEvent
{
    public readonly float CurrentHP;
    public readonly float MaxHP;

    public PlayerHealthChangedEvent(float currentHP, float maxHP)
    {
        CurrentHP = currentHP;
        MaxHP = maxHP;
    }
}

// 플레이어 체력이 0이 되어 사망했을 때 발행되는 이벤트입니다.
// 사망 UI, 사운드, 화면 효과는 이 이벤트의 DeathInfo를 사용합니다.
public readonly struct PlayerDeadEvent
{
    // 사망 순간의 위치, 원인, 마지막 데미지 정보입니다.
    public readonly DeathInfo DeathInfo;

    public PlayerDeadEvent(DeathInfo deathInfo)
    {
        DeathInfo = deathInfo;
    }
}

// 플레이어가 사망 후 부활했을 때 발행되는 이벤트입니다.
// 체크포인트 부활, 부활 이펙트, UI 복구 흐름에서 사용할 수 있습니다.
public readonly struct PlayerRevivedEvent
{
    // 부활한 플레이어 오브젝트입니다.
    public readonly GameObject PlayerObject;

    // 부활이 완료된 위치입니다.
    public readonly Vector3 RevivePosition;

    // 부활 후 현재 체력입니다.
    public readonly float CurrentHP;

    // 플레이어의 최대 체력입니다.
    public readonly float MaxHP;

    public PlayerRevivedEvent(
        GameObject playerObject,
        Vector3 revivePosition,
        float currentHP,
        float maxHP)
    {
        PlayerObject = playerObject;
        RevivePosition = revivePosition;
        CurrentHP = currentHP;
        MaxHP = maxHP;
    }
}

// 플레이어가 더 높은 번호의 체크포인트를 활성화했을 때 발행되는 이벤트입니다.
// UI, 사운드, 저장 연출은 이 이벤트를 구독해서 처리합니다.
// 플레이어 목숨 수가 변경될 때 발행되는 이벤트입니다.
// UI는 이 이벤트를 구독해서 현재 남은 목숨을 표시합니다.
public readonly struct PlayerLifeChangedEvent
{
    public readonly GameObject PlayerObject;
    public readonly int CurrentLifeCount;
    public readonly int MaxLifeCount;

    public PlayerLifeChangedEvent(GameObject playerObject, int currentLifeCount, int maxLifeCount)
    {
        PlayerObject = playerObject;
        CurrentLifeCount = currentLifeCount;
        MaxLifeCount = maxLifeCount;
    }
}

// 플레이어 목숨이 0이 되는 순간 발행되는 이벤트입니다.
// 게임오버 UI와 연출은 이 이벤트를 구독해서 후처리합니다.
public readonly struct PlayerLifeDepletedEvent
{
    public readonly GameObject PlayerObject;

    public PlayerLifeDepletedEvent(GameObject playerObject)
    {
        PlayerObject = playerObject;
    }
}

// 플레이어 경험치가 변경될 때 발행되는 이벤트입니다.
// UI와 성장 연출은 이 이벤트를 구독해서 현재 경험치 진행도를 표시합니다.
public readonly struct PlayerExperienceChangedEvent
{
    public readonly GameObject PlayerObject;
    public readonly int CurrentLevel;
    public readonly float CurrentExp;
    public readonly float RequiredExp;

    public PlayerExperienceChangedEvent(
        GameObject playerObject,
        int currentLevel,
        float currentExp,
        float requiredExp)
    {
        PlayerObject = playerObject;
        CurrentLevel = currentLevel;
        CurrentExp = currentExp;
        RequiredExp = requiredExp;
    }
}

// 플레이어가 레벨업했을 때 발행되는 이벤트입니다.
// 스킬 선택 구조는 이 이벤트를 받아 다음 단계에서 진입합니다.
public readonly struct PlayerLevelUpEvent
{
    public readonly GameObject PlayerObject;
    public readonly int PreviousLevel;
    public readonly int CurrentLevel;

    public PlayerLevelUpEvent(GameObject playerObject, int previousLevel, int currentLevel)
    {
        PlayerObject = playerObject;
        PreviousLevel = previousLevel;
        CurrentLevel = currentLevel;
    }
}

public readonly struct CheckpointActivatedEvent
{
    // 체크포인트를 활성화한 플레이어 오브젝트입니다.
    public readonly GameObject PlayerObject;

    // 활성화된 체크포인트 오브젝트입니다.
    public readonly GameObject CheckpointObject;

    // 활성화된 체크포인트 번호입니다.
    public readonly int CheckpointNumber;

    // 이 체크포인트가 제공하는 부활 위치입니다.
    public readonly Vector3 RespawnPosition;

    public CheckpointActivatedEvent(
        GameObject playerObject,
        GameObject checkpointObject,
        int checkpointNumber,
        Vector3 respawnPosition)
    {
        PlayerObject = playerObject;
        CheckpointObject = checkpointObject;
        CheckpointNumber = checkpointNumber;
        RespawnPosition = respawnPosition;
    }
}

// 플레이어 피해의 발생 주체를 구분합니다.
public enum PlayerDamageSource
{
    Unknown,
    Boss
}

// 플레이어가 실제 데미지를 받은 뒤 발행되는 이벤트입니다.
// UI, 사운드, 피격 이펙트는 이 이벤트를 구독해서 후처리만 담당합니다.
public readonly struct PlayerDamagedEvent
{
    // 실제로 데미지를 받은 플레이어 오브젝트입니다.
    public readonly GameObject PlayerObject;

    // 피해 적용 후 현재 체력입니다.
    public readonly float CurrentHP;

    // 플레이어의 최대 체력입니다.
    public readonly float MaxHP;

    // 실제 피해를 적용한 공격 주체입니다.
    public readonly PlayerDamageSource Source;

    public PlayerDamagedEvent(
        GameObject playerObject,
        float currentHP,
        float maxHP,
        PlayerDamageSource source = PlayerDamageSource.Unknown)
    {
        PlayerObject = playerObject;
        CurrentHP = currentHP;
        MaxHP = maxHP;
        Source = source;
    }
}

// 플레이어가 공격을 발사했을 때 발행되는 이벤트입니다.
// 사운드, 카메라 흔들림, 발사 이펙트 같은 부가 연출을 분리해서 처리할 수 있습니다.
// 플레이어 회복 아이템 보유량이 바뀔 때 발행되는 이벤트입니다.
// UI, 사운드, 획득 이펙트는 이 이벤트를 구독해서 후처리합니다.
public readonly struct PlayerHealItemCountChangedEvent
{
    // 보유량이 변경된 플레이어 오브젝트입니다.
    public readonly GameObject PlayerObject;

    // 현재 회복 아이템 보유량입니다.
    public readonly int CurrentCount;

    // 회복 아이템 최대 보유량입니다.
    public readonly int MaxCount;

    public PlayerHealItemCountChangedEvent(GameObject playerObject, int currentCount, int maxCount)
    {
        PlayerObject = playerObject;
        CurrentCount = currentCount;
        MaxCount = maxCount;
    }
}

public readonly struct PlayerAttackFiredEvent
{
    // 총알이 생성된 위치입니다.
    public readonly Vector3 FirePosition;

    // 공격이 날아가는 방향입니다.
    public readonly Vector3 AttackDirection;

    // 발사 시점에 계산된 피해량입니다.
    public readonly float Damage;

    public PlayerAttackFiredEvent(Vector3 firePosition, Vector3 attackDirection, float damage)
    {
        FirePosition = firePosition;
        AttackDirection = attackDirection;
        Damage = damage;
    }
}

// 플레이어 총알이 무언가에 닿았을 때 발행되는 이벤트입니다.
// 실제 데미지 적용, 피격 사운드, 충돌 이펙트 등을 한 곳에 묶지 않기 위해 사용합니다.
public readonly struct PlayerBulletHitEvent
{
    // 맞은 GameObject입니다. 체력 컴포넌트 탐색 등에 사용할 수 있습니다.
    public readonly GameObject HitObject;

    // 실제 충돌한 Collider입니다. 피격 부위나 충돌 설정을 확인할 때 사용할 수 있습니다.
    public readonly Collider HitCollider;

    // 충돌이 발생한 위치입니다. 이펙트나 사운드 재생 위치로 사용합니다.
    public readonly Vector3 HitPoint;

    // 총알이 날아오던 방향입니다. 넉백이나 피격 방향 계산에 사용할 수 있습니다.
    public readonly Vector3 AttackDirection;

    // 총알이 가진 피해량입니다.
    public readonly float Damage;

    public PlayerBulletHitEvent(
        GameObject hitObject,
        Collider hitCollider,
        Vector3 hitPoint,
        Vector3 attackDirection,
        float damage)
    {
        HitObject = hitObject;
        HitCollider = hitCollider;
        HitPoint = hitPoint;
        AttackDirection = attackDirection;
        Damage = damage;
    }
}
