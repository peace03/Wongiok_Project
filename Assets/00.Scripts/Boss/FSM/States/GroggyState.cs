using UnityEngine;

public class GroggyState : BossState
{
    public GroggyState(BossController controller, IBossLogics logics)
        : base(controller, logics) { }

    public override void Enter()
    {
        //Debug.Log("洹몃줈湲??곹깭 吏꾩엯");
        logics.SetStateDone(false);
        logics.LogicInit();
    }
    public override void Update()
    {
        if (logics.PlayAnimGroggy_Time((int)BossAnimation.Groggy) == NodeState.Success)
            controller.ChangeState(BossStateId.Attack);
    }
}
