using UnityEngine;

public class AttackingState : BossState
{
    public AttackingState(BossController controller, IBossLogics logics)
        : base(controller, logics) { }
    
    private Node curBT;
    private int enrangedCount = 0; //寃⑸끂?곹깭 怨듦꺽 2?곗냽 媛??
    public override void Enter()
    {
        Debug.Log("Attack ?곹깭 吏꾩엯");
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
        if (logics.IsEnranged == true) logics.EnrangedTimer(); //寃⑸끂 ??대㉧
        //Debug.Log("Attack BossStateId Update ?ㅽ뻾");
        if (logics.GetStateDone())
        {
            if (logics.IsEnranged == true && enrangedCount < 2)
                controller.ChangeState(BossStateId.Attack);
            else
                controller.ChangeState(BossStateId.Idle);
        }
    }
    public override void Exit()
    {
        EventBus<AttackFinish>.Publish(default);
        if(!logics.IsEnranged) enrangedCount = 0;
        //Debug.Log("Attack ?곹깭 ?댄깉");
    }
}
