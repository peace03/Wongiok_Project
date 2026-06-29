using UnityEngine;

// 플레이어가 피격 직후 경직과 넉백을 처리하는 상태입니다.
public class PlayerHitState : PlayerBaseState
{
    #region 필드

    // 경직이 끝날 때까지 남은 시간입니다.
    private float stunTimer;

    // 경직 시간 동안 적용할 넉백 속도입니다.
    private Vector3 knockbackVelocity;

    #endregion

    #region 상태 권한

    public override bool CanAttack => false;
    public override bool CanDash => false;
    public override bool CanParry => false;
    public override bool CanUseHealItem => false;
    public override bool CanUpdateFacingDirection => false;
    public override bool CanTakeDamage => false;

    #endregion

    #region 생성자

    public PlayerHitState(PlayerController controller) : base(controller) { }

    #endregion

    #region 상태 생명주기

    public void SetHit(DamageInfo damageInfo)
    {
        stunTimer = PlayerStatus.HitStunDuration;
        knockbackVelocity = GetKnockbackDirection(damageInfo) *
            (PlayerStatus.HitKnockbackDistance / PlayerStatus.HitStunDuration);
    }

    public override void EnterState()
    {
        Debug.Log("Hit Enter");

        controller.Movement.ResetVerticalVelocity();
    }

    public override void UpdateState()
    {
        stunTimer -= Time.deltaTime;

        controller.Movement.ApplyGravity();
        controller.Movement.MoveByVelocity(knockbackVelocity);
        controller.Movement.MoveVerticalVelocity();

        if (stunTimer > 0f) return;

        if (CheckFallTransition()) return;
        if (CheckMoveTransition()) return;

        controller.TransitionTo(controller.PlayerIdleState);
    }

    public override void FixedUpdateState()
    {
    }

    public override void ExitState()
    {
        Debug.Log("Hit Exit");
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

    #region 내부 계산

    private Vector3 GetKnockbackDirection(DamageInfo damageInfo)
    {
        Vector3 direction = new Vector3(damageInfo.HitDirection.x, 0f, 0f);
        if (direction.sqrMagnitude > Mathf.Epsilon)
            return direction.normalized;

        if (damageInfo.AttackerObject != null)
        {
            Vector3 fromAttacker = controller.transform.position - damageInfo.AttackerObject.transform.position;
            fromAttacker.y = 0f;

            if (fromAttacker.sqrMagnitude > Mathf.Epsilon) return fromAttacker.normalized;
        }

        return controller.IsFacingRight ? Vector3.right : Vector3.left;
    }

    #endregion
}
