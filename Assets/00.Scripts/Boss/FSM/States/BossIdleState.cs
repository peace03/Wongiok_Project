using UnityEngine;

public class BossIdleState : BossState
{
    public BossIdleState(BossController controller, IBossLogics logics)
        : base(controller, logics)
    {
    }

    public override void Enter()
    {
        Debug.Log("Boss Idle 상태 진입");
        logics.SetStateDone(false);
        logics.InitCurTime_Idle();
        logics.SetRandomPos();
    }

    public override void FixedUpdate()
    {
        logics.IdleMove();
        logics.ExcuteMove();
    }

    public override void Update()
    {
        if (logics.IsEnranged)
            logics.EnrangedTimer();

        if (logics.GetStateDone())
            controller.ChangeState(BossStateId.Attack);
    }

    public override void Exit()
    {
    }
}
