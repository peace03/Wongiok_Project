using UnityEngine;

public class GroggyState_Boss : BossState
{
    private readonly BossStatus bossStatus;

    public GroggyState_Boss(BossController controller, IBossLogics logics, BossStatus bossStatus)
        : base(controller, logics) { this.bossStatus = bossStatus; }

    public override void Enter()
    {
        //Debug.Log("그로기 상태 진입");
        logics.SetStateDone(false);
        logics.LogicInit();
        bossStatus.SetGroggyDamageMultiplierActive(true); //그로기 피격 배율 증가
    }
    public override void Update()
    {
        if (logics.PlayAnimGroggy_Time((int)Animation.Groggy) == NodeState.Success)
            controller.ChangeState(State.Attack);
    }
    public override void Exit()
    {
        bossStatus.SetGroggyDamageMultiplierActive(false); //그로기 피격 배율 증가
    }
}
