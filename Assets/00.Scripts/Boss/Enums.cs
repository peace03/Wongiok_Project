public enum BossStateId
{
    Spawn,
    Idle,
    Attack,
    Ultimate,
    Groggy,
    Defeated
}

public enum BossAttackType
{
    A,
    B,
    C
}

// 보스 애니메이터의 정수 파라미터와 대응되는 값입니다.
public enum BossAnimation
{
    Idle,
    AttackA,
    AttackB,
    AttackC,
    Ultimate1,
    Ultimate2,
    Ultimate3,
    Chase,
    Parry,
    Groggy
}

public enum BossFacing
{
    Left,
    Right
}
