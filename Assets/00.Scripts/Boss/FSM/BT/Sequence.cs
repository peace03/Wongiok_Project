using System.Collections.Generic;

public class Sequence : Node
{
    private List<Node> children;

    public Sequence(List<Node> children)
    {
        this.children = children;
    }

    public override NodeState Evaluate()
    {
        foreach(var node in children)
        {
            var BossStateId = node.Evaluate();
            if (BossStateId == NodeState.Failure) return NodeState.Failure;
            else if (BossStateId == NodeState.Running) return NodeState.Running;
        }
        return NodeState.Success;
    }
}
