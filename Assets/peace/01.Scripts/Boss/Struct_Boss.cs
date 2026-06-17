//패링 가능, 불가능 이벤트
public struct ParryEvent 
{
    public bool CanParry { get; private set; }
    public ParryEvent(bool canParry)
    {
        CanParry = canParry;
    }
}

//공격 콜라이더 토글 이벤트
public struct ColliderEvent
{
    public AttackType type { get; private set; }
    public bool state { get; private set; }
    public ColliderEvent(AttackType type, bool state)
    {
        this.type = type;
        this.state = state;
    }
}

//보스의 시점 방향에 따라 콜라이더 위치 변경
public struct BossFacingChangeEvent
{
    public Facing dir { get; private set; }
    public BossFacingChangeEvent(Facing dir) { this.dir = dir; }
}

//공격 종료 이벤트 (공격 콜라이더가 플레이어 1회만 공격하도록 기억)
public struct AttackFinish { }

//궁극기 발동
public struct UltimateInvoke { }