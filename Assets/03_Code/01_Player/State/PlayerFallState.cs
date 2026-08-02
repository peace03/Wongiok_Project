using UnityEngine;

// 플레이어가 공중에 있거나 낙하 중일 때의 상태입니다.
public class PlayerFallState : PlayerBaseState
{
    #region 생성자

    public PlayerFallState(PlayerController controller) : base(controller) { }

    #endregion

    #region 상태 생명주기

    public override void EnterState()
    {
        Debug.Log("Fall Enter");
        controller.Animation.PlayLanding();
    }

    public override void UpdateState()
    {
        controller.Movement.ApplyGravity();
        controller.Movement.Move(controller.MoveInput);

        if (CheckDashTransition()) return;
        if (CheckJumpTransition()) return;
        if (CheckGroundTransition()) return;
    }

    public override void FixedUpdateState()
    {
    }

    public override void ExitState()
    {
        Debug.Log("Fall Exit");
    }

    #endregion

    #region 상태 전환 체크

    private bool CheckDashTransition()
    {
        if (controller.DashTriggered && controller.Movement.CanDash())
        {
            controller.TransitionTo(controller.PlayerDashState);
            return true;
        }

        return false;
    }

    private bool CheckJumpTransition()
    {
        if (controller.JumpTriggered && controller.Movement.CanJump())
        {
            controller.TransitionTo(controller.PlayerJumpState);
            return true;
        }

        return false;
    }

    private bool CheckGroundTransition()
    {
        if (!controller.Movement.IsGrounded || !controller.Movement.IsFalling) return false;

        controller.Audio?.PlayLanding();

        if (controller.MoveInput != Vector2.zero)
        {
            controller.TransitionTo(controller.PlayerMoveState);
            return true;
        }

        controller.TransitionTo(controller.PlayerIdleState);
        return true;
    }

    #endregion
}
