using UnityEngine;

public class TargetEffect : Effect, ITargetEffect
{
    [Header("따라다닐 대상")]
    [SerializeField] private Transform target;

    private void Update()
    {
        // 따라다닐 대상이 없다면
        if (target == null)
            // 종료
            return;

        // 현재 위치를 타겟 위치로 설정
        transform.position = target.position;
    }

    /// <summary>
    /// 정보 설정 함수
    /// </summary>
    /// <param name="target">따라다닐 대상(생략 가능, 기본값 : 비어있음)</param>
    public void SetInfo(Transform target = null) => this.target = target;
}