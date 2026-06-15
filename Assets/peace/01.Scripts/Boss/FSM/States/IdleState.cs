using UnityEngine;

public class IdleState : BossState
{
    public IdleState(BossController controller, IBossLogics logics)
        : base(controller, logics) { }

    public override void Enter()
    {
        Debug.Log("Idle 상태 진입");
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
        if (logics.GetStateDone() == true)
            controller.ChangeState(State.Attack);
    }
    public override void Exit()
    {
        //Debug.Log("Idle 상태 이탈");
    }
}
