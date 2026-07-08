using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class LaserSight : MonoBehaviour
{
    [Header("위치 참조")]
    [Tooltip("레이저 발사될 총구 위치")][SerializeField] private Transform muzzlePos;
    [Tooltip("레이저 조준할 타겟 위치")][SerializeField] private Transform targetPos;

    [Header("레이저 Setting")]
    [Tooltip("레이저가 뚫지 못하고 막힐 레이어 (지형, 플레이어 등)")]
    [SerializeField] private LayerMask obstacleLayer;
    [Tooltip("레이저 최대 사거리")][SerializeField] private float maxLaserDistance = 50f;

    private LineRenderer lr;
    private bool isAiming = false; //조준 렌더링 스위치

    private void Awake()
    {
        lr = GetComponent<LineRenderer>();
        lr.positionCount = 2; //레이저는 시작점, 끝점 2개만 필요
        lr.enabled = false; //평상시 렌더러 끄기
    }

    //보스 패턴에서 사전신호 시작할 때 호출
    public void StartAiming()
    {
        isAiming = true;
        lr.enabled = true;
    }

    //사격 직전 레이저 끄기
    public void StopAiming()
    {
        isAiming = false;
        lr.enabled = false;
    }

    private void Update()
    {
        if (!isAiming) return; //조준 중이 아니면 연산 생략

        //시작점 고정 (0번 인덱스, 현재 총구 위치)
        lr.SetPosition(0, muzzlePos.position);
        Vector3 dirToTarget = (targetPos.position - muzzlePos.position).normalized; //방향벡터

        //물리적 레이캐스트(벽뚫림 방지)
        //총구 위치에서 타겟 방향으로 레이저를 쏴서 obstacleLayer에 닿는 무언가가 있는지 검사
        if (Physics.Raycast(muzzlePos.position, dirToTarget, out RaycastHit hit, maxLaserDistance, obstacleLayer))
            lr.SetPosition(1, hit.point); //타겟에 맞았다면, 끝점을 충동좌표로 설정
        //충돌한 것이 없다면 최대사거리까지 렌더링
        else lr.SetPosition(1, muzzlePos.position + dirToTarget * maxLaserDistance);
    }
}
