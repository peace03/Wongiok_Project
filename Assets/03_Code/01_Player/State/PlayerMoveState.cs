using UnityEngine;

// 플레이어가 지상에서 좌우 이동 중인 상태입니다.
public class PlayerMoveState : PlayerBaseState
{
    #region 생성자

    public PlayerMoveState(PlayerController controller) : base(controller) { }

    #endregion

    #region 상태 생명주기

    public override void EnterState()
    {
        controller.SetLocomotionAnimation(true);
        Debug.Log("Move Enter");
    }

    public override void UpdateState()
    {
        // 이동 중에도 중력과 수직 속도는 계속 반영합니다.
        controller.Movement.ApplyGravity();
        controller.Movement.Move(controller.MoveInput);

        if (CheckFallTransition()) return;
        if (CheckDashTransition()) return;
        if (CheckIdleTransition()) return;
        if (CheckJumpTransition()) return;
    }

    public override void FixedUpdateState()
    {
    }

    public override void ExitState()
    {
        Debug.Log("Move Exit");
    }

    #endregion

    #region 상태 전환 체크

    private bool CheckFallTransition()
    {
        if (!controller.Movement.IsGrounded && controller.Movement.IsFalling)
        {
            controller.TransitionTo(controller.PlayerFallState);
            return true;
        }

        return false;
    }

    private bool CheckDashTransition()
    {
        if (controller.DashTriggered && controller.Movement.CanDash())
        {
            controller.TransitionTo(controller.PlayerDashState);
            return true;
        }

        return false;
    }

    private bool CheckIdleTransition()
    {
        if (controller.MoveInput == Vector2.zero)
        {
            controller.TransitionTo(controller.PlayerIdleState);
            return true;
        }

        return false;
    }

    private bool CheckJumpTransition()
    {
        if (controller.JumpTriggered)
        {
            controller.TransitionTo(controller.PlayerJumpState);
            return true;
        }

        return false;
    }

    #endregion
}
