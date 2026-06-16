using UnityEngine;

public class UltimateCastingState : BossState
{
    public UltimateCastingState(BossController controller, IBossLogics logics)
        : base(controller, logics) { }

    private Node BT;

    public override void Enter()
    {
        Debug.Log("궁극기 상태 전환 완료");
        logics.LogicInit();
    }
    public override void Update()
    {
        BT.Evaluate();
        if (logics.CanTransitionToGroggy())
            controller.ChangeState(State.Groggy);
    }
}