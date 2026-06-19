using System;

public class ConditionLeaf : Node
{
    private Func<bool> BossStateId;
    public ConditionLeaf(Func<bool> BossStateId)
    {
        this.BossStateId = BossStateId;
    }
    public override NodeState Evaluate()
    {
        return BossStateId() ? NodeState.Success : NodeState.Failure;
    }
}
