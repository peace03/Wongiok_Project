using UnityEngine;

public class UltimateCastingState : BossState
{
    public UltimateCastingState(BossController controller, IBossLogics logics)
        : base(controller, logics) { }

    private Node curBT;

    public override void Enter()
    {
        //Debug.Log("沅곴레湲??곹깭 ?꾪솚 ?꾨즺");
        curBT = logics.GetUltimateBT();
        logics.SetStateDone(false);
        logics.LogicInit();
    }
    public override void FixedUpdate()
    {
        logics.ExcuteMove();
    }
    public override void Update()
    {
        curBT.Evaluate();
        if (logics.CanTransitionToGroggy())
            controller.ChangeState(BossStateId.Groggy);
        if (logics.GetStateDone())
            controller.ChangeState(BossStateId.Idle);
    }
    public override void Exit()
    {

    }
}