using UnityEngine;

// 플레이어의 근접 패링 입력 시간을 관리합니다.
public class PlayerParry : MonoBehaviour
{
    [Header("Melee Parry")]
    // 패링 입력 후 근접 공격을 막을 수 있는 시간입니다.
    [SerializeField] private float parryWindowDuration = 0.2f;

    // 이전 원거리 패링 인스펙터 데이터가 남아 있어도 Inspector가 깨지지 않도록 숨겨서 보존합니다.
#pragma warning disable 0414
    [HideInInspector, SerializeField] private float parryRadius = 1.5f;
    [HideInInspector, SerializeField] private LayerMask parryMask;
#pragma warning restore 0414

    private PlayerController playerController;
    private bool isInitialized;
    private bool isParryWindowOpen;
    private bool isParryConsumed;
    private float parryWindowEndTime;

    // 다른 공격 판정 스크립트가 현재 패링 창이 열려 있는지 확인할 때 사용합니다.
    public bool IsParryWindowOpen => IsValidParryWindow();

    private void Awake()
    {
        // 실제 참조 연결은 PlayerInitializer에서 순서를 보장해 처리합니다.
    }

    public void Initialize(PlayerController controller)
    {
        // PlayerInitializer가 넘겨준 컨트롤러를 우선 사용하고, 없으면 같은 오브젝트에서 보강합니다.
        playerController = controller != null ? controller : GetComponent<PlayerController>();
        ResetParryWindow();
        isInitialized = true;
    }

    private void Update()
    {
        if (!isParryWindowOpen) return;
        if (Time.time <= parryWindowEndTime) return;

        ResetParryWindow();
    }

    private void OnDisable()
    {
        ResetParryWindow();
    }

    public bool TryParry()
    {
        // 현재 상태가 패링을 허용하지 않으면 패링 창을 열지 않습니다.
        if (!EnsureInitialized()) return false;
        if (!CanUseParry()) return false;

        isParryWindowOpen = true;
        isParryConsumed = false;
        parryWindowEndTime = Time.time + Mathf.Max(0f, parryWindowDuration);

        Debug.Log("근접 패링 준비");
        return true;
    }

    public bool TryConsumeMeleeParry(DamageInfo damageInfo)
    {
        // 몬스터 근접 공격이 데미지를 넣기 직전에 호출해 피해 취소 여부를 확인합니다.
        if (!EnsureInitialized()) return false;
        if (!CanUseParry()) return false;
        if (!IsValidParryWindow()) return false;
        if (!IsTargetSelf(damageInfo.TargetObject)) return false;

        isParryConsumed = true;
        ResetParryWindow();

        Debug.Log("근접 패링 성공");
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

    private bool IsValidParryWindow()
    {
        if (!isParryWindowOpen) return false;
        if (isParryConsumed) return false;
        if (Time.time > parryWindowEndTime) return false;

        return true;
    }

    private bool IsTargetSelf(GameObject targetObject)
    {
        // DamageInfo의 대상이 비어 있으면 현재 플레이어를 향한 직접 호출로 간주합니다.
        if (targetObject == null) return true;

        return targetObject == gameObject || targetObject.transform.IsChildOf(transform);
    }

    private void ResetParryWindow()
    {
        isParryWindowOpen = false;
        isParryConsumed = false;
        parryWindowEndTime = 0f;
    }
}
