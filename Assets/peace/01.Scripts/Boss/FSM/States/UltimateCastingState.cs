using UnityEngine;

public class UltimateCastingState : BossState
{
    public UltimateCastingState(BossController controller, IBossLogics logics)
        : base(controller, logics) { }

    public override void Enter()
    {
        Debug.Log("궁극기 상태 전환 완료");
    }
    public override void Update()
    {
        
    }
}