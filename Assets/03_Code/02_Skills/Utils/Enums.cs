/// <summary>
/// 스킬 종류
/// </summary>
public enum SKILL_TYPE
{
    Passive,        // 패시브
    Active          // 액티브
}

/// <summary>
/// 챕터 종류
/// </summary>
public enum CHAPTER_TYPE
{
    First,          // 첫번째
    Second          // 두번째
}

/// <summary>
/// 액티브 스킬 이펙트 종류
/// </summary>
public enum ACTIVE_SKILL_EFFECT_TYPE
{
    Charging,       // 차징 이펙트
    Muzzle,         // 총구 이펙트
    Trail,          // 궤적 이펙트
    Main,           // 메인(발사체, 범위) 이펙트
    Target,         // 타겟(과녁) 이펙트
    Hit             // 타격/피격 이펙트
}

/// <summary>
/// 패시브 발동 조건 종류
/// </summary>
public enum PASSIVE_TRIGGER_TYPE
{
    None,           // 상시
    Triggered       // 조건
}

/// <summary>
/// 스탯 종류
/// </summary>
public enum STAT_TYPE
{
    Health,         // 체력
    AtkPower,       // 공격력
    MoveSpeed,      // 이동 속도
    AtkSpeed        // 공격 속도
}

/// <summary>
/// 수식 종류
/// </summary>
public enum MODIFY_TYPE
{
    Addition,       // 더하기
    Subtraction     // 빼기
}

/// <summary>
/// 액티브 스킬 종류
/// </summary>
public enum ACTIVE_SKILL_TYPE
{
    Projectile,     // 발사체
    Area            // 범위
}

/// <summary>
/// 스킬 상태
/// </summary>
public enum SKILL_STATE
{
    Ready,          // 사용 가능
    CoolTime,       // [사용 후] 쿨타임 중
    Executing,      // 실행 중
    Charging        // [사용 전] 차징 중
}

/// <summary>
/// 액티브 스킬 슬롯 종류
/// </summary>
public enum ACTIVE_SKILL_SLOT_TYPE
{
    A,              // 첫번째
    S,              // 두번째
    D               // 세번째
}

/// <summary>
/// 액티브 스킬 ID
/// </summary>
public enum ACTIVE_SKILL_ID
{
    Start = 1000,   // 시작
    Magnum,         // 매그넘
    Rifle,          // 라이플(돌격소총)
    Sniper,         // 스나이퍼(저격총)
    End             // 끝
}