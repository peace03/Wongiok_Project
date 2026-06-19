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
            var state = node.Evaluate();
            if (state == NodeState.Failure) return NodeState.Failure;
            else if (state == NodeState.Running) return NodeState.Running;
        }
        return NodeState.Success;
    }
}
