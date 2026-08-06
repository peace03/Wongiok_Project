using UnityEngine;

// 지면에 닿은 직후 Landing 애니메이션이 이동 애니메이션에 덮이지 않도록 유지하는 연출 상태입니다.
public class PlayerLandingState : PlayerBaseState
{
    private bool animationStarted;

    public override bool CanAttack => false;
    public override bool CanDash => false;
    public override bool CanParry => false;
    public override bool CanUseHealItem => false;

    public PlayerLandingState(PlayerController controller) : base(controller) { }

    public override void EnterState()
    {
        Debug.Log("Landing Enter");

        controller.Audio?.PlayLanding();
        animationStarted = controller.Animation.PlayLanding();
    }

    public override void UpdateState()
    {
        // 착지 연출 중에도 CharacterController의 접지와 수평 이동 계산은 계속 유지합니다.
        controller.Movement.ApplyGravity();
        controller.Movement.Move(controller.MoveInput);

        // Landing 도중 지면을 벗어나면 남은 연출을 기다리지 않고 다시 낙하 상태로 돌아갑니다.
        if (!controller.Movement.IsGrounded && controller.Movement.IsFalling)
        {
            controller.TransitionTo(controller.PlayerFallState);
            return;
        }

        // 착지 직후 다시 점프하면 입력 반응을 우선합니다.
        if (controller.JumpTriggered && controller.Movement.CanJump())
        {
            controller.TransitionTo(controller.PlayerJumpState);
            return;
        }

        if (animationStarted && !controller.Animation.IsLandingAnimationFinished())
            return;

        if (controller.MoveInput != Vector2.zero)
        {
            controller.TransitionTo(controller.PlayerMoveState);
            return;
        }

        controller.TransitionTo(controller.PlayerIdleState);
    }

    public override void FixedUpdateState()
    {
    }

    public override void ExitState()
    {
        Debug.Log("Landing Exit");
    }
}
