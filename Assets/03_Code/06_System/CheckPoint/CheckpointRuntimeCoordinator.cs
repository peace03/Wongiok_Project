using UnityEngine;

public class CheckpointRuntimeCoordinator : MonoBehaviour
{
    [Header("Checkpoint Services")]
    [SerializeField] private CheckpointSaveSystem saveSystem;
    [SerializeField] private CheckpointUI checkpointUI;
    [SerializeField] private BossPreparationUI bossPreparationUI;

    [Header("스킬 시스템")]
    [SerializeField] private SkillSystemController skillSystem;

    [Header("Optional Features")]
    [SerializeField] private bool enablePersistentSave;
    [SerializeField] private bool enableBossPreparationUI;

    [Header("Boss Checkpoints")]
    [SerializeField] private int[] bossCheckpointNumbers;

    [Header("Debug")]
    [SerializeField] private bool logProcessing;

    // Stage 체크포인트 활성화 이벤트를 구독합니다
    private void OnEnable()
    {
        EventBus<StageCheckpointActivatedEvent>.action +=
            HandleCheckpointActivated;
    }

    // Stage 체크포인트 활성화 이벤트 구독을 해제합니다
    private void OnDisable()
    {
        EventBus<StageCheckpointActivatedEvent>.action -=
            HandleCheckpointActivated;
    }

    // 회복과 런타임 저장 및 체크포인트 연출을 처리합니다.
    private void HandleCheckpointActivated(
        StageCheckpointActivatedEvent checkpointEvent)
    {
        if (!TryResolveHealSystem(
                checkpointEvent.PlayerObject,
                out CheckpointHealSystem healSystem))
        {
            return;
        }

        CheckpointDefinition definition =
            checkpointEvent.Definition;

        bool didRecover =
            healSystem.RecoverMissingResources(definition);

        healSystem.CaptureSnapshot(
            out float currentHP,
            out int currentHealItemCount);

        CheckpointProgressSnapshot progress =
            CaptureProgressSnapshot(
                checkpointEvent.PlayerObject,
                currentHP,
                currentHealItemCount);

        CheckpointRuntimeData runtimeData =
            CheckpointRuntimeData.FromDefinition(
                definition,
                currentHP,
                currentHealItemCount,
                progress);

        CheckpointRuntimeSession.SetActiveCheckpoint(
            runtimeData);

        DefenseStageRuntimeSession.CaptureCheckpointSnapshot();

        if (enablePersistentSave && saveSystem != null)
        {
            saveSystem.SaveCheckpoint(runtimeData);
        }

        ShowCheckpointUI(definition.DisplayNumber);

        PlayCheckpointEffect(
            checkpointEvent.CheckpointObject);

        OpenBossPreparationIfNeeded(
            definition.DisplayNumber);

        if (logProcessing)
        {
            Debug.Log(
                $"Checkpoint activated: " +
                $"{definition.CheckpointId}, " +
                $"Recovered: {didRecover}",
                checkpointEvent.CheckpointObject);
        }
    }

    private CheckpointProgressSnapshot CaptureProgressSnapshot(
        GameObject playerObject,
        float currentHP,
        int healItemCount)
    {
        PlayerStatus playerStatus =
            playerObject.GetComponent<PlayerStatus>();
        PlayerLifeTracker lifeTracker =
            playerObject.GetComponent<PlayerLifeTracker>();
        PlayerExperienceTracker experienceTracker =
            playerObject.GetComponent<PlayerExperienceTracker>();

        return new CheckpointProgressSnapshot
        {
            IsValid = true,
            CurrentHP = currentHP,
            HealItemCount = healItemCount,
            LifeCount = lifeTracker != null
                ? lifeTracker.CurrentLifeCount
                : 0,
            PlayerLevel = experienceTracker != null
                ? experienceTracker.CurrentLevel
                : 1,
            CurrentExperience = experienceTracker != null
                ? experienceTracker.CurrentExp
                : 0f,
            PersistentStats = playerStatus != null
                ? playerStatus.CapturePersistentStatSnapshot()
                : default,
            Skills = skillSystem != null
                ? skillSystem.CaptureCheckpointSnapshot()
                : System.Array.Empty<CheckpointSkillSnapshot>(),
            ClearedDefenseStageIds =
                DefenseStageRuntimeSession.CaptureCurrentState()
        };
    }

    // Player에 부착된 Stage 소유 회복 시스템을 찾습니다
    private bool TryResolveHealSystem(
        GameObject playerObject,
        out CheckpointHealSystem healSystem)
    {
        healSystem = null;

        if (playerObject == null)
        {
            Debug.LogWarning(
                "Checkpoint event has no player object",
                this);
            return false;
        }

        healSystem =
            playerObject.GetComponentInParent<
                CheckpointHealSystem>();

        if (healSystem != null)
        {
            return true;
        }

        Debug.LogWarning(
            "CheckpointHealSystem is missing on Player",
            playerObject);
        return false;
    }

    // 체크포인트 번호를 알림 UI에 표시합니다
    private void ShowCheckpointUI(int checkpointNumber)
    {
        if (checkpointUI == null)
        {
            return;
        }

        checkpointUI.ShowCheckpoint(checkpointNumber);
    }

    // 실제 활성화된 체크포인트의 최초 연출만 재생합니다
    private void PlayCheckpointEffect(
        GameObject checkpointObject)
    {
        if (checkpointObject == null)
        {
            return;
        }

        CheckpointEffect checkpointEffect =
            checkpointObject.GetComponent<CheckpointEffect>();

        if (checkpointEffect == null)
        {
            checkpointEffect =
                checkpointObject.GetComponentInChildren<
                    CheckpointEffect>(true);
        }

        checkpointEffect?.PlayOnce();
    }

    // 설정된 보스 체크포인트에서만 준비 화면을 엽니다
    private void OpenBossPreparationIfNeeded(
        int checkpointNumber)
    {
        if (!enableBossPreparationUI ||
            bossPreparationUI == null ||
            !IsBossCheckpoint(checkpointNumber))
        {
            return;
        }

        bossPreparationUI.Open();
    }

    // 전달된 번호가 보스 준비 체크포인트인지 확인합니다
    private bool IsBossCheckpoint(int checkpointNumber)
    {
        if (bossCheckpointNumbers == null)
        {
            return false;
        }

        for (int i = 0;
             i < bossCheckpointNumbers.Length;
             i++)
        {
            if (bossCheckpointNumbers[i] ==
                checkpointNumber)
            {
                return true;
            }
        }

        return false;
    }
}
