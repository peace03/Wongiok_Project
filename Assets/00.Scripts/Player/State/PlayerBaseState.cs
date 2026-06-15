public abstract class PlayerBaseState
{
    // 상태에서 플레이어 입력, 이동, 공격 컴포넌트에 접근하기 위한 컨트롤러 참조입니다.
    protected PlayerController controller;

    // 현재 상태에서 공격 입력을 처리할 수 있는지 나타냅니다.
    // 기본값은 true이며, DashState처럼 공격을 막아야 하는 상태에서 false로 재정의합니다.
    public virtual bool CanAttack => true;

    // 현재 상태에서 대쉬 가능 여부 및 바라보는 방향 갱신 허용 여부에 사용됩니다.
    // DashState는 대쉬 중 방향이 바뀌지 않도록 false로 재정의합니다.
    public virtual bool CanDash => true;

    // 대쉬처럼 방향을 고정해야 하는 상태에서 false로 재정의합니다.
    public virtual bool CanUpdateFacingDirection => true;

    // 패링 기능이 추가될 경우 상태별 패링 가능 여부를 제어하기 위한 속성입니다.
    public virtual bool CanParry => true;

    // 현재 상태에서 회복 아이템 사용을 시작할 수 있는지 제어합니다.
    public virtual bool CanUseHealItem => true;

    // 현재 상태에서 피해를 받을 수 있는지 나타냅니다.
    // DashState처럼 회피 무적이 필요한 상태에서 false로 재정의합니다.
    public virtual bool CanTakeDamage => true;

    public PlayerBaseState(PlayerController controller)
    {
        this.controller = controller;
    }

    // 상태에 처음 진입할 때 한 번 호출됩니다.
    public abstract void EnterState();

    // 매 프레임 호출되어 입력 처리, 이동, 상태 전환 조건을 검사합니다.
    public abstract void UpdateState();

    // 물리 업데이트가 필요할 때 사용할 수 있는 고정 업데이트용 메서드입니다.
    public abstract void FixedUpdateState();

    // 다른 상태로 전환되기 직전에 한 번 호출됩니다.
    public abstract void ExitState();
}
