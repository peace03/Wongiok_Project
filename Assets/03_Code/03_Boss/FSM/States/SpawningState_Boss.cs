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
        EventBus<UISetBossHudVisibleEvent>.Publish(new UISetBossHudVisibleEvent(true));
        Debug.Log("sadfdsfa보스체력");
    }
    public override void Update()
    {
        
    }
    public override void Exit()
    {

    }
}
