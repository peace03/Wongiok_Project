using UnityEngine;
using System.Collections.Generic;

public class StatefulSequence : Node
{
    private List<Node> children;
    private int curIndex = 0; //?꾩옱 ?ㅽ뻾以묒씤 ?몃뜳??湲곗뼲

    public StatefulSequence(List<Node> children)
    {
        this.children = children;
    }

    public override NodeState Evaluate()
    {
        while(curIndex < children.Count)
        {
            NodeState BossStateId = children[curIndex].Evaluate();
            if (BossStateId == NodeState.Running) return NodeState.Running;
            if(BossStateId == NodeState.Failure)
            {
                curIndex = 0;
                return NodeState.Failure;
            }
            curIndex++;
        }
        curIndex = 0;
        return NodeState.Success;
    }
}
