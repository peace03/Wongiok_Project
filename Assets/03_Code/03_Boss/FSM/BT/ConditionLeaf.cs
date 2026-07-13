using System;

public class ConditionLeaf : Node
{
    private Func<bool> state;
    public ConditionLeaf(Func<bool> state)
    {
        this.state = state;
    }
    public override NodeState Evaluate()
    {
        return state() ? NodeState.Success : NodeState.Failure;
    }
}
