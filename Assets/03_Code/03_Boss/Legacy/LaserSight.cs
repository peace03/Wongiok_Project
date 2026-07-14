using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class LaserSight : MonoBehaviour
{
    [Header("위치 참조")]
    [Tooltip("레이저가 발사될 총구 위치")]
    [SerializeField] private Transform muzzlePos;
    [Tooltip("레이저가 조준할 타겟 위치")]
    [SerializeField] private Transform targetPos;

    [Header("레이저 판정")]
    [Tooltip("레이저가 뚫지 못하고 막힐 레이어 (지형, 플레이어 등)")]
    [SerializeField] private LayerMask obstacleLayer;
    [Tooltip("레이저 최대 사거리")]
    [SerializeField, Min(0f)] private float maxLaserDistance = 50f;

    [Header("레이저 시각 설정")]
    [Tooltip("레이저 색상. 알파값을 낮추면 반투명해진다.")]
    [SerializeField] private Color coreColor = new Color(1f, 0.08f, 0.04f, 1f);
    [Tooltip("레이저 굵기")]
    [SerializeField, Min(0.001f)] private float coreWidth = 0.06f;
    [Tooltip("레이저에 사용할 머티리얼. 비워두면 LineRenderer에 이미 설정된 머티리얼을 사용한다.")]
    [SerializeField] private Material coreMaterial;
    [Tooltip("Aim 고정되었을 때 깜빡이는 시간간격")]
    [SerializeField] private float blinkGap;


    private LineRenderer line;
    private bool isAiming = false;  // 조준 렌더링 스위치
    private bool aimLock = false;   //조준 잠금
    private float blinkTimer = 0f;  //깜빡임 지속시간

    private void Awake()
    {
        EnsureLineRenderer();
        ApplyVisualSettings();
        SetLineEnabled(false);
    }

    private void OnValidate() //인스펙터에서 조절할 때마다 실행
    {
        coreWidth = Mathf.Max(0.001f, coreWidth);
        maxLaserDistance = Mathf.Max(0f, maxLaserDistance);

        // 인스펙터에서 색상/굵기를 바꿀 때 단일 LineRenderer에 즉시 반영한다.
        line = GetComponent<LineRenderer>();
        ApplyVisualSettings();
    }

    // 보스 패턴에서 사전신호 시작할 때 호출
    public void StartAiming()
    {
        EnsureLineRenderer();
        ApplyVisualSettings();
        isAiming = true;
        aimLock = false;
        blinkTimer = 0f;
        SetLineEnabled(true);
        UpdateLaserVisual();
    }

    // 사격 직전 레이저 끄기
    public void StopAiming()
    {
        isAiming = false;
        SetLineEnabled(false);
    }

    //조준 잠금
    public void LockAim()
    {
        aimLock = true;
    }
    public bool GetAimLock() { return aimLock; }

    private void Update()
    {
        if (!isAiming) return; // 조준 중이 아니면 연산 생략
        UpdateLaserVisual();
    }

    private void UpdateLaserVisual()
    {
        if (muzzlePos == null || targetPos == null)
        {
            SetLineEnabled(false);
            return;
        }

        if(aimLock == false)
        {
            Vector3 startPos = muzzlePos.position;
            Vector3 dirToTarget = (targetPos.position - startPos).normalized;

            if (dirToTarget == Vector3.zero)
                dirToTarget = transform.forward;

            Vector3 endPos;

            // 물리적 레이캐스트(벽뚫림 방지)
            // 총구 위치에서 타겟 방향으로 레이저를 쏴서 obstacleLayer에 닿는 무언가가 있는지 검사한다.
            if (Physics.Raycast(startPos, dirToTarget, out RaycastHit hit, maxLaserDistance, obstacleLayer))
                endPos = hit.point; // 타겟에 맞았다면, 끝점을 충돌 좌표로 설정
            else
                endPos = startPos + dirToTarget * maxLaserDistance; // 충돌한 것이 없다면 최대 사거리까지 렌더링
            SetLinePositions(startPos, endPos);
        }
        else
        {
            blinkTimer += Time.deltaTime;
            if (blinkTimer >= blinkGap) { blinkTimer = 0f; line.enabled = !line.enabled; }
        }
    }

    private void EnsureLineRenderer()
    {
        line = GetComponent<LineRenderer>();
        if (line == null) return;

        line.positionCount = 2; // 레이저는 시작점, 끝점 2개만 필요하다.
        line.useWorldSpace = true;
        line.loop = false;
    }

    private void ApplyVisualSettings()
    {
        if (line == null) return;

        line.positionCount = 2;
        line.widthMultiplier = coreWidth;
        line.startColor = coreColor;
        line.endColor = coreColor;

        // 머티리얼을 강제로 새로 만들지 않고, 인스펙터에서 넣은 경우에만 교체한다.
        // 이렇게 해야 기존 씬/프리팹의 LineRenderer 머티리얼 세팅을 덮어쓰지 않는다.
        if (coreMaterial != null)
            line.sharedMaterial = coreMaterial;
    }

    private void SetLinePositions(Vector3 startPos, Vector3 endPos)
    {
        if (line == null) return;

        line.SetPosition(0, startPos);
        line.SetPosition(1, endPos);
    }

    private void SetLineEnabled(bool enabled)
    {
        if (line != null)
            line.enabled = enabled;
    }
}
