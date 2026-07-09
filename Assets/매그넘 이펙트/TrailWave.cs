using UnityEngine;

public class TrailWave : MonoBehaviour
{
    [Header("나선의 너비")]
    [Tooltip("높은 값이면 나선(원통)이 넓어지고, 작은 값이면 나선(원통)이 좁아짐")]
    [SerializeField] private float waveWidth = 0.5f;        // 나선의 너비
    [Header("나선의 간격")]
    [Tooltip("높은 값이면 나선이 촘촘하게 감기며(///), 작은 값이면 나선이 느슨하게 감김(/  /  /)")]
    [SerializeField] private float waveInterval = 8f;       // 나선의 간격
    [Header("나선의 시작 각도")]
    [Tooltip("나선이 회전할 때, 몇 도 방향에서 출발할 지 정할 수 있음")]
    [SerializeField] private float offsetAngle = 0f;        // 나선의 시작 각도
    [Header("나선의 회전 방식 변경(기본값 : 삼각함수 - 사인)")]
    [Tooltip("나선의 회전 방식을 사인 함수에서 코사인 함수로 변경할 수 있음")]
    [SerializeField] private bool useCos = false;

    private Vector3 baseLocalPos;                           // 기본 위치
    private Vector3 lastTargetPos;                          // 마지막 타겟 위치
    private float moveDistance = 0f;                        // 타겟이 이동한 거리

    public void OnEnable()
    {
        if(transform.parent != null)
        {
            // 기본 위치 초기화
            baseLocalPos = transform.localPosition;
            lastTargetPos = transform.parent.position;
        }
    }

    void Update()
    {
        // 부모(본체)가 이동한 거리 누적
        moveDistance += Vector3.Distance(transform.parent.position, lastTargetPos);
        lastTargetPos = transform.parent.position;

        float wave = useCos
            ? Mathf.Cos(moveDistance * waveInterval + offsetAngle)
            : Mathf.Sin(moveDistance * waveInterval + offsetAngle);
        transform.localPosition = baseLocalPos + new Vector3(0f, wave * waveWidth, 0f);
    }
}