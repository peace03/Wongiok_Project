using UnityEngine;

// 점프 입력으로 실제 점프 힘을 적용하는 즉시 상태입니다.
public class PlayerJumpState : PlayerBaseState
{
    #region 생성자

    public PlayerJumpState(PlayerController controller) : base(controller) { }

    #endregion

    #region 상태 생명주기

    public override void EnterState()
    {
        Debug.Log("Jump Enter");

        controller.Animation.PlayJump();
        controller.Movement.Jump();
        controller.Audio?.PlayJump();
        EventBus<CanExecutingActiveSkill>.Publish(new(controller.gameObject, false));
    }

    public override void UpdateState()
    {
        controller.Movement.ApplyGravity();
        controller.Movement.Move(controller.MoveInput);

        if (controller.DashTriggered && controller.Movement.CanDash())
        {
            controller.TransitionTo(controller.PlayerDashState);
            return;
        }

        if (controller.JumpTriggered && controller.Movement.CanJump())
        {
            controller.Movement.Jump();
            controller.Animation.PlayJump();
            controller.Audio?.PlayJump();
            return;
        }

        if (controller.Movement.IsFalling)
            controller.TransitionTo(controller.PlayerFallState);
    }

    public override void FixedUpdateState()
    {
    }

    public override void ExitState()
    {
        Debug.Log("Jump Exit");
    }

    #endregion
}
