// 보스 패링 가능 여부를 알리는 이벤트입니다.
public struct CanParryEvent
{
    public bool CanParry { get; private set; }

    public CanParryEvent(bool canParry)
    {
        CanParry = canParry;
    }
}

// 보스 공격 콜라이더를 켜고 끄기 위한 이벤트입니다.
public struct ColliderToggleEvent
{
    public BossAttackType Type { get; private set; }
    public bool IsEnabled { get; private set; }

    public ColliderToggleEvent(BossAttackType type, bool isEnabled)
    {
        Type = type;
        IsEnabled = isEnabled;
    }
}

// 보스가 바라보는 방향이 바뀌었을 때 콜라이더 위치를 갱신하기 위한 이벤트입니다.
public struct BossFacingChangeEvent
{
    public BossFacing Direction { get; private set; }

    public BossFacingChangeEvent(BossFacing direction)
    {
        Direction = direction;
    }
}

// 보스 공격이 끝났음을 알리는 이벤트입니다.
public struct AttackFinish
{
}

// 보스 궁극기 상태 진입을 알리는 이벤트입니다.
public struct UltimateInvoke
{
}
