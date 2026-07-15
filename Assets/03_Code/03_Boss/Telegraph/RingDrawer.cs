using UnityEngine;
using System.Collections;

/// <summary>
/// [시스템 아키텍처: 데이터 주도적 수동형 뷰 (Data-Driven Passive View)]
/// 기존의 시간 기반 수축 코루틴 대신, Animator의 ParryTelegraphProgress 커브를 읽어 링을 그립니다.
/// 패링 창의 기계적 시작과 종료는 Animation Event가 결정하며, 이 컴포넌트는 그 시각적 진행도와 정점 연출을 담당합니다.
/// </summary>
[RequireComponent(typeof(LineRenderer))]
public class RingDrawer : MonoBehaviour
{
    [SerializeField] private RingDrawerSimple ringDrawerSimple; // 고정된 바깥쪽 테두리를 그리는 서브 컴포넌트

    #region [1. 인스펙터 설정 (시각 효과 및 수축 데이터)]
    [Header("중심점 연출")]
    [SerializeField] private LineRenderer centerDotLine; // 중앙에 위치한 점(Dot) 렌더러
    [SerializeField] private int blinkCount = 3;         // 정점 도달 시 깜빡임 횟수
    [SerializeField] private Color blinkColor = new Color(4f, 4f, 4f); // 깜빡일 때의 HDR 발광 색상
    [SerializeField] private Color normalColor = new Color(1.8f, 1.3f, 0.4f); // 평상시 대기 색상

    [Header("해상도 및 굵기")]
    [SerializeField] private int segments = 48;          // 원을 구성하는 꼭짓점(Vertex)의 개수 (높을수록 원이 부드러워짐)
    [SerializeField] private float width = 0.08f;        // 선의 두께

    [Header("패링 수축 설정")]
    [SerializeField] private float startRadius = 3f;     // 링이 처음 생성될 때의 최대 반지름
    [SerializeField] private float contactRadius = 0.5f; // 링이 수축을 완료하여 패링이 성립되는 목표 반지름
    [SerializeField] private Color goldColor = new Color(1.8f, 1.3f, 0.4f); // 링의 기본 베이스 색상

    [Header("닿는 순간 연출")]
    [SerializeField] private Transform centerDot;        // 중앙 점의 트랜스폼 (스케일 조절용 등)
    [SerializeField] private SpriteRenderer centerDotSprite;
    [SerializeField] private float dotFlashScale = 1.8f;
    [SerializeField] private Color flashColor = new Color(3f, 2.5f, 1.5f);

    [Tooltip("패링 정점 도달 시 일시적인 엔진 시간 왜곡 여부")]
    [SerializeField] private bool useSlowMo = true;      // 불릿타임(Slow Motion) 사용 스위치
    [SerializeField] private float slowMoScale = 0.2f;   // 느려지는 배율 (1이 정상, 0.2면 20% 속도)
    [SerializeField] private float slowMoDuration = 0.15f; // 느려짐이 유지되는 현실 시간(Realtime)
    #endregion

    #region [2. 상태 및 캐싱 변수 (메모리 최적화)]
    // 매 프레임 문자열 대신 Animator 파라미터 해시를 사용해 진행도 값을 읽는다.
    private const string ParryTelegraphProgressName = "ParryTelegraphProgress";
    private static readonly int ParryTelegraphProgressId = Animator.StringToHash(ParryTelegraphProgressName);

    private LineRenderer lr;              // 실제 선을 그리는 유니티 컴포넌트
    private float radius;                 // 이번 프레임에 그려질 링의 현재 반지름
    private Animator sourceAnimator;      // 데이터를 주입(Push)해줄 출처 애니메이터 (보스)

    // 사전신호 한 회차 동안만 유지되는 상태다.
    private float previousProgress;       // 전환 블렌딩으로 인한 시각적 역행을 막기 위한 이전 진행도
    private bool isPlaying;               // 현재 Animator 진행도를 읽어 링을 렌더링할지 여부
    private bool hasReachedApex;          // 중복된 CanParryEvent(true)에서 정점 연출을 다시 실행하지 않기 위한 가드
    private bool hasWarnedInvalidAnimator;// 에러 로그 도배 방지용 플래그
    #endregion

    #region [3. 생명주기 및 이벤트 구독]
    private void OnEnable()
    {
        // 보스 Animation Event가 발행한 패링 창 열림/닫힘 신호를 받는다.
        EventBus<CanParryEvent>.action += HandleCanParry;
    }

    private void OnDisable()
    {
        EventBus<CanParryEvent>.action -= HandleCanParry;
        StopAllCoroutines();
        isPlaying = false;
        sourceAnimator = null;
    }

    public void Init()
    {
        lr = GetComponent<LineRenderer>();
        lr.loop = true;               // 마지막 꼭짓점과 첫 꼭짓점을 연결하여 완벽한 원을 만듦
        lr.useWorldSpace = false;     // 보스가 이동해도 링이 보스를 따라가도록 로컬 스페이스 사용
        lr.widthMultiplier = width;
        lr.positionCount = segments;
        radius = startRadius;
        ringDrawerSimple.Init();
        gameObject.SetActive(false);  // 평상시에는 렌더링 연산을 막기 위해 꺼둠
    }
    #endregion

    #region [4. 핵심 API (신호 제어)]
    /// <summary>
    /// [API: 사전 신호 시작]
    /// BT가 공격 애니메이션 진입 직전에 호출한다.
    /// Animator 참조를 저장할 뿐, 실제 수축 진행도는 이후 LateUpdate에서 현재 재생 중인 Clip의 커브로 읽는다.
    /// </summary>
    public void BeginSignal(Animator sourceAnimator)
    {
        StopAllCoroutines();
        this.sourceAnimator = sourceAnimator;
        previousProgress = 0f;
        hasReachedApex = false;
        radius = startRadius;

        // Animator 또는 해당 Float 파라미터가 없으면 신호를 시작하지 않는다.
        // 이 검사는 파라미터 존재만 확인하며, 현재 Clip에 커브가 있는지는 Animation 자산 검증으로 보장해야 한다.
        if (sourceAnimator == null || !HasProgressParameter(sourceAnimator))
        {
            WarnInvalidAnimator(sourceAnimator);
            isPlaying = false;
            return;
        }

        isPlaying = true;
        ringDrawerSimple.DrawCircle(); // 바깥쪽 고정 링 표시
        DrawCircle();                  // 수축할 안쪽 링 초기화 표시
    }

    /// <summary>
    /// [API: 사전 신호 강제 종료]
    /// 패링 성공, 공격 취소, 상태 전환 또는 보스 비활성화 시 사전신호와 정점 연출을 즉시 정리합니다.
    /// </summary>
    public void StopSignal()
    {
        StopAllCoroutines();
        isPlaying = false;
        sourceAnimator = null;
        previousProgress = 0f;
        hasReachedApex = false;
        radius = startRadius;

        if (lr != null) DrawCircle();
        if (gameObject.activeSelf) gameObject.SetActive(false); // UI 비활성화
    }
    #endregion

    #region [5. 렌더링 파이프라인 (Data Polling & Drawing)]

    /// <summary>
    /// 현재 프레임에 Animator가 평가한 파라미터 값을 읽어 링을 갱신한다.
    /// LateUpdate를 사용해 일반 Update 이후의 값을 기준으로 그리지만, Animation Event와 렌더링 순서 전체를 보장하지는 않는다.
    /// </summary>
    private void LateUpdate()
    {
        if (!isPlaying || sourceAnimator == null || lr == null) return;

        // 1. 현재 상태 또는 전환 블렌딩으로 계산된 Float 값을 0~1 범위로 읽는다.
        float sampledProgress = Mathf.Clamp01(sourceAnimator.GetFloat(ParryTelegraphProgressId));

        // 2. 전환 블렌딩 중 값이 낮아져도 한 번 수축한 링이 다시 벌어지지 않게 한다.
        // BeginSignal과 StopSignal이 previousProgress를 0으로 초기화하므로, 새 공격은 다시 시작 반지름에서 출발한다.
        float progress = Mathf.Max(previousProgress, sampledProgress);
        previousProgress = progress;

        // 3. 반지름 선형 보간(Lerp): 진행도(0->1)에 따라 링의 크기를 시작(3f)에서 목표(0.5f)로 부드럽게 축소시킵니다.
        radius = Mathf.Lerp(startRadius, contactRadius, progress);

        // 4. 발광(Brightness) 보간: 정점(1.0)에 다가갈수록 링의 색상을 더 밝게 빛나게 만듭니다.
        float bright = Mathf.Lerp(0.6f, 1.4f, progress);
        lr.startColor = lr.endColor = goldColor * bright;

        // 5. 폴리곤 다시 그리기
        DrawCircle();
    }

    /// <summary>
    /// BossAnimationKeyReceiver가 Animation Event를 CanParryEvent로 변환해 발행할 때 호출된다.
    /// </summary>
    private void HandleCanParry(CanParryEvent data)
    {
        // DisableParry, 패링 성공, 공격 취소 경로가 발행한 false 신호에서는 사전신호를 종료한다.
        if (!data.CanParry)
        {
            StopSignal();
            return;
        }

        if (!isPlaying || hasReachedApex) return;

        // EnableParry 이벤트를 패링 시작의 최종 기준으로 사용한다.
        // 전환 블렌딩이나 커브 키 설정 오차로 sampledProgress가 아직 1이 아니어도,
        // 패링 창이 열리는 프레임에는 링을 정점으로 보정한다.
        // previousProgress도 1로 고정해 이후 LateUpdate에서 링이 다시 벌어지지 않게 한다.
        hasReachedApex = true;
        previousProgress = 1f;
        radius = contactRadius;
        DrawCircle();

        // 정점 연출 (중심점 번쩍임 및 시스템 전역 슬로모션 발동)
        if (centerDot != null) StartCoroutine(DotFlash());

        if (useSlowMo)
        {
            EventBus<SlowMoEvent>.Publish(new SlowMoEvent(
                slowMoScale,
                slowMoDuration,
                TimeEffectSource.Telegraph,
                TimeEffectPriority.Low,
                TimeEffectGroups.CombatFeel));
        }
    }

    // Animator Controller에 필요한 Float 파라미터가 등록되어 있는지만 확인한다.
    private bool HasProgressParameter(Animator animator)
    {
        foreach (AnimatorControllerParameter parameter in animator.parameters)
        {
            if (parameter.nameHash == ParryTelegraphProgressId)
                return true;
        }
        return false;
    }

    private void WarnInvalidAnimator(Animator animator)
    {
        if (hasWarnedInvalidAnimator) return;
        hasWarnedInvalidAnimator = true;
        string reason = animator == null ? "Animator 참조가 없습니다." : $"{ParryTelegraphProgressName} 파라미터가 없습니다.";
        Debug.LogWarning($"[RingDrawer] 패링 사전신호를 시작할 수 없습니다: {reason}", this);
    }

    // 중앙점 깜빡임을 여러 프레임에 나누어 실행하는 코루틴이다. 별도 스레드로 실행되지는 않는다.
    private IEnumerator DotFlash()
    {
        if (centerDotLine == null) yield break;

        float blinkDur = 0.08f;

        for (int i = 0; i < blinkCount; i++)
        {
            centerDotLine.startColor = centerDotLine.endColor = blinkColor;
            yield return new WaitForSecondsRealtime(blinkDur); // 엔진 시간이 멈춰도 현실 시간으로 작동 보장

            centerDotLine.startColor = centerDotLine.endColor = normalColor;
            yield return new WaitForSecondsRealtime(blinkDur);
        }
    }

    /// <summary>
    /// [수학적 렌더링 로직]: 라디안(Radian)을 이용한 삼각함수 좌표 계산
    /// 원을 segments 개수만큼 쪼개어(2π / segments), 각도(ang)에 따른 Cos(x축)와 Sin(y축)을 구해
    /// 현재 반지름(radius)을 곱해 LineRenderer에 원형에 가까운 다각형의 꼭짓점을 설정합니다.
    /// </summary>
    void DrawCircle()
    {
        for (int i = 0; i < segments; i++)
        {
            float ang = (float)i / segments * Mathf.PI * 2f;
            lr.SetPosition(i, new Vector3(Mathf.Cos(ang) * radius, Mathf.Sin(ang) * radius, 0f));
        }
    }
    #endregion
}
