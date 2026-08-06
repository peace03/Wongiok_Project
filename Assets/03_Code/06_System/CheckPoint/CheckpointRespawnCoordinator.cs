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
    }

    // 체크포인트 복귀 코루틴을 중복 없이 시작합니다.
    private void HandleLoadCheckpointRequested(
        UIGameOverLoadCheckpointRequestedEvent eventData)
    {
        if (isRespawning)
        {
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
        PrototypeGameSession.ClearCheckpoint();

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

        int remainingLifeCount = data.Progress.IsValid
            ? data.Progress.LifeCount
            : currentLifeTracker.CurrentLifeCount;

        if (remainingLifeCount <= 0 && !data.Progress.IsValid)
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
            references.SkillSystem?.RestoreCheckpointSnapshot(
                progress.Skills);
            references.PlayerStatus.ApplyPersistentStatSnapshot(
                progress.PersistentStats);
            references.ExperienceTracker.RestoreProgress(
                progress.PlayerLevel,
                progress.CurrentExperience);
            references.PlayerStatus.RestoreCurrentHP(
                progress.CurrentHP);
            DefenseStageRuntimeSession.RestoreCheckpointSnapshot(
                progress.ClearedDefenseStageIds);
        }
        else
        {
            references.PlayerStatus.RestoreCurrentHP(data.SavedHP);
            DefenseStageRuntimeSession.RestoreCheckpointSnapshot();
        }

        references.LifeTracker.RestoreCount(
            Mathf.Max(1, remainingLifeCount));

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
