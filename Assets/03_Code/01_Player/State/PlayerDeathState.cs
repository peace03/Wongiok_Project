using UnityEngine;

// 플레이어가 사망한 뒤 부활하기 전까지 모든 조작과 피격을 막는 상태입니다.
public class PlayerDeathState : PlayerBaseState
{
    #region 필드

    // 마지막 사망 정보를 저장해 이후 연출 확장에서 참조할 수 있게 합니다.
    private DeathInfo deathInfo;

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

    public PlayerDeathState(PlayerController controller) : base(controller) { }

    #endregion

    #region 상태 생명주기

    public void SetDeath(DeathInfo info)
    {
        deathInfo = info;
    }

    public override void EnterState()
    {
        Debug.Log($"Death Enter: {deathInfo.Cause}");

        controller.Movement.ResetVerticalVelocity();
        controller.Animation.PlayDeath();
    }

    public override void UpdateState()
    {
    }

    public override void FixedUpdateState()
    {
    }

    public override void ExitState()
    {
        Debug.Log("Death Exit");
    }

    #endregion
}
