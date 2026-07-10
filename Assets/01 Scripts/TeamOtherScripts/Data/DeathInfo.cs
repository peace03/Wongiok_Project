using UnityEngine;

// 플레이어 사망 원인을 구분하기 위한 값입니다.
// 체크포인트, 함정, 낙하사 처리가 추가될 때 분기 기준으로 사용할 수 있습니다.
public enum DeathCause
{
    Unknown,
    Damage,
    Fall,
    Trap
}

// 플레이어 사망 순간의 정보를 이벤트로 전달하기 위한 값 타입입니다.
// UI, 사운드, 화면 효과, 사망 로그가 필요한 정보를 한 번에 받을 수 있게 합니다.
public readonly struct DeathInfo
{
    // 사망한 플레이어 오브젝트입니다.
    public readonly GameObject PlayerObject;

    // 사망이 발생한 위치입니다.
    public readonly Vector3 DeathPosition;

    // 마지막으로 적용된 데미지 정보입니다.
    public readonly DamageInfo LastDamageInfo;

    // 사망 원인입니다.
    public readonly DeathCause Cause;

    // 사망 처리 시점의 현재 체력입니다.
    public readonly float CurrentHP;

    // 플레이어의 최대 체력입니다.
    public readonly float MaxHP;

    public DeathInfo(
        GameObject playerObject,
        Vector3 deathPosition,
        DamageInfo lastDamageInfo,
        DeathCause cause,
        float currentHP,
        float maxHP)
    {
        PlayerObject = playerObject;
        DeathPosition = deathPosition;
        LastDamageInfo = lastDamageInfo;
        Cause = cause;
        CurrentHP = currentHP;
        MaxHP = maxHP;
    }
}
