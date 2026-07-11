using UnityEngine;

public class SpawningState_Boss : BossState
{
    public SpawningState_Boss(BossController controller, IBossLogics logics)
        : base(controller, logics) { }

    public override void Enter()
    {
        //Debug.Log("SpawningState 진입");
        logics.Spawn();
        //Debug.Log("SpawningState 애니메이션, 효과음 재생");
        controller.ChangeState(State.Idle);
    }
    public override void Update()
    {
        
    }
    public override void Exit()
    {

    }
}
