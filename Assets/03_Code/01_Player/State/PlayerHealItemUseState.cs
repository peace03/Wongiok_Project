using UnityEngine;

// 회복 아이템 사용 시간 동안 플레이어 조작을 제한하는 상태입니다.
public class PlayerHealItemUseState : PlayerBaseState
{
    #region 필드

    // 회복 아이템 사용이 완료될 때까지 남은 시간입니다.
    private float useTimer;

    #endregion

    #region 상태 권한

    public override bool CanAttack => false;
    public override bool CanDash => false;
    public override bool CanUpdateFacingDirection => false;
    public override bool CanParry => false;
    public override bool CanUseHealItem => false;
    public override bool CanTakeDamage => true;

    #endregion

    #region 생성자

    public PlayerHealItemUseState(PlayerController controller) : base(controller) { }

    #endregion

    #region 상태 생명주기

    public override void EnterState()
    {
        Debug.Log("Heal Item Use Enter");

        useTimer = controller.HealItemInventory.UseDuration;
        controller.Movement.ResetVerticalVelocity();
    }

    public override void UpdateState()
    {
        useTimer -= Time.deltaTime;
        if (useTimer > 0f) return;

        controller.HealItemInventory.TryCompleteUse();

        if (CheckFallTransition()) return;
        if (CheckMoveTransition()) return;

        controller.TransitionTo(controller.PlayerIdleState);
    }

    public override void FixedUpdateState()
    {
    }

    public override void ExitState()
    {
        Debug.Log("Heal Item Use Exit");
    }

    #endregion

    #region 상태 전환 체크

    private bool CheckFallTransition()
    {
        if (controller.Movement.IsGrounded) return false;

        controller.TransitionTo(controller.PlayerFallState);
        return true;
    }

    private bool CheckMoveTransition()
    {
        if (controller.MoveInput == Vector2.zero) return false;

        controller.TransitionTo(controller.PlayerMoveState);
        return true;
    }

    #endregion
}
