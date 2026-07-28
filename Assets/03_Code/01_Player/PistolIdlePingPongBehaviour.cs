using UnityEngine;

public sealed class PistolIdlePingPongBehaviour : StateMachineBehaviour
{
    private const float EndNormalizedTime = 0.999f;
    private const float StartNormalizedTime = 0.001f;
    private static readonly int PistolIdleSpeed = Animator.StringToHash("PistolIdleSpeed");

    private bool isReversing;

    public override void OnStateEnter(
        Animator animator,
        AnimatorStateInfo stateInfo,
        int layerIndex)
    {
        isReversing = animator.GetFloat(PistolIdleSpeed) < 0f;
        animator.SetFloat(PistolIdleSpeed, isReversing ? -1f : 1f);
    }

    public override void OnStateUpdate(
        Animator animator,
        AnimatorStateInfo stateInfo,
        int layerIndex)
    {
        if (!isReversing && stateInfo.normalizedTime >= EndNormalizedTime)
        {
            isReversing = true;
            animator.SetFloat(PistolIdleSpeed, -1f);
            return;
        }

        if (isReversing && stateInfo.normalizedTime <= StartNormalizedTime)
        {
            isReversing = false;
            animator.SetFloat(PistolIdleSpeed, 1f);
        }
    }

    public override void OnStateExit(
        Animator animator,
        AnimatorStateInfo stateInfo,
        int layerIndex)
    {
        isReversing = false;
        animator.SetFloat(PistolIdleSpeed, 1f);
    }
}
