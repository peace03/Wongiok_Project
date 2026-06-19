using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

// 플레이어의 보스 근접 패링 입력 창과 보스 애니메이션 패링 가능 창을 함께 관리합니다.
public class PlayerParry : MonoBehaviour
{
    private const float ParryGizmoRadius = 1.2f;
    private const float ParrySuccessDamageBlockDuration = 0.1f;

    [Header("Boss Melee Parry")]
    // 패링 입력 후 보스 근접 공격을 막을 수 있는 시간입니다.
    [SerializeField] private float parryWindowDuration = 0.2f;

    // 이전 원거리 패링 인스펙터 데이터가 남아 있어도 Inspector가 깨지지 않도록 숨겨서 보존합니다.
#pragma warning disable 0414
    [HideInInspector, SerializeField] private float parryRadius = 1.5f;
    [HideInInspector, SerializeField] private LayerMask parryMask;
#pragma warning restore 0414

    private PlayerController playerController;
    private bool isInitialized;
    private bool isPlayerParryWindowOpen;
    private bool isBossParryWindowOpen;
    private float playerParryWindowEndTime;
    private float parrySuccessDamageBlockEndTime;

    public bool LastParrySucceeded { get; private set; }

    private void Awake()
    {
        // 실제 참조 연결은 PlayerInitializer에서 순서를 보장해 처리합니다.
    }

    private void OnEnable()
    {
        EventBus<CanParryEvent>.action += SetBossParryWindow;
        EventBus<UltimateInvoke>.action += CloseBossParryWindow;
    }

    private void OnDisable()
    {
        EventBus<CanParryEvent>.action -= SetBossParryWindow;
        EventBus<UltimateInvoke>.action -= CloseBossParryWindow;
        ResetPlayerParryWindow();
        isBossParryWindowOpen = false;
        parrySuccessDamageBlockEndTime = 0f;
    }

    private void Update()
    {
        if (!isPlayerParryWindowOpen) return;
        if (Time.time <= playerParryWindowEndTime) return;

        ResetPlayerParryWindow();
    }

    public void Initialize(PlayerController controller)
    {
        // PlayerInitializer가 넘겨준 컨트롤러를 우선 사용하고, 없으면 같은 오브젝트에서 보강합니다.
        playerController = controller != null ? controller : GetComponent<PlayerController>();
        ResetPlayerParryWindow();
        isInitialized = true;
    }

    public bool TryParry()
    {
        // 현재 상태가 패링을 허용하지 않으면 패링 창을 열지 않습니다.
        if (!EnsureInitialized()) return false;
        if (!CanUseParry()) return false;

        LastParrySucceeded = false;
        isPlayerParryWindowOpen = true;
        playerParryWindowEndTime = Time.time + Mathf.Max(0f, parryWindowDuration);

        if (isBossParryWindowOpen)
        {
            // 보스 패턴은 입력 순간 ParryKeyDown을 받아 패링 분기로 넘어가는 구조입니다.
            return CompleteBossParry();
        }

        Debug.Log("플레이어 패링 입력");
        return true;
    }

    public bool TryConsumeBossParry()
    {
        // 입력 순간 패링은 성공했지만 히트박스가 같은 프레임에 남아 들어오는 경우 데미지만 막습니다.
        if (Time.time <= parrySuccessDamageBlockEndTime) return true;

        // 보스 히트박스가 데미지를 넣는 순간, 보스/플레이어 패링 창이 모두 열려 있는지 확인합니다.
        if (!EnsureInitialized()) return false;
        if (!CanUseParry()) return false;
        if (!isBossParryWindowOpen) return false;
        if (!IsPlayerParryWindowValid()) return false;

        return CompleteBossParry();
    }

    private bool CompleteBossParry()
    {
        // 패링 성공을 보스 패턴과 플레이어 데미지 방어 양쪽에 동시에 반영합니다.
        ResetPlayerParryWindow();
        isBossParryWindowOpen = false;
        LastParrySucceeded = true;
        parrySuccessDamageBlockEndTime = Time.time + ParrySuccessDamageBlockDuration;

        EventBus<ParryKeyDown>.Publish(default);
        Debug.Log("보스 근접 패링 성공");
        return true;
    }

    private bool EnsureInitialized()
    {
        // 테스트 씬에서 PlayerInitializer 순서가 빠졌더라도 최소 참조만 보강합니다.
        if (isInitialized) return true;

        Initialize(GetComponent<PlayerController>());
        return isInitialized;
    }

    private bool CanUseParry()
    {
        return playerController == null || playerController.CanParry;
    }

    private bool IsPlayerParryWindowValid()
    {
        if (!isPlayerParryWindowOpen) return false;
        if (Time.time > playerParryWindowEndTime) return false;

        return true;
    }

    private void SetBossParryWindow(CanParryEvent data)
    {
        // 보스 애니메이션 이벤트가 알려주는 패링 가능 KeyFrame 상태를 저장합니다.
        isBossParryWindowOpen = data.CanParry;
    }

    private void CloseBossParryWindow(UltimateInvoke data)
    {
        // 궁극기 전환 시 이전 패링 가능 창이 남지 않도록 닫습니다.
        isBossParryWindowOpen = false;
    }

    private void ResetPlayerParryWindow()
    {
        isPlayerParryWindowOpen = false;
        playerParryWindowEndTime = 0f;
    }

    private void OnDrawGizmos()
    {
        // 런타임 중 현재 패링 타이밍 상태를 Scene 뷰에서 색으로 확인합니다.
        bool playerWindowOpen = Application.isPlaying && IsPlayerParryWindowValid();
        bool bossWindowOpen = Application.isPlaying && isBossParryWindowOpen;

        if (!playerWindowOpen && !bossWindowOpen)
        {
            DrawParryTimingGizmo(new Color(1f, 1f, 1f, 0.25f), "Parry Idle");
            return;
        }

        if (playerWindowOpen && bossWindowOpen)
        {
            DrawParryTimingGizmo(Color.green, "Parry OK");
            return;
        }

        if (playerWindowOpen)
        {
            DrawParryTimingGizmo(Color.yellow, "Player Window");
            return;
        }

        DrawParryTimingGizmo(Color.cyan, "Boss Window");
    }

    private void DrawParryTimingGizmo(Color color, string label)
    {
        // 플레이어 몸통 주변에 패링 타이밍 상태를 표시합니다.
        Vector3 center = transform.position + Vector3.up;
        Gizmos.color = color;
        Gizmos.DrawWireSphere(center, ParryGizmoRadius);

        Color fillColor = color;
        fillColor.a = 0.12f;
        Gizmos.color = fillColor;
        Gizmos.DrawSphere(center, ParryGizmoRadius);

#if UNITY_EDITOR
        Handles.color = color;
        Handles.Label(center + Vector3.up * (ParryGizmoRadius + 0.25f), label);
#endif
    }
}
