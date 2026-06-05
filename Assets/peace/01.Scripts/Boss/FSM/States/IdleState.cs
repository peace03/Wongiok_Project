using UnityEngine;

public class IdleState : BossState
{
    public IdleState(BossController controller, IBossLogics logics)
        : base(controller, logics) { }

    public override void Enter()
    {
        Debug.Log("Idle 상태 진입");
    }
    public override void FixedUpdate()
    {
        logics.ExcuteIdleMove();
    }
    public override void Update()
    {

    }
    public override void Exit()
    {
        Debug.Log("Idle 상태 이탈");
    }
}
