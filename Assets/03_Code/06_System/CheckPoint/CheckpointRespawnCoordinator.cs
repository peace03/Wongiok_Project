using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class CheckpointRespawnCoordinator : MonoBehaviour
{
    private static CheckpointRespawnCoordinator instance;

    [Header("Persistent Fallback")]
    [SerializeField] private CheckpointSaveSystem saveSystem;
    [SerializeField] private CheckpointCatalog checkpointCatalog;
    [SerializeField]
    private bool loadPersistentSaveWhenSessionIsEmpty = true;
    [SerializeField]
    private bool deletePersistentSaveOnNewGame = true;

    [Header("Scene Loading")]
    [SerializeField] private string normalSceneName = "Chapter1Scene";
    [SerializeField]
    private float playerInitializationTimeout = 10f;
    [SerializeField]
    private bool restoreTimeScaleOnRespawn = true;

    [Header("Debug")]
    [SerializeField] private bool logProcessing;

    private bool isRespawning;

    // 2026.08.07_psb수정
    // 보스 씬이 새 Player 프리팹을 생성해도 진입 직전의 플레이어 진행 정보를 이어서 적용하기 위한 임시 스냅샷이다.
    private CheckpointProgressSnapshot pendingBossSceneProgress;
    private bool hasPendingBossSceneProgress;
    private bool isRestoringBossSceneProgress;

    // 중복 인스턴스를 제거하고 씬 전환 중에도 유지합니다.
    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // 게임 오버 체크포인트 복귀 요청 이벤트를 구독합니다.
    private void OnEnable()
    {
        EventBus<UIGameOverLoadCheckpointRequestedEvent>.action +=
            HandleLoadCheckpointRequested;

        EventBus<UIGameOverRestartChapterRequestedEvent>.action +=
            HandleRestartChapterRequested;

        EventBus<UITitleNewGameRequestedEvent>.action +=
            HandleNewGameRequested;

        // 2026.08.07_psb수정
        // 보스 진입 직전에 현재 플레이어 진행도를 보관하고, 보스 씬의 Player 생성 완료 후 복원한다.
        EventBus<UIBossEncounterRequestedEvent>.action +=
            HandleBossEncounterRequested;
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    // 게임 오버 체크포인트 복귀 요청 이벤트를 해제합니다.
    private void OnDisable()
    {
        EventBus<UIGameOverLoadCheckpointRequestedEvent>.action -=
            HandleLoadCheckpointRequested;

        EventBus<UIGameOverRestartChapterRequestedEvent>.action -=
            HandleRestartChapterRequested;

        EventBus<UITitleNewGameRequestedEvent>.action -=
            HandleNewGameRequested;

        // 2026.08.07_psb수정
        EventBus<UIBossEncounterRequestedEvent>.action -=
            HandleBossEncounterRequested;
        SceneManager.sceneLoaded -= HandleSceneLoaded;
    }

    // 새 게임 요청 시 체크포인트와 디펜스 스테이지 런타임 상태를 제거합니다.
    private void HandleNewGameRequested(
        UITitleNewGameRequestedEvent eventData)
    {
        CheckpointRuntimeSession.ClearAll();
        DefenseStageRuntimeSession.ClearAll();

        if (deletePersistentSaveOnNewGame &&
            saveSystem != null)
        {
            saveSystem.DeleteCheckpointSave();
        }

        // 2026.08.07_psb수정
        hasPendingBossSceneProgress = false;
        pendingBossSceneProgress = default;
    }

    // 2026.08.07_psb수정
    // 보스 조우 연출이 씬 로드를 시작하기 전에 현재 일반 스테이지의 플레이어 진행도를 보관한다.
    private void HandleBossEncounterRequested(
        UIBossEncounterRequestedEvent eventData)
    {
        if (isRespawning ||
            !TryCaptureCurrentPlayerProgress(
                out CheckpointProgressSnapshot progress))
        {
            return;
        }

        pendingBossSceneProgress = progress;
        hasPendingBossSceneProgress = true;
    }

    // 2026.08.07_psb수정
    // 보스 씬이 활성화된 뒤에만 새 Player에 진입 직전 진행도를 적용한다.
    private void HandleSceneLoaded(
        Scene loadedScene,
        LoadSceneMode loadSceneMode)
    {
        if (!hasPendingBossSceneProgress ||
            isRespawning ||
            isRestoringBossSceneProgress)
        {
            return;
        }

        StartCoroutine(RestoreBossSceneProgressRoutine());
    }

    // 체크포인트 복귀 코루틴을 중복 없이 시작합니다.
    private void HandleLoadCheckpointRequested(
        UIGameOverLoadCheckpointRequestedEvent eventData)
    {
        if (isRespawning)
        {
            return;
        }

        // 2026.08.07_psb수정
        // 사망 처리 후 목숨이 없으면 씬을 바꾸지 않고 게임오버 Alert만 표시한다.
        PlayerLifeTracker lifeTracker =
            FindFirstObjectByType<PlayerLifeTracker>();

        if (lifeTracker == null || !lifeTracker.HasRemainingLife)
        {
            EventBus<UIShowAlertPopupEvent>.Publish(
                new UIShowAlertPopupEvent(
                    "알림",
                    "모든 목숨을 소진했습니다."));
            return;
        }

        StartCoroutine(RespawnAtCheckpointRoutine());
    }

    private void HandleRestartChapterRequested(
        UIGameOverRestartChapterRequestedEvent eventData)
    {
        if (isRespawning)
            return;

        StartCoroutine(RestartChapterRoutine());
    }

    private IEnumerator RestartChapterRoutine()
    {
        isRespawning = true;
        Time.timeScale = 1f;

        CheckpointRuntimeSession.ClearAll();
        DefenseStageRuntimeSession.ClearAll();

        // 2026.08.07_psb수정
        hasPendingBossSceneProgress = false;
        pendingBossSceneProgress = default;

        // 2026.08.07_psb수정
        // 보스 씬에서도 일반 스테이지의 초기 능력치와 시작 지점으로 되돌린다.
        PrototypeGameSession.RestartRunFromBeginning();

        if (saveSystem != null)
        {
            saveSystem.DeleteCheckpointSave();
        }

        AsyncOperation loadOperation =
            SceneManager.LoadSceneAsync(
                normalSceneName,
                LoadSceneMode.Single);

        if (loadOperation != null)
        {
            while (!loadOperation.isDone)
                yield return null;
        }

        isRespawning = false;
    }

    // 현재 체크포인트 Scene을 다시 불러오고 새 Player를 복원합니다.
    private IEnumerator RespawnAtCheckpointRoutine()
    {
        isRespawning = true;

        if (!TryPrepareRuntimeCheckpoint())
        {
            isRespawning = false;
            yield break;
        }

        CheckpointRuntimeData data =
            CheckpointRuntimeSession.Current;

        PlayerLifeTracker currentLifeTracker =
            FindFirstObjectByType<PlayerLifeTracker>();

        if (currentLifeTracker == null)
        {
            Debug.LogWarning(
                "PlayerLifeTracker가 없어 남은 잔기를 보존할 수 없습니다.",
                this);

            isRespawning = false;
            yield break;
        }

        // 2026.08.07_psb수정
        // 체크포인트 저장 당시의 목숨이 아니라 이번 사망으로 차감된 현재 목숨을 유지한다.
        int remainingLifeCount = currentLifeTracker.CurrentLifeCount;

        if (remainingLifeCount <= 0)
        {
            Debug.LogWarning(
                "남은 잔기가 없어 체크포인트에서 부활할 수 없습니다.",
                this);

            isRespawning = false;
            yield break;
        }

        if (!Application.CanStreamedLevelBeLoaded(
                data.ScenePath))
        {
            Debug.LogError(
                $"Build Profile에서 Scene을 찾을 수 없습니다: " +
                $"{data.ScenePath}",
                this);

            isRespawning = false;
            yield break;
        }

        CheckpointRuntimeSession.SetPendingLifeCount(
            remainingLifeCount);

        AsyncOperation loadOperation =
            SceneManager.LoadSceneAsync(
                data.ScenePath,
                LoadSceneMode.Single);

        if (loadOperation == null)
        {
            Debug.LogError(
                $"Scene 로드를 시작하지 못했습니다: " +
                $"{data.ScenePath}",
                this);

            CheckpointRuntimeSession.ClearPendingLifeCount();

            isRespawning = false;
            yield break;
        }

        while (!loadOperation.isDone)
        {
            yield return null;
        }

        PlayerRuntimeReferences references = null;
        float elapsed = 0f;

        while (elapsed < playerInitializationTimeout)
        {
            if (TryFindInitializedPlayer(
                    out references))
            {
                break;
            }

            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        if (references == null)
        {
            Debug.LogError(
                "Scene 로드 후 초기화된 Player를 찾지 못했습니다.",
                this);

            CheckpointRuntimeSession.ClearPendingLifeCount();

            isRespawning = false;
            yield break;
        }

        RestoreLoadedPlayer(
            references,
            data,
            CheckpointRuntimeSession.PendingLifeCount);

        CheckpointRuntimeSession.ClearPendingLifeCount();

        if (restoreTimeScaleOnRespawn)
        {
            Time.timeScale = 1f;
        }

        EventBus<UIChangeScreenEvent>.Publish(
            new UIChangeScreenEvent(
                UIScreenState.InGame));

        if (logProcessing)
        {
            Debug.Log(
                $"Checkpoint respawn complete: " +
                $"{data.CheckpointId}",
                this);
        }

        isRespawning = false;
    }

    // 2026.08.07_psb수정
    // 보스 씬의 Player 초기화가 끝날 때까지 기다린 뒤, 위치와 체크포인트는 건드리지 않고 진행 정보만 이어받는다.
    private IEnumerator RestoreBossSceneProgressRoutine()
    {
        isRestoringBossSceneProgress = true;

        PlayerRuntimeReferences references = null;
        float elapsed = 0f;

        while (elapsed < playerInitializationTimeout)
        {
            if (TryFindInitializedPlayer(out references))
                break;

            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        if (references != null)
        {
            RestorePlayerProgress(
                references,
                pendingBossSceneProgress);
        }
        else
        {
            Debug.LogWarning(
                "보스 씬의 Player 초기화가 완료되지 않아 진행 정보를 복원하지 못했습니다.",
                this);
        }

        hasPendingBossSceneProgress = false;
        pendingBossSceneProgress = default;
        isRestoringBossSceneProgress = false;
    }

    // 런타임 체크포인트가 없으면 선택적으로 영구 저장을 불러옵니다.
    private bool TryPrepareRuntimeCheckpoint()
    {
        if (CheckpointRuntimeSession.HasActiveCheckpoint)
        {
            return true;
        }

        if (!loadPersistentSaveWhenSessionIsEmpty ||
            saveSystem == null)
        {
            Debug.LogWarning(
                "활성 체크포인트가 없습니다.",
                this);

            return false;
        }

        bool restored =
            saveSystem.RestoreRuntimeSession(
                checkpointCatalog);

        if (!restored)
        {
            Debug.LogWarning(
                "불러올 체크포인트 저장 데이터가 없습니다.",
                this);
        }

        return restored;
    }

    // 2026.08.07_psb수정
    // 보스 진입 시 이어받아야 하는 체력·목숨·경험치·스탯·스킬 정보를 현재 Player에서 수집한다.
    private bool TryCaptureCurrentPlayerProgress(
        out CheckpointProgressSnapshot progress)
    {
        progress = default;

        PlayerStatus playerStatus =
            FindFirstObjectByType<PlayerStatus>();
        PlayerLifeTracker lifeTracker =
            FindFirstObjectByType<PlayerLifeTracker>();
        PlayerHealItemInventory healItemInventory =
            FindFirstObjectByType<PlayerHealItemInventory>();
        PlayerExperienceTracker experienceTracker =
            FindFirstObjectByType<PlayerExperienceTracker>();
        SkillSystemController skillSystem =
            FindFirstObjectByType<SkillSystemController>();

        if (playerStatus == null ||
            lifeTracker == null ||
            healItemInventory == null ||
            experienceTracker == null)
        {
            return false;
        }

        progress = new CheckpointProgressSnapshot
        {
            IsValid = true,
            CurrentHP = playerStatus.GetCurrentHP(),
            HealItemCount = healItemInventory.CurrentCount,
            LifeCount = lifeTracker.CurrentLifeCount,
            PlayerLevel = experienceTracker.CurrentLevel,
            CurrentExperience = experienceTracker.CurrentExp,
            PersistentStats =
                playerStatus.CapturePersistentStatSnapshot(),
            Skills = skillSystem != null
                ? skillSystem.CaptureCheckpointSnapshot()
                : System.Array.Empty<CheckpointSkillSnapshot>(),
            ClearedDefenseStageIds =
                DefenseStageRuntimeSession.CaptureCurrentState()
        };

        return true;
    }

    // PlayerInitializer 완료를 공개 상태 값으로 확인합니다.
    private bool TryFindInitializedPlayer(
        out PlayerRuntimeReferences references)
    {
        references = null;

        PlayerStatus playerStatus =
            FindFirstObjectByType<PlayerStatus>();

        PlayerController playerController =
            FindFirstObjectByType<PlayerController>();

        PlayerLifeTracker lifeTracker =
            FindFirstObjectByType<PlayerLifeTracker>();

        PlayerHealItemInventory healItemInventory =
            FindFirstObjectByType<PlayerHealItemInventory>();

        PlayerCheckpointTracker checkpointTracker =
            FindFirstObjectByType<PlayerCheckpointTracker>();

        PlayerExperienceTracker experienceTracker =
            FindFirstObjectByType<PlayerExperienceTracker>();

        SkillSystemController skillSystem =
            FindFirstObjectByType<SkillSystemController>();

        if (playerStatus == null ||
            playerController == null ||
            lifeTracker == null ||
            healItemInventory == null ||
            checkpointTracker == null ||
            experienceTracker == null)
        {
            return false;
        }

        if (playerStatus.GetMaxHP() <= 0f ||
            playerController.Cc == null)
        {
            return false;
        }

        references = new PlayerRuntimeReferences(
            playerStatus,
            playerController,
            lifeTracker,
            healItemInventory,
            checkpointTracker,
            experienceTracker,
            skillSystem);

        return true;
    }

    // 새 Scene의 Player에 잔기와 아이템 및 부활 위치를 적용합니다.
    private void RestoreLoadedPlayer(
        PlayerRuntimeReferences references,
        CheckpointRuntimeData data,
        int remainingLifeCount)
    {
        CheckpointProgressSnapshot progress = data.Progress;

        if (progress.IsValid)
        {
            RestorePlayerProgress(references, progress);
        }
        else
        {
            references.PlayerStatus.RestoreCurrentHP(data.SavedHP);
            DefenseStageRuntimeSession.RestoreCheckpointSnapshot();
        }

        // 2026.08.07_psb수정
        references.LifeTracker.RestoreCount(remainingLifeCount);

        references.HealItemInventory.RestoreCount(
            progress.IsValid
                ? progress.HealItemCount
                : data.SavedHealItemCount);

        references.CheckpointTracker.RestoreCheckpoint(data);

        references.PlayerController.TeleportTo(
            data.RespawnPosition);

        references.PlayerController.transform.rotation =
            data.RespawnRotation;
    }

    // 2026.08.07_psb수정
    // 체크포인트 부활과 보스 씬 진입이 동일한 진행 데이터 복원 규칙을 사용하도록 공통화한다.
    private void RestorePlayerProgress(
        PlayerRuntimeReferences references,
        CheckpointProgressSnapshot progress)
    {
        if (!progress.IsValid)
            return;

        references.SkillSystem?.RestoreCheckpointSnapshot(
            progress.Skills);
        references.PlayerStatus.ApplyPersistentStatSnapshot(
            progress.PersistentStats);
        references.ExperienceTracker.RestoreProgress(
            progress.PlayerLevel,
            progress.CurrentExperience);
        references.PlayerStatus.RestoreCurrentHP(progress.CurrentHP);
        references.LifeTracker.RestoreCount(progress.LifeCount);
        references.HealItemInventory.RestoreCount(progress.HealItemCount);
        DefenseStageRuntimeSession.RestoreCheckpointSnapshot(
            progress.ClearedDefenseStageIds);
    }

    private sealed class PlayerRuntimeReferences
    {
        public PlayerStatus PlayerStatus { get; }
        public PlayerController PlayerController { get; }
        public PlayerLifeTracker LifeTracker { get; }
        public PlayerHealItemInventory HealItemInventory { get; }
        public PlayerCheckpointTracker CheckpointTracker { get; }
        public PlayerExperienceTracker ExperienceTracker { get; }
        public SkillSystemController SkillSystem { get; }

        // Scene 로드 후 찾은 Player 컴포넌트 참조를 저장합니다.
        public PlayerRuntimeReferences(
            PlayerStatus playerStatus,
            PlayerController playerController,
            PlayerLifeTracker lifeTracker,
            PlayerHealItemInventory healItemInventory,
            PlayerCheckpointTracker checkpointTracker,
            PlayerExperienceTracker experienceTracker,
            SkillSystemController skillSystem)
        {
            PlayerStatus = playerStatus;
            PlayerController = playerController;
            LifeTracker = lifeTracker;
            HealItemInventory = healItemInventory;
            CheckpointTracker = checkpointTracker;
            ExperienceTracker = experienceTracker;
            SkillSystem = skillSystem;
        }
    }
}
