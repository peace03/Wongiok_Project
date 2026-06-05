using UnityEngine;

public class AttackingState : BossState
{
    public AttackingState(BossController controller, IBossLogics logics)
        : base(controller, logics) { }

    bool chance = false;

    public override void Enter()
    {
        Debug.Log("Attack 상태 진입");
    }
    public override void Update()
    {
        if (!chance) { Debug.Log("Attack Update 실행"); chance = true; }
    }
    public override void Exit()
    {
        chance = false;
        Debug.Log("Attack 상태 이탈");
    }
}
