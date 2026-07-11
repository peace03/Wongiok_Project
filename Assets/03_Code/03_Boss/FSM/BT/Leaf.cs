using System;

public class Leaf : Node
{
    private Func<NodeState> action;
    public Leaf(Func<NodeState> action)
    {
        this.action = action;
    }
    public override NodeState Evaluate()
    {
        return action();
    }
}