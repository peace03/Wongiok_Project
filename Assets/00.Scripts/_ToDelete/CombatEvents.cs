using UnityEngine;

// 공격 판정이 실제 HP를 직접 깎지 않고, 데미지 처리를 요청할 때 발행하는 이벤트입니다.
// 총알, 돌진, 근접 공격 같은 공격 판정 스크립트는 이 이벤트만 발행하고,
// PlayerStatus나 MonsterStatus가 자신에게 온 요청인지 확인한 뒤 HP를 줄입니다.
public readonly struct DamageRequestEvent
{
    // 데미지를 받을 후보 오브젝트입니다. Status 쪽에서 자기 자신인지 검사합니다.
    public readonly GameObject TargetObject;

    // 실제로 맞은 콜라이더입니다. 피격 위치, 부위 판정, 패링 판정 등에 사용할 수 있습니다.
    public readonly Collider HitCollider;

    // 공격을 만든 주체입니다. 반격, 어그로, 킬 로그 같은 처리에 사용할 수 있습니다.
    public readonly GameObject AttackerObject;

    // 공격이 닿은 월드 좌표입니다. 이펙트나 사운드 위치로 사용할 수 있습니다.
    public readonly Vector3 HitPoint;

    // 공격이 들어온 방향입니다. 넉백, 패링 방향 검사 등에 사용할 수 있습니다.
    public readonly Vector3 AttackDirection;

    // 요청된 피해량입니다. 실제 적용 여부는 Status 쪽에서 결정합니다.
    public readonly float Damage;

    public DamageRequestEvent(
        GameObject targetObject,
        Collider hitCollider,
        GameObject attackerObject,
        Vector3 hitPoint,
        Vector3 attackDirection,
        float damage)
    {
        TargetObject = targetObject;
        HitCollider = hitCollider;
        AttackerObject = attackerObject;
        HitPoint = hitPoint;
        AttackDirection = attackDirection;
        Damage = damage;
    }
}

// HP가 실제로 감소한 뒤 발행하는 이벤트입니다.
// 색 변경, 카메라 흔들림, 피격 사운드처럼 "맞은 뒤" 반응하는 기능들이 구독합니다.
public readonly struct DamageAppliedEvent
{
    // 실제로 HP가 감소한 오브젝트입니다.
    public readonly GameObject VictimObject;

    // DamageRequestEvent에서 넘어온 피격 콜라이더입니다.
    public readonly Collider HitCollider;

    // DamageRequestEvent에서 넘어온 공격 주체입니다.
    public readonly GameObject AttackerObject;

    // 실제 피격 위치입니다.
    public readonly Vector3 HitPoint;

    // 실제 공격 방향입니다.
    public readonly Vector3 AttackDirection;

    // 최종 적용된 피해량입니다.
    public readonly float Damage;

    // 피해 적용 후 현재 체력입니다.
    public readonly float CurrentHP;

    // 피해 대상의 최대 체력입니다.
    public readonly float MaxHP;

    public DamageAppliedEvent(
        GameObject victimObject,
        Collider hitCollider,
        GameObject attackerObject,
        Vector3 hitPoint,
        Vector3 attackDirection,
        float damage,
        float currentHP,
        float maxHP)
    {
        VictimObject = victimObject;
        HitCollider = hitCollider;
        AttackerObject = attackerObject;
        HitPoint = hitPoint;
        AttackDirection = attackDirection;
        Damage = damage;
        CurrentHP = currentHP;
        MaxHP = maxHP;
    }
}

// 몬스터 체력이 바뀌었을 때 UI나 디버그 표시가 구독할 수 있는 이벤트입니다.
public readonly struct MonsterHealthChangedEvent
{
    public readonly GameObject MonsterObject;
    public readonly float CurrentHP;
    public readonly float MaxHP;

    public MonsterHealthChangedEvent(GameObject monsterObject, float currentHP, float maxHP)
    {
        MonsterObject = monsterObject;
        CurrentHP = currentHP;
        MaxHP = maxHP;
    }
}

// 몬스터 체력이 0 이하가 되었을 때 발행하는 이벤트입니다.
public readonly struct MonsterDeadEvent
{
    public readonly GameObject MonsterObject;

    public MonsterDeadEvent(GameObject monsterObject)
    {
        MonsterObject = monsterObject;
    }
}
