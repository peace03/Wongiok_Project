// Enum 확장 기능
public static class EnumExtensions
{
    // 스킬 종류 한국어 반환 함수
    public static string ToKoreanString(this SKILL_TYPE type)
    {
        return type switch
        {
            SKILL_TYPE.Passive => "패시브",
            SKILL_TYPE.Active => "액티브",
            _ => ""                               // default
        };
    }

    // 챕터 종류 한국어 반환 함수
    public static string ToKoreanString(this CHAPTER_TYPE type)
    {
        return type switch
        {
            CHAPTER_TYPE.First  => "챕터 1",
            CHAPTER_TYPE.Second => "챕터 2",
            _                   => ""
        };
    }

    // 패시브 발동 조건 종류 한국어 반환 함수
    public static string ToKoreanString(this PASSIVE_TRIGGER_TYPE type)
    {
        return type switch
        {
            PASSIVE_TRIGGER_TYPE.None       => "상시",
            PASSIVE_TRIGGER_TYPE.Triggered  => "조건",
            _                               => ""
        };
    }

    // 스탯 종류 한국어 반환 함수
    public static string ToKoreanString(this STAT_TYPE type)
    {
        return type switch
        {
            STAT_TYPE.Health    => "체력",
            STAT_TYPE.AtkPower  => "공격력",
            STAT_TYPE.MoveSpeed => "이동 속도",
            STAT_TYPE.AtkSpeed  => "공격 속도",
            _                   => ""
        };
    }

    // 수식 종류 한국어 반환 함수
    public static string ToKoreanString(this MODIFY_TYPE type)
    {
        return type switch
        {
            MODIFY_TYPE.Addition    => "+",
            MODIFY_TYPE.Subtraction => "-",
            _                       => ""
        };
    }

    public static string ToKoreanString(this ACTIVE_SKILL_TYPE type)
    {
        return type switch
        {
            ACTIVE_SKILL_TYPE.Projectile    => "발사체",
            ACTIVE_SKILL_TYPE.Area          => "영역",
            _                               => ""
        };
    }
}