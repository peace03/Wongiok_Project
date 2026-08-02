using UnityEngine;
using System.Collections.Generic;

public class Weapon : MonoBehaviour, IHaveFirePoint
{
    [Header("총구 위치들")]
    [SerializeField] private List<Transform> firePoints;

    private readonly int IdleState = Animator.StringToHash("Idle");
    private readonly int StartState = Animator.StringToHash("Start");
    private readonly int layerIndex = 0;

    private Animator animator;

    public List<Transform> FirePoints => firePoints;

    private void Awake()
    {
        animator = transform.GetComponent<Animator>();

        if (animator == null)
            Debug.Log($"[Skill] 애니메이터 없음 => 입력 - {gameObject.name}");
    }

    public void PlayAnimation()
    {
        if (animator == null)
            return;

        animator.CrossFadeInFixedTime(StartState, 0f, layerIndex, 0f);
    }

    public void CancelAnimation()
    {
        if (animator == null)
            return;

        animator.CrossFadeInFixedTime(IdleState, 0f, layerIndex, 0f);
    }
}