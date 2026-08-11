// Enum 확장 기능
public static class EnumExtensions
{
    /// <summary>
    /// 스킬 종류 한국어 반환 함수
    /// </summary>
    /// <param name="type">스킬 종류</param>
    public static string ToKoreanString(this SKILL_TYPE type)
    {
        return type switch
        {
            SKILL_TYPE.Passive                      => "패시브",
            SKILL_TYPE.Active                       => "액티브",
            _                                       => ""             // default
        };
    }

    /// <summary>
    /// 챕터 종류 한국어 반환 함수
    /// </summary>
    /// <param name="type">챕터 종류</param>
    public static string ToKoreanString(this CHAPTER_TYPE type)
    {
        return type switch
        {
            CHAPTER_TYPE.First                      => "챕터 1",
            CHAPTER_TYPE.Second                     => "챕터 2",
            _                                       => ""
        };
    }

    /// <summary>
    /// 액티브 스킬 이펙트 종류 한국어 반환 함수
    /// </summary>
    /// <param name="type">액티브 스킬 이펙트 종류</param>
    public static string ToKoreanString(this ACTIVE_SKILL_EFFECT_TYPE type)
    {
        return type switch
        {
            ACTIVE_SKILL_EFFECT_TYPE.Charging       => "차징 이펙트",
            ACTIVE_SKILL_EFFECT_TYPE.Muzzle         => "총구 이펙트",
            ACTIVE_SKILL_EFFECT_TYPE.Trail          => "궤적 이펙트",
            ACTIVE_SKILL_EFFECT_TYPE.Main           => "메인 이펙트",
            ACTIVE_SKILL_EFFECT_TYPE.Target         => "타겟 이펙트",
            ACTIVE_SKILL_EFFECT_TYPE.Hit            => "타격/피격 이펙트",
            _                                       => ""
        };
    }

    /// <summary>
    /// 패시브 발동 조건 종류 한국어 반환 함수
    /// </summary>
    /// <param name="type">패시브 발동 조건 종류</param>
    public static string ToKoreanString(this PASSIVE_TRIGGER_TYPE type)
    {
        return type switch
        {
            PASSIVE_TRIGGER_TYPE.None               => "상시",
            PASSIVE_TRIGGER_TYPE.Triggered          => "조건",
            _                                       => ""
        };
    }

    /// <summary>
    /// 스탯 종류 한국어 반환 함수
    /// </summary>
    /// <param name="type">스탯 종류</param>
    public static string ToKoreanString(this STAT_TYPE type)
    {
        return type switch
        {
            STAT_TYPE.Health                        => "체력",
            STAT_TYPE.AtkPower                      => "공격력",
            STAT_TYPE.MoveSpeed                     => "이동 속도",
            STAT_TYPE.AtkSpeed                      => "공격 속도",
            _                                       => type.ToString()
        };
    }

    /// <summary>
    /// 수식 종류 한국어 반환 함수
    /// </summary>
    /// <param name="type">수식 종류</param>
    public static string ToKoreanString(this MODIFY_TYPE type)
    {
        return type switch
        {
            MODIFY_TYPE.Addition                    => "+",
            MODIFY_TYPE.Subtraction                 => "-",
            MODIFY_TYPE.Multiplier                  => "배",
            _                                       => ""
        };
    }

    /// <summary>
    /// 액티브 스킬 종류 한국어 반환 함수
    /// </summary>
    /// <param name="type">액티브 스킬 종류</param>
    public static string ToKoreanString(this ACTIVE_SKILL_TYPE type)
    {
        return type switch
        {
            ACTIVE_SKILL_TYPE.Projectile            => "발사체",
            ACTIVE_SKILL_TYPE.Area                  => "범위",
            _                                       => ""
        };
    }

    /// <summary>
    /// 스킬 상태 한국어 반환 함수
    /// </summary>
    /// <param name="state">스킬 상태</param>
    public static string ToKoreanString(this SKILL_STATE state)
    {
        return state switch
        {
            SKILL_STATE.Ready                       => "사용 가능",
            SKILL_STATE.CoolTime                    => "쿨타임 중",
            SKILL_STATE.Executing                   => "사용 중",
            SKILL_STATE.Charging                    => "차징 중",
            _                                       => ""
        };
    }

    /// <summary>
    /// 액티브 스킬 슬롯 종류 한국어 반환 함수
    /// </summary>
    /// <param name="type">액티브 스킬 슬롯 종류</param>
    public static string ToKoreanString(this ACTIVE_SKILL_SLOT_TYPE type)
    {
        return type switch
        {
            ACTIVE_SKILL_SLOT_TYPE.A                => "슬롯 A",
            ACTIVE_SKILL_SLOT_TYPE.S                => "슬롯 S",
            ACTIVE_SKILL_SLOT_TYPE.D                => "슬롯 D",
            _                                       => ""
        };
    }

    /// <summary>
    /// 액티브 스킬 ID 한국어 반환 함수
    /// </summary>
    /// <param name="type">액티브 스킬 ID 종류</param>
    public static string ToKoreanString(this ACTIVE_SKILL_ID type)
    {
        return type switch
        {
            ACTIVE_SKILL_ID.Magnum                  => "매그넘",
            ACTIVE_SKILL_ID.Rifle                   => "돌격소총",
            ACTIVE_SKILL_ID.Sniper                  => "저격총",
            _                                       => ""
        };
    }
}