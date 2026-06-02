// 스킬 종류
public enum SKILL_TYPE
{
    Passive,        // 패시브
    Active          // 액티브
}

// 챕터 종류
public enum CHAPTER_TYPE
{
    First,          // 첫번째
    Second          // 두번째
}

// 패시브 발동 조건 종류
public enum PASSIVE_TRIGGER_TYPE
{
    None,           // 상시
    Triggered       // 조건
}

// 스탯 종류
public enum STAT_TYPE
{
    Health,         // 체력
    AtkPower,       // 공격력
    MoveSpeed,      // 이동 속도
    AtkSpeed        // 공격 속도
}

// 수식 종류
public enum MODIFY_TYPE
{
    Addition,       // 더하기
    Subtraction     // 빼기
}

// 액티브 스킬 종류
public enum ACTIVE_SKILL_TYPE
{
    Projectile,     // 발사체
    Area            // 영역
}

// 스킬 상태
public enum SKILL_STATE
{
    Ready,          // 사용 가능
    CoolTime,       // [사용 후] 쿨타임 중
    Executing,      // 실행 중(지속 사용 중)
    Charging        // [사용 전] 충전 중
}