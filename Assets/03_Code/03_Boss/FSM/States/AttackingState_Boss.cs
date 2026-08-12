using UnityEngine;

public class AttackingState_Boss : BossState
{
    public AttackingState_Boss(BossController controller, IBossLogics logics)
        : base(controller, logics) { }
    
    private Node curBT;
    private int enrangedCount = 0; //격노상태 공격 2연속 가능

    public override void Enter()
    {
        curBT = logics.GetAttackBT();
        logics.SetStateDone(false);
        logics.LogicInit();
        if(logics.IsEnranged) enrangedCount++;
    }
    public override void FixedUpdate()
    {
        logics.ExcuteMove();
    }
    public override void Update()
    {
        curBT.Evaluate();
        if (logics.IsEnranged == true) logics.EnrangedTimer(); //격노 타이머
        if (logics.GetStateDone())
        {
            if (logics.IsEnranged == true && enrangedCount < 2)
                controller.ChangeState(State.Attack);
            else
                controller.ChangeState(State.Idle);
        }
    }
    public override void Exit()
    {
        EventBus<AttackFinishEvent>.Publish(default);
        if(!logics.IsEnranged) enrangedCount = 0;
    }
}
