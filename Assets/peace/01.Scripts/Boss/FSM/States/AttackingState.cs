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
    }
    public override void FixedUpdate()
    {
        logics.ExcuteAttackMove();
    }
    public override void Update()
    {
        curBT.Evaluate();
    }
    public override void Exit()
    {
        
        Debug.Log("Attack 상태 이탈");
    }
}
