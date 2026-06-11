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
        logics.AttackInit();
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
            controller.ChangeState(BossController.State.Idle);
        }
    }
    public override void Exit()
    {
        
        //Debug.Log("Attack 상태 이탈");
    }
}
