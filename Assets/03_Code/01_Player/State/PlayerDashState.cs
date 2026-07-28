using UnityEngine;

// 플레이어가 짧은 시간 동안 고속 이동하는 대쉬 상태입니다.
public class PlayerDashState : PlayerBaseState
{
    #region 필드

    // 대쉬가 남은 시간을 저장합니다.
    private float dashTimer;

    // 대쉬 시작 시점에 확정된 이동 방향입니다.
    private Vector3 dashDirection;

    #endregion

    #region 상태 권한

    public override bool CanAttack => false;
    public override bool CanDash => false;
    public override bool CanUpdateFacingDirection => false;
    public override bool CanParry => false;
    public override bool CanUseHealItem => false;
    public override bool CanTakeDamage => false;
    public override bool CanDashPiercing => true;

    #endregion

    #region 생성자

    public PlayerDashState(PlayerController controller) : base(controller) { }

    #endregion

    #region 상태 생명주기

    public override void EnterState()
    {
        Debug.Log("Dash Enter");

        dashTimer = controller.Movement.DashDuration;
        dashDirection = GetDashDirection();

        controller.Animation.PlaySliding();
        controller.Movement.SetDashPiercing(CanDashPiercing);
        controller.Movement.ConsumeDash();
        controller.Movement.ResetVerticalVelocity();
    }

    public override void UpdateState()
    {
        dashTimer -= Time.deltaTime;

        Vector3 dashVelocity = dashDirection * controller.Movement.DashSpeed;
        controller.Movement.MoveByVelocity(dashVelocity);

        if (dashTimer > 0f) return;

        if (CheckGroundTransition()) return;
        if (CheckAirTransition()) return;
    }

    public override void FixedUpdateState()
    {
    }

    public override void ExitState()
    {
        controller.Movement.SetDashPiercing(false);
        Debug.Log("Dash Exit");
    }

    #endregion

    #region 상태 전환 체크

    private bool CheckGroundTransition()
    {
        if (!controller.Movement.IsGrounded) return false;

        if (controller.MoveInput != Vector2.zero)
        {
            controller.TransitionTo(controller.PlayerMoveState);
            return true;
        }

        controller.TransitionTo(controller.PlayerIdleState);
        return true;
    }

    private bool CheckAirTransition()
    {
        if (controller.Movement.IsGrounded) return false;

        controller.TransitionTo(controller.PlayerFallState);
        return true;
    }

    #endregion

    #region 내부 계산

    private Vector3 GetDashDirection()
    {
        if (controller.MoveInput.x < 0f) return Vector3.left;
        if (controller.MoveInput.x > 0f) return Vector3.right;

        return controller.IsFacingRight ? Vector3.right : Vector3.left;
    }

    #endregion
}
