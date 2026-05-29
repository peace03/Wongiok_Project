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
// 현재는 데이터가 필요 없으므로 비어 있는 이벤트 구조체로 선언되어 있습니다.
public readonly struct PlayerDeadEvent
{
}

// 플레이어가 공격을 발사했을 때 발행되는 이벤트입니다.
// 사운드, 카메라 흔들림, 발사 이펙트 같은 부가 연출을 분리해서 처리할 수 있습니다.
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
