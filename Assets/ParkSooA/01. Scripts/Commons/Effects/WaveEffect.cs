using UnityEngine;

public class WaveEffect : Effect, IWaveEffect
{
    [Header("트레일 렌더러")]
    [Tooltip("오브젝트가 움직인 경로대로 그려주는 렌더러")]
    [SerializeField] private TrailRenderer trail;                       // 트레일 렌더러
    [Header("나선의 너비")]
    [Tooltip("높은 값이면 나선(원통)이 넓어지고, 작은 값이면 나선(원통)이 좁아짐")]
    [SerializeField] private float waveWidth = 1f;                      // 나선의 너비
    [Header("나선의 간격")]
    [Tooltip("높은 값이면 나선이 촘촘하게 감기며(///), 작은 값이면 나선이 느슨하게 감김(/  /  /)")]
    [SerializeField] private float waveInterval = 4f;                   // 나선의 간격
    [Header("나선의 시작 각도")]
    [Tooltip("나선이 회전할 때, 몇 도 방향에서 출발할 지 정할 수 있음")]
    [SerializeField] private float offsetAngle = 0f;                    // 나선의 시작 각도
    [Header("나선의 회전 방식 변경(기본값 : 삼각함수 - 사인)")]
    [Tooltip("나선의 회전 방식을 사인 함수에서 코사인 함수로 변경할 수 있음")]
    [SerializeField] private bool useCos = false;                       // 나선의 회전 방식

    private Vector3 baseLocalPos;                                       // 나선의 처음 위치
    private Vector3 lastTargetPos;                                      // 타겟(따라다닐 대상)의 마지막 위치

    private float moveDistance = 0f;                                    // 타겟의 총 이동거리
    private float curwaveHeight;                                        // 나선의 현재 높이

    private void OnEnable()
    {
        // 트레일 렌더러 컴포넌트 비활성화
        trail.enabled = false;
        // 나선의 처음 위치 초기화
        baseLocalPos = Vector3.zero;
        // 타겟의 총 이동거리 초기화
        moveDistance = 0f;
    }

    private void Update()
    {
        // 트레일 렌더러가 없거나, 따라다닐 대상이 없거나, 컨테이너에 있다면
        if (trail == null || transform.parent == null || transform.parent == Container)
            return;
        
        // 타겟(따라다닐 대상)이 이동한 거리(현재 위치 - 마지막 위치)를 받아와서 더하기(총 이동거리)
        moveDistance += Vector3.Distance(transform.parent.position, lastTargetPos);
        // 타겟의 현재 위치를 저장
        lastTargetPos = transform.parent.position;
        // 나선의 현재 높이 받아오기
        // 회전 방식 : 사인 OR 코사인 함수(타겟의 총 이동거리 * 나선의 간격 + 나선의 시작 각도)
        // 타겟의 총 이동거리 : 나선의 현재 높이를 결정함(최대/최소 높이가 정해져 있음)
        curwaveHeight = !useCos ? Mathf.Sin(moveDistance * waveInterval + offsetAngle)
                                : Mathf.Cos(moveDistance * waveInterval + offsetAngle);
        // 현재 위치 변경(나선의 처음 위치 + 나선의 현재 높이 * 나선의 너비)
        transform.localPosition = baseLocalPos + new Vector3(0f, curwaveHeight * waveWidth, 0f);
    }

    /// <summary>
    /// 정보 설정 함수
    /// </summary>
    public void SetInfo()
    {
        // 타겟(따라다닐 대상)의 마지막 위치 받아오기
        lastTargetPos = transform.parent.position;
        // 트레일 렌더러 컴포넌트 활성화
        trail.enabled = true;
        // 트레일 렌더러 초기화
        trail.Clear();
        //// 나선의 너비 설정
        //waveWidth = width;
        //// 나선의 간격 설정
        //waveInterval = interval;
        //// 나선의 시작 각도 설정
        //offsetAngle = offset;
        //// 나선의 회전 방식 설정
        //this.useCos = useCos;
    }

    /*
    /// <summary>
    /// 트레일 렌더러 색깔 설정 함수
    /// </summary>
    /// <param name="newColor">바꿀 색깔</param>
    private void SetTrailColor(Color newColor)
    {
        // 기존 색상의 알파 값들 받아오기
        var alphas = trail.colorGradient.alphaKeys;
        // 기존 색상의 색깔 값들 받아오기
        var colors = trail.colorGradient.colorKeys;

        // 색깔 값들을 받아왔다면
        if(colors.Length > 0)
            // 첫번째 색깔 변경
            colors[0].color = newColor;

        // 바꾼 색깔을 담을 그라디언트 생성
        Gradient newGradient = new();
        // 그라디언트에 바꾼 색깔 저장하기
        newGradient.SetKeys(colors, alphas);
        // 트레일 렌더러에 바꾼 색깔 적용하기
        trail.colorGradient = newGradient;
    }
    */
}