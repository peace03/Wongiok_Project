using UnityEngine;

// 플레이어가 지상에서 가만히 있는 상태입니다.
public class PlayerIdleState : PlayerBaseState
{
    #region 생성자

    public PlayerIdleState(PlayerController controller) : base(controller) { }

    #endregion

    #region 상태 생명주기

    public override void EnterState()
    {
        controller.Animation.SetLocomotion(false);
    }

    public override void UpdateState()
    {
        // 정지 상태에서도 중력과 수직 속도는 계속 반영합니다.
        controller.Movement.ApplyGravity();
        controller.Movement.Move(Vector2.zero);

        if (CheckFallTransition()) return;
        if (CheckDashTransition()) return;
        if (CheckMoveTransition()) return;
        if (CheckJumpTransition()) return;
    }

    public override void FixedUpdateState()
    {
    }

    public override void ExitState()
    {
    }

    #endregion

    #region 상태 전환 체크

    public bool CheckFallTransition()
    {
        if (!controller.Movement.IsGrounded && controller.Movement.IsFalling)
        {
            controller.TransitionTo(controller.PlayerFallState);
            return true;
        }

        return false;
    }

    public bool CheckDashTransition()
    {
        if (controller.DashTriggered && controller.Movement.CanDash())
        {
            controller.TransitionTo(controller.PlayerDashState);
            return true;
        }

        return false;
    }

    public bool CheckMoveTransition()
    {
        if (controller.MoveInput != Vector2.zero)
        {
            controller.TransitionTo(controller.PlayerMoveState);
            return true;
        }

        return false;
    }

    public bool CheckJumpTransition()
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
