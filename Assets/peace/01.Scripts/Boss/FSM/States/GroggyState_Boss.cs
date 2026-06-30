using UnityEngine;

public class GroggyState_Boss : BossState
{
    public GroggyState_Boss(BossController controller, IBossLogics logics)
        : base(controller, logics) { }

    public override void Enter()
    {
        //Debug.Log("그로기 상태 진입");
        logics.SetStateDone(false);
        logics.LogicInit();
    }
    public override void Update()
    {
        if (logics.PlayAnimGroggy_Time((int)Animation.Groggy) == NodeState.Success)
            controller.ChangeState(State.Attack);
    }
    public override void Exit()
    {

    }
}
