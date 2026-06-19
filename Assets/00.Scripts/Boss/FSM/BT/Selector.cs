using System.Collections.Generic;

public class Selector : Node
{
    private List<Node> children;

    public Selector(List<Node> children)
    {
        this.children = children;
    }

    public override NodeState Evaluate()
    {
        foreach(var node in children)
        {
            var BossStateId = node.Evaluate();
            if (BossStateId == NodeState.Success) return NodeState.Success;
            else if (BossStateId == NodeState.Running) return NodeState.Running;
        }
        return NodeState.Failure;
    }
}
