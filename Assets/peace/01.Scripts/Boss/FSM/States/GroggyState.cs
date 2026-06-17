using UnityEngine;

public class GroggyState : BossState
{
    public GroggyState(BossController controller, IBossLogics logics)
        : base(controller, logics) { }

    public override void Enter()
    {
        Debug.Log("그로기 상태 진입");
    }
    public override void Update()
    {
        
    }
}
