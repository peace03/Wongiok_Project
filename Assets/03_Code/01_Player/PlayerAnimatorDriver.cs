using UnityEngine;

public sealed class PlayerAnimatorDriver : MonoBehaviour
{
    private static readonly int IsMoving = Animator.StringToHash("IsMoving");
    private static readonly int IsFalling = Animator.StringToHash("IsFalling");
    private static readonly int IdleState = Animator.StringToHash("Base Layer.Idle");
    private static readonly int PistolRunState = Animator.StringToHash("Base Layer.Pistol Run");
    private static readonly int JumpState = Animator.StringToHash("Base Layer.Jump");
    private static readonly int LandingState = Animator.StringToHash("Base Layer.Landing");
    private static readonly int SlidingState = Animator.StringToHash("Base Layer.Sliding");

    private const float LocomotionBlendDuration = 0.05f;
    private const float LandingBlendDuration = 0.15f;

    [SerializeField] private Transform visualRoot;
    [SerializeField] private Animator animator;

    private Transform animatedModelRoot;
    private Vector3 animatedModelInitialLocalPosition;
    private Quaternion animatedModelInitialLocalRotation;
    private bool hasAnimatedModelAnchor;

    private void Awake()
    {
        Initialize();
    }

    private void LateUpdate()
    {
        if (!hasAnimatedModelAnchor) return;

        animatedModelRoot.localPosition = animatedModelInitialLocalPosition;
        animatedModelRoot.localRotation = animatedModelInitialLocalRotation;
    }

    public void Initialize()
    {
        ResolveReferences();
        CacheAnimatedModelAnchor();
    }

    public void SetFacing(bool isFacingRight)
    {
        ResolveReferences();
        if (visualRoot == null) return;

        visualRoot.localRotation = isFacingRight
            ? Quaternion.identity
            : Quaternion.Euler(0f, 180f, 0f);

        Vector3 localScale = visualRoot.localScale;
        localScale.x = Mathf.Abs(localScale.x);
        visualRoot.localScale = localScale;
    }

    public void SetLocomotion(bool isMoving)
    {
        if (!CanPlay()) return;

        animator.SetBool(IsMoving, isMoving);
        animator.SetBool(IsFalling, false);
        animator.CrossFadeInFixedTime(
            isMoving ? PistolRunState : IdleState,
            LocomotionBlendDuration,
            0);
    }

    public void PlayJump()
    {
        if (!CanPlay()) return;

        animator.SetBool(IsFalling, false);
        animator.CrossFadeInFixedTime(
            JumpState,
            LocomotionBlendDuration,
            0,
            0);
    }

    public void PlayLanding()
    {
        if (!CanPlay()) return;

        animator.SetBool(IsFalling, true);

        AnimatorStateInfo currentState = animator.GetCurrentAnimatorStateInfo(0);
        AnimatorStateInfo nextState = animator.GetNextAnimatorStateInfo(0);
        bool isJumpState = currentState.fullPathHash == JumpState
            || (animator.IsInTransition(0) && nextState.fullPathHash == JumpState);

        if (isJumpState) return;

        animator.CrossFadeInFixedTime(
            LandingState,
            LandingBlendDuration,
            0,
            0);
    }

    public void PlaySliding()
    {
        if (!CanPlay()) return;

        animator.SetBool(IsFalling, false);
        animator.CrossFadeInFixedTime(
            SlidingState,
            LocomotionBlendDuration,
            0,
            0);
    }

    private bool CanPlay()
    {
        ResolveReferences();
        if (animator == null || animator.runtimeAnimatorController == null) return false;

        if (!hasAnimatedModelAnchor)
            CacheAnimatedModelAnchor();

        return true;
    }

    private void ResolveReferences()
    {
        if (animator == null)
            animator = GetComponentInChildren<Animator>(true);

        if (visualRoot == null && animator != null && animator.transform != transform)
            visualRoot = animator.transform.parent;
    }

    private void CacheAnimatedModelAnchor()
    {
        if (animator == null || animator.transform == transform)
        {
            hasAnimatedModelAnchor = false;
            return;
        }

        animatedModelRoot = animator.transform;
        animatedModelInitialLocalPosition = animatedModelRoot.localPosition;
        animatedModelInitialLocalRotation = animatedModelRoot.localRotation;
        hasAnimatedModelAnchor = true;
    }
}
