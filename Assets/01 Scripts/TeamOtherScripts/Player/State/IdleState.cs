using UnityEngine;

// 플레이어가 지상에서 가만히 있는 상태입니다.
// 이동, 점프, 대쉬 입력을 감지해서 다른 상태로 전환합니다.
public class IdleState : PlayerBaseState
{
    public IdleState(PlayerController controller) : base(controller) { }

    public override void EnterState()
    {
        Debug.Log("Idle Enter");
    }

    public override void UpdateState()
    {
        // 정지 상태에서도 중력은 계속 적용해야 바닥 판정과 점프 카운트가 정상 갱신됩니다.
        controller.Movement.ApplyGravity();

        // 수평 입력 없이 수직 속도만 반영합니다.
        controller.Movement.Move(Vector2.zero);

        if (!controller.Movement.IsGrounded && controller.Movement.IsFalling)
        {
            controller.TransitionTo(controller.FallState);
            return;
        }

        // 대쉬 입력이 들어오고 쿨타임/공중 대쉬 조건을 만족하면 대쉬 상태로 전환합니다.
        if (controller.DashTriggered && controller.Movement.CanDash())
        {
            controller.TransitionTo(controller.DashState);
            return;
        }

        // 이동 입력이 들어오면 이동 상태로 전환합니다.
        if (controller.MoveInput != Vector2.zero)
        {
            controller.TransitionTo(controller.MoveState);
            return;
        }

        // 점프 입력이 들어오면 점프 상태로 전환합니다.
        if (controller.JumpTriggered)
        {
            controller.TransitionTo(controller.JumpState);
            return;
        }
    }

    public override void ExitState()
    {
        Debug.Log("Idle Exit");
    }

    public override void FixedUpdateState()
    {
    }
}
