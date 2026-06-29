public abstract class PlayerBaseState
{
    #region 필드

    // 상태에서 플레이어 입력, 이동, 공격 컴포넌트에 접근하기 위한 컨트롤러 참조입니다.
    protected PlayerController controller;

    #endregion

    #region 상태 권한

    // 현재 상태에서 공격 입력을 처리할 수 있는지 판단합니다.
    public virtual bool CanAttack => true;

    // 현재 상태에서 대쉬를 사용할 수 있는지 판단합니다.
    public virtual bool CanDash => true;

    // 현재 상태에서 바라보는 방향을 갱신할 수 있는지 판단합니다.
    public virtual bool CanUpdateFacingDirection => true;

    // 현재 상태에서 패링을 사용할 수 있는지 판단합니다.
    public virtual bool CanParry => true;

    // 현재 상태에서 회복 아이템 사용을 시작할 수 있는지 판단합니다.
    public virtual bool CanUseHealItem => true;

    // 현재 상태에서 데미지를 받을 수 있는지 판단합니다.
    public virtual bool CanTakeDamage => true;

    #endregion

    #region 생성자

    public PlayerBaseState(PlayerController controller)
    {
        this.controller = controller;
    }

    #endregion

    #region 상태 생명주기

    public abstract void EnterState();

    public abstract void UpdateState();

    public abstract void FixedUpdateState();

    public abstract void ExitState();

    #endregion
}
