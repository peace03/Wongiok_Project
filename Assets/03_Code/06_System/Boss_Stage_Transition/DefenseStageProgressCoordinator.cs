using UnityEngine;
using UnityEngine.Events;

public class DefenseStageProgressCoordinator : MonoBehaviour
{
    [Header("Required Defense Stages")]
    [SerializeField] private DefenseStageController[] requiredStages;

    [Header("Portal")]
    [SerializeField] private BossPortalTrigger bossPortal;

    [Header("Events")]
    [SerializeField] private UnityEvent onAllDefenseStagesCleared;

    private bool isPortalUnlocked;

    public bool IsPortalUnlocked => isPortalUnlocked;

    // 포탈 참조를 확인하고 잠긴 초기 상태로 준비합니다.
    private void Awake()
    {
        if (bossPortal == null)
        {
            Debug.LogError(
                "DefenseStageProgressCoordinator의 Boss Portal 참조가 비어 있습니다.",
                this);
            return;
        }

        bossPortal.SetUnlocked(false);
    }

    // 필수 디펜스존이 모두 완료되었으면 포탈을 활성화합니다.
    public void EvaluateProgress()
    {
        if (isPortalUnlocked)
        {
            return;
        }

        if (!AreAllRequiredStagesCleared())
        {
            return;
        }

        if (bossPortal == null)
        {
            Debug.LogError(
                "모든 디펜스존이 완료되었지만 Boss Portal 참조가 비어 있습니다.",
                this);
            return;
        }

        bossPortal.SetUnlocked(true);
        isPortalUnlocked = true;

        onAllDefenseStagesCleared?.Invoke();
    }

    // 모든 디펜스 컨트롤러의 초기화가 끝난 뒤 첫 완료 판정을 실행합니다.
    private void Start()
    {
        EvaluateProgress();
    }

    // 포탈이 열릴 때까지 필수 디펜스존의 완료 상태를 확인합니다.
    private void Update()
    {
        if (isPortalUnlocked)
        {
            return;
        }

        EvaluateProgress();
    }

    // 등록된 필수 디펜스존이 모두 클리어 상태인지 확인합니다.
    private bool AreAllRequiredStagesCleared()
    {
        if (requiredStages == null || requiredStages.Length == 0)
        {
            return false;
        }

        for (int i = 0; i < requiredStages.Length; i++)
        {
            DefenseStageController stage = requiredStages[i];

            if (stage == null || !stage.IsCleared)
            {
                return false;
            }
        }

        return true;
    }
}
