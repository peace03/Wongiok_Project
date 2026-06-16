using UnityEngine;

public class AttackingState : BossState
{
    public AttackingState(BossController controller, IBossLogics logics)
        : base(controller, logics) { }
    
    private Node curBT;

    public override void Enter()
    {
        Debug.Log("Attack 상태 진입");
        curBT = logics.GetAttackBT();
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
        //Debug.Log("Attack State Update 실행");
        if (logics.GetStateDone())
        {
            controller.ChangeState(State.Idle);
        }
    }
    public override void Exit()
    {
        EventBus<AttackFinish>.Publish(default);
        //Debug.Log("Attack 상태 이탈");
    }
}
