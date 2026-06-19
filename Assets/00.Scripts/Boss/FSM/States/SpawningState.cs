using UnityEngine;

public class SpawningState : BossState
{
    public SpawningState(BossController controller, IBossLogics logics)
        : base(controller, logics) { }

    public override void Enter()
    {
        //Debug.Log("SpawningState 吏꾩엯");
        logics.Spawn();
        //Debug.Log("SpawningState ?좊땲硫붿씠?? ?④낵???ъ깮");
        controller.ChangeState(BossStateId.Idle);
    }
    public override void Update()
    {
        
    }
    public override void Exit()
    {

    }
}
