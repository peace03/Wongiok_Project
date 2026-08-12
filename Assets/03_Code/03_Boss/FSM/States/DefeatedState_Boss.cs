using UnityEngine;

public class DefeatedState_Boss : BossState
{
    public DefeatedState_Boss(BossController controller, IBossLogics logics)
        : base(controller, logics) { }

    bool chance = false;

    public override void Enter()
    {
        
    }
    public override void Update()
    {
        if (!chance) { chance = true; }
    }
    public override void Exit()
    {
        chance = false;
    }
}
