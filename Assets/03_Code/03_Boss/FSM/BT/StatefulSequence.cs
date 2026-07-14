using UnityEngine;
using System.Collections.Generic;

public class StatefulSequence : Node
{
    private List<Node> children;
    private int curIndex = 0; //현재 실행중인 인덱스 기억

    public StatefulSequence(List<Node> children)
    {
        this.children = children;
    }

    public override NodeState Evaluate()
    {
        while(curIndex < children.Count)
        {
            NodeState state = children[curIndex].Evaluate();
            if (state == NodeState.Running) return NodeState.Running;
            if(state == NodeState.Failure)
            {
                curIndex = 0;
                return NodeState.Failure;
            }
            curIndex++;
        }
        curIndex = 0;
        return NodeState.Success;
    }

    //현재 노드인덱스 초기화
    public override void Reset()
    {
        curIndex = 0;
        foreach (var child in children)
        {
            child.Reset();
        }
    }
}
