using UnityEngine;

public class DefeatedState : BossState
{
    public DefeatedState(BossController controller, IBossLogics logics)
        : base(controller, logics) { }

    bool chance = false;

    public override void Enter()
    {
        Debug.Log("Defeated ?곹깭 吏꾩엯");
    }
    public override void Update()
    {
        if (!chance) { Debug.Log("Defeated Update ?ㅽ뻾"); chance = true; }
    }
    public override void Exit()
    {
        chance = false;
        Debug.Log("Defeated ?곹깭 ?댄깉");
    }
}
