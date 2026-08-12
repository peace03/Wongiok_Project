using UnityEngine;

public class IdleState_Boss : BossState
{
    public IdleState_Boss(BossController controller, IBossLogics logics)
        : base(controller, logics) { }

    public override void Enter()
    {
        logics.SetStateDone(false);
        logics.InitCurTime_Idle();
        logics.SetRandomPos();      //Idle 이동좌표 지정
    }
    public override void FixedUpdate()
    {
        logics.IdleMove();
        logics.ExcuteMove();
    }
    public override void Update()
    {
        if(logics.IsEnranged == true) logics.EnrangedTimer(); //격노 타이머
        if (logics.GetStateDone() == true)
            controller.ChangeState(State.Attack);
    }
    public override void Exit()
    {
        
    }
}
