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


    // 김연호 : 체크포인트, 챕터 진입, 새 게임, 보스 전환에 필요한 런타임 이벤트를 구독합니다.
    private void OnEnable()
    {
        EventBus<UIGameOverLoadCheckpointRequestedEvent>.action +=
            HandleLoadCheckpointRequested;

        EventBus<UIGameOverRestartChapterRequestedEvent>.action +=
            HandleRestartChapterRequested;

        EventBus<UITitleNewGameRequestedEvent>.action +=
            HandleNewGameRequested;

        // 김연호 : 새 챕터가 시작되기 전에 이전 런의 디펜스 클리어 상태를 제거하기 위해 구독합니다.
        EventBus<UIChapterEnterRequestedEvent>.action +=
            HandleChapterEnterRequested;

        // 2026.08.07_psb수정
        // 보스 진입 직전에 현재 플레이어 진행도를 보관하고, 보스 씬의 Player 생성 완료 후 복원한다.
        EventBus<UIBossEncounterRequestedEvent>.action +=
            HandleBossEncounterRequested;

        SceneManager.sceneLoaded +=
            HandleSceneLoaded;
    }


    // 김연호 : 등록한 체크포인트, 챕터 진입, 새 게임, 보스 전환 이벤트 구독을 해제합니다.
    private void OnDisable()
    {
        EventBus<UIGameOverLoadCheckpointRequestedEvent>.action -=
            HandleLoadCheckpointRequested;

        EventBus<UIGameOverRestartChapterRequestedEvent>.action -=
            HandleRestartChapterRequested;

        EventBus<UITitleNewGameRequestedEvent>.action -=
            HandleNewGameRequested;

        // 김연호 : 챕터 진입 이벤트가 중복 호출되지 않도록 구독을 해제합니다.
        EventBus<UIChapterEnterRequestedEvent>.action -=
            HandleChapterEnterRequested;

        // 2026.08.07_psb수정
        EventBus<UIBossEncounterRequestedEvent>.action -=
            HandleBossEncounterRequested;

        SceneManager.sceneLoaded -=
            HandleSceneLoaded;
    }


    // 김연호 : 새 챕터 진입 시 이전 플레이에서 남은 디펜스 스테이지 클리어 상태를 초기화합니다.
    private void HandleChapterEnterRequested(
        UIChapterEnterRequestedEvent eventData)
    {
        // 김연호 : 같은 챕터를 다시 진행하더라도 이전 런의 디펜스 클리어 정보가 재사용되지 않도록 합니다.
        DefenseStageRuntimeSession.ClearAll();
    }


    // 김연호 : 새 게임 요청 시 체크포인트와 디펜스 스테이지 런타임 상태를 초기화합니다.
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
        // 이전 런에서 보스 씬으로 전달하려던 진행 정보가 새 게임에 남지 않도록 제거한다.
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

        StartCoroutine(
            RestoreBossSceneProgressRoutine());
    }


    // 체크포인트 복귀 요청을 확인하고 남은 잔기가 있을 때만 복귀 코루틴을 시작합니다.
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

        if (lifeTracker == null ||
            !lifeTracker.HasRemainingLife)
        {
            EventBus<UIShowAlertPopupEvent>.Publish(
                new UIShowAlertPopupEvent(
                    "알림",
                    "모든 목숨을 소진했습니다."));

            return;
        }

        StartCoroutine(
            RespawnAtCheckpointRoutine());
    }


    // 챕터 재시작 요청을 확인하고 중복 처리 없이 재시작 코루틴을 시작합니다.
    private void HandleRestartChapterRequested(
        UIGameOverRestartChapterRequestedEvent eventData)
    {
        if (isRespawning)
        {
            return;
        }

        StartCoroutine(
            RestartChapterRoutine());
    }


    // 챕터의 체크포인트와 디펜스 진행 상태를 초기화하고 시작 Scene을 다시 불러옵니다.
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
            {
                yield return null;
            }
        }

        isRespawning = false;
    }


    // 현재 체크포인트 Scene을 다시 불러오고 저장된 Player 상태와 진행 상태를 복원합니다.
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
        int remainingLifeCount =
            currentLifeTracker.CurrentLifeCount;

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

        // 김연호 : 새 Scene의 DefenseStageController.Awake가 실행되기 전에
        // 체크포인트 저장 시점의 디펜스 클리어 상태를 먼저 복원합니다.
        RestoreDefenseStageStateBeforeSceneLoad(
            data);

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

            elapsed +=
                Time.unscaledDeltaTime;

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


    // 김연호 : 새 Scene의 DefenseStageController가 초기화되기 전에 체크포인트 시점의 디펜스 스테이지 상태를 복원합니다.
    private void RestoreDefenseStageStateBeforeSceneLoad(
        CheckpointRuntimeData data)
    {
        CheckpointProgressSnapshot progress =
            data.Progress;

        if (progress.IsValid)
        {
            // 김연호 : 체크포인트 저장 당시 클리어되어 있던 디펜스 스테이지만 다시 적용합니다.
            DefenseStageRuntimeSession.RestoreCheckpointSnapshot(
                progress.ClearedDefenseStageIds);

            return;
        }

        // 김연호 : 이전 형식의 체크포인트 데이터라면 런타임에 저장된 체크포인트 스냅샷을 사용합니다.
        DefenseStageRuntimeSession.RestoreCheckpointSnapshot();
    }


    // 2026.08.07_psb수정
    // 보스 씬의 Player 초기화가 끝날 때까지 기다린 뒤 위치와 체크포인트는 건드리지 않고 진행 정보만 이어받는다.
    private IEnumerator RestoreBossSceneProgressRoutine()
    {
        isRestoringBossSceneProgress = true;

        PlayerRuntimeReferences references = null;
        float elapsed = 0f;

        while (elapsed < playerInitializationTimeout)
        {
            if (TryFindInitializedPlayer(
                    out references))
            {
                break;
            }

            elapsed +=
                Time.unscaledDeltaTime;

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


    // 런타임 체크포인트가 없으면 설정에 따라 영구 저장 데이터를 런타임으로 불러옵니다.
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
    // 보스 진입 시 이어받아야 하는 체력, 목숨, 경험치, 스탯, 스킬 정보를 현재 Player에서 수집한다.
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
            CurrentHP =
                playerStatus.GetCurrentHP(),
            HealItemCount =
                healItemInventory.CurrentCount,
            LifeCount =
                lifeTracker.CurrentLifeCount,
            PlayerLevel =
                experienceTracker.CurrentLevel,
            CurrentExperience =
                experienceTracker.CurrentExp,
            PersistentStats =
                playerStatus.CapturePersistentStatSnapshot(),
            Skills =
                skillSystem != null
                    ? skillSystem.CaptureCheckpointSnapshot()
                    : System.Array.Empty<CheckpointSkillSnapshot>(),
            ClearedDefenseStageIds =
                DefenseStageRuntimeSession.CaptureCurrentState()
        };

        return true;
    }


    // PlayerInitializer 완료 여부를 필요한 Player 컴포넌트와 공개 상태 값을 통해 확인합니다.
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

        references =
            new PlayerRuntimeReferences(
                playerStatus,
                playerController,
                lifeTracker,
                healItemInventory,
                checkpointTracker,
                experienceTracker,
                skillSystem);

        return true;
    }


    // 새 Scene의 Player에 저장된 진행 정보와 현재 잔기 및 체크포인트 부활 위치를 적용합니다.
    private void RestoreLoadedPlayer(
        PlayerRuntimeReferences references,
        CheckpointRuntimeData data,
        int remainingLifeCount)
    {
        CheckpointProgressSnapshot progress =
            data.Progress;

        if (progress.IsValid)
        {
            RestorePlayerProgress(
                references,
                progress);
        }
        else
        {
            references.PlayerStatus.RestoreCurrentHP(
                data.SavedHP);

            DefenseStageRuntimeSession
                .RestoreCheckpointSnapshot();
        }

        // 2026.08.07_psb수정
        // 이번 사망으로 이미 차감된 현재 잔기를 유지한다.
        references.LifeTracker.RestoreCount(
            remainingLifeCount);

        references.HealItemInventory.RestoreCount(
            progress.IsValid
                ? progress.HealItemCount
                : data.SavedHealItemCount);

        references.CheckpointTracker.RestoreCheckpoint(
            data);

        references.PlayerController.TeleportTo(
            data.RespawnPosition);

        references.PlayerController.transform.rotation =
            data.RespawnRotation;
    }


    // 2026.08.07_psb수정
    // 체크포인트 부활과 보스 씬 진입이 동일한 플레이어 진행 데이터 복원 규칙을 사용하도록 공통 처리합니다.
    private void RestorePlayerProgress(
        PlayerRuntimeReferences references,
        CheckpointProgressSnapshot progress)
    {
        if (!progress.IsValid)
        {
            return;
        }

        references.SkillSystem?.RestoreCheckpointSnapshot(
            progress.Skills);

        references.PlayerStatus.ApplyPersistentStatSnapshot(
            progress.PersistentStats);

        references.ExperienceTracker.RestoreProgress(
            progress.PlayerLevel,
            progress.CurrentExperience);

        references.PlayerStatus.RestoreCurrentHP(
            progress.CurrentHP);

        references.LifeTracker.RestoreCount(
            progress.LifeCount);

        references.HealItemInventory.RestoreCount(
            progress.HealItemCount);

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


        // Scene 로드 후 찾은 Player 관련 컴포넌트 참조를 하나의 객체에 저장합니다.
        public PlayerRuntimeReferences(
            PlayerStatus playerStatus,
            PlayerController playerController,
            PlayerLifeTracker lifeTracker,
            PlayerHealItemInventory healItemInventory,
            PlayerCheckpointTracker checkpointTracker,
            PlayerExperienceTracker experienceTracker,
            SkillSystemController skillSystem)
        {
            PlayerStatus =
                playerStatus;

            PlayerController =
                playerController;

            LifeTracker =
                lifeTracker;

            HealItemInventory =
                healItemInventory;

            CheckpointTracker =
                checkpointTracker;

            ExperienceTracker =
                experienceTracker;

            SkillSystem =
                skillSystem;
        }
    }
}