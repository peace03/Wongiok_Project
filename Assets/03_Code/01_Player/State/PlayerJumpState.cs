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

        controller.Movement.Jump();
        controller.TransitionTo(controller.PlayerFallState);
    }

    public override void UpdateState()
    {
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
