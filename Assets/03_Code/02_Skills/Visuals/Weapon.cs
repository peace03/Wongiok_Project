using UnityEngine;
using System.Collections.Generic;

public class Weapon : MonoBehaviour, IHaveFirePoint
{
    [Header("총구 위치들")]
    [SerializeField] private List<Transform> firePoints;                    // 총구 위치들

    private readonly int IdleState = Animator.StringToHash("Idle");         // 대기 노드 상태
    private readonly int StartState = Animator.StringToHash("Start");       // 시작 노드 상태
    private readonly int layerIndex = 0;                                    // 레이어 위치

    private Animator animator;                                              // 무기 애니메이터

    public List<Transform> FirePoints => firePoints;

    // 무기 애니메이터 받아오기
    private void Awake() => animator = transform.GetComponent<Animator>();

    /// <summary>
    /// 애니메이션 재생 함수
    /// </summary>
    public void PlayAnimation()
    {
        // 애니메이터가 없다면
        if (animator == null)
            return;

        // 애니메이션 재생
        animator.CrossFadeInFixedTime(StartState, 0f, layerIndex, 0f);
    }

    /// <summary>
    /// 애니메이션 취소 함수
    /// </summary>
    public void CancelAnimation()
    {
        // 애니메이터가 없다면
        if (animator == null)
            return;

        // 애니메이션 취소
        animator.CrossFadeInFixedTime(IdleState, 0f, layerIndex, 0f);
    }
}