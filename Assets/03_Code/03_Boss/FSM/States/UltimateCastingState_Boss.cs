using UnityEngine;

public class UltimateCastingState_Boss : BossState
{
    public UltimateCastingState_Boss(BossController controller, IBossLogics logics)
        : base(controller, logics) { }

    private Node curBT;

    public override void Enter()
    {
        //Debug.Log("궁극기 상태 전환 완료");
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
            controller.ChangeState(State.Groggy);
        if (logics.GetStateDone())
            controller.ChangeState(State.Idle);
    }
    public override void Exit()
    {

    }
}