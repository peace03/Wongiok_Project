using UnityEngine;

public class DefeatedState : BossState
{
    public DefeatedState(BossController controller, IBossLogics logics)
        : base(controller, logics) { }

    bool chance = false;

    public override void Enter()
    {
        Debug.Log("Defeated 상태 진입");
    }
    public override void Update()
    {
        if (!chance) { Debug.Log("Defeated Update 실행"); chance = true; }
    }
    public override void Exit()
    {
        chance = false;
        Debug.Log("Defeated 상태 이탈");
    }
}
