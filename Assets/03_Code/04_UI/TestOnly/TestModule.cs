using UnityEngine;
using System.Linq;
using System.Collections.Generic;

public class TestModule : MonoBehaviour
{
    [Header("챕터 테스트")]
    [SerializeField] private int currentChapterId = 1;
    [SerializeField] private int highestClearedChapterId = 0;
    [SerializeField] private int maxChapterId = 3;

    [Header("플레이 테스트")]
    [SerializeField] private int playerLevel = 1;
    [SerializeField] private float currentExp = 0f;
    [SerializeField] private float requiredExp = 100f;
    [SerializeField] private float expGainAmount = 25;
    [SerializeField] private float currentHp = 100f;
    [SerializeField] private float maxHp = 100f;
    [SerializeField] private float hpDamageAmount = 25f;
    [SerializeField] private int currentLife = 2;
    [SerializeField] private int maxLife = 2;

    [Header("보스 테스트")]
    [SerializeField] private string bossName = "테스트 보스";
    [SerializeField] private bool bossSpawned;
    [SerializeField] private float bossCurrentHp = 100f;
    [SerializeField] private float bossMaxHp = 100f;
    [SerializeField] private float bossDamageAmount = 25f;
    [SerializeField] private string titleSceneName = "Lobby";
    [SerializeField] private bool canLoadCheckPoint = true;

    // 2026.08.07_psb수정
    [Header("게임오버 테스트 흐름")]
    [SerializeField] private bool usePrototypeGameOverFlow = true;

    private List<UIPauseSkillInfoData> canEnhanceSkillDatas = new();

    private BaseSkillData[] cachedActiveSkillDatas;
    private UIPauseSkillInfoData[] cachedEquippedActiveSkills;
    private UIPauseSkillInfoData[] cachedOwnedSkills;
    private int[] cachedOwnedSkillOrder;
    [SerializeField] private PlayerStatus playerStatus;
    [SerializeField] private PlayerExperienceTracker playerExperienceTracker;

    [SerializeField] private GameInputReader _input;

    [SerializeField] private SkillSystemController skillController;

    // 2026.08.10_UI 정리: 초기 데이터와 화면 상태를 설정한다.
    private void Start()
    {
        PrototypeGameSession.EnsureInitialized();

        currentChapterId = PrototypeGameSession.CurrentChapterId;
        highestClearedChapterId = PrototypeGameSession.HighestClearedChapterId;

        currentLife = maxLife;

        ApplyPrototypeSnapshot(PrototypeGameSession.GetChapterStart());
    }

    // 2026.08.10_UI 정리: 활성화 시 필요한 UI 상태와 이벤트 구독을 준비한다.
    private void OnEnable()
    {
        EventBus<RefreshUIEvent>.action += HandleRefreshUI;
        EventBus<UILevelUpSkillSelectedEvent>.action += HandleLevelUpSkillSelected;
        EventBus<PlayerLevelUpEvent>.action += HandlePlayerLevelUp;

        // 2026.08.07_psb수정
        // Real 씬에서는 CheckpointRespawnCoordinator만 복구와 재시작을 처리한다.
        if (usePrototypeGameOverFlow)
        {
            EventBus<UIGameOverRestartChapterRequestedEvent>.action += HandleGameOverRestartChapterRequested;
            EventBus<UIGameOverLoadCheckpointRequestedEvent>.action += HandleGameOverLoadCheckPointRequested;
        }
        EventBus<UIGameOverMainMenuRequestedEvent>.action += HandleGameOverMainMenuRequested;

        EventBus<UIChapterClearNextRequestedEvent>.action += HandleChapterClearNextRequested;
        EventBus<UIChapterClearMainMenuRequestedEvent>.action += HandleChapterClearMainMenuRequested;
        EventBus<UIChapterClearQuitGameRequestedEvent>.action += HandleChapterClearQuitRequested;
        EventBus<UIBossClearVideoFinishedEvent>.action += HandleBossClearVideoFinished;
    }

    // 2026.08.10_UI 정리: 비활성화 시 등록한 이벤트와 임시 UI 상태를 정리한다.
    private void OnDisable()
    {
        EventBus<RefreshUIEvent>.action -= HandleRefreshUI;
        EventBus<UILevelUpSkillSelectedEvent>.action -= HandleLevelUpSkillSelected;
        EventBus<PlayerLevelUpEvent>.action -= HandlePlayerLevelUp;

        // 2026.08.07_psb수정
        if (usePrototypeGameOverFlow)
        {
            EventBus<UIGameOverRestartChapterRequestedEvent>.action -= HandleGameOverRestartChapterRequested;
            EventBus<UIGameOverLoadCheckpointRequestedEvent>.action -= HandleGameOverLoadCheckPointRequested;
        }
        EventBus<UIGameOverMainMenuRequestedEvent>.action -= HandleGameOverMainMenuRequested;

        EventBus<UIChapterClearNextRequestedEvent>.action -= HandleChapterClearNextRequested;
        EventBus<UIChapterClearMainMenuRequestedEvent>.action -= HandleChapterClearMainMenuRequested;
        EventBus<UIChapterClearQuitGameRequestedEvent>.action -= HandleChapterClearQuitRequested;
        EventBus<UIBossClearVideoFinishedEvent>.action -= HandleBossClearVideoFinished;
    }

    // 2026.08.10_UI 정리: 프레임 단위 UI 상태와 입력을 갱신한다.
    private void Update()
    {
        if (_input.TestF1Pressed)
            CompleteCurrentChapter();

        if (_input.TestF2Pressed)
            GainExp();

        if (_input.TestF3Pressed)
            ShowLevelUp();

        if (_input.TestF4Pressed)
            DamagePlayer();

        if (usePrototypeGameOverFlow && _input.TestF5Pressed)
            ShowGameOver();

        if (_input.TestF6Pressed)
            EventBus<UIBossEncounterRequestedEvent>.Publish(
                new UIBossEncounterRequestedEvent());

        if (_input.TestF7Pressed)
            DamageBoss();

        if (_input.TestF8Pressed)
            KillBossAndClearChapter();

        if (_input.TestF9Pressed)
            ShowChapterSelect();

        if (_input.TestF10Pressed)
            ResetTestState();

        if (_input.TestF11Pressed)
            SaveCheckpoint();
    }

    // 2026.08.10_UI 정리: 현재 Prototype Snapshot 상태를 저장한다.
    private PrototypeProgressSnapshot CapturePrototypeSnapshot()
    {
        PlayerStatus status = GetPlayerStatus();
        List<PrototypeSkillState> skills = new();

        if (cachedEquippedActiveSkills != null)
        {
            for (int i = 0; i < cachedEquippedActiveSkills.Length; i++)
            {
                UIPauseSkillInfoData skill = cachedEquippedActiveSkills[i];

                if (skill.SkillId > (int)ACTIVE_SKILL_ID.Start)
                    skills.Add(new PrototypeSkillState(skill.SkillId, skill.Level, i));
            }
        }

        if (cachedOwnedSkills != null)
        {
            foreach (UIPauseSkillInfoData skill in cachedOwnedSkills)
            {
                if (skill.SkillId > (int)ACTIVE_SKILL_ID.Start && skill.IsUnlocked)
                    skills.Add(new PrototypeSkillState(skill.SkillId, skill.Level, -1));
            }
        }

        return new PrototypeProgressSnapshot
        {
            Level = playerLevel,
            CurrentExp = currentExp,
            RequiredExp = requiredExp,
            MaxHp = maxHp,
            PersistentStats = status != null
                ? status.CapturePersistentStatSnapshot()
                : default,
            Skills = skills.ToArray(),
            OwnedSkillOrder = cachedOwnedSkillOrder == null
                ? System.Array.Empty<int>()
                : cachedOwnedSkillOrder.ToArray()
        };
    }

    // 2026.08.10_UI 정리: 계산된 Prototype Snapshot 상태를 화면에 적용한다.
    private void ApplyPrototypeSnapshot(PrototypeProgressSnapshot snapshot)
    {
        if (snapshot == null) return;

        currentChapterId = PrototypeGameSession.CurrentChapterId;

        highestClearedChapterId = PrototypeGameSession.HighestClearedChapterId;

        playerLevel = Mathf.Max(1, snapshot.Level);
        currentExp = Mathf.Max(0f, snapshot.CurrentExp);
        requiredExp = Mathf.Max(1f, snapshot.RequiredExp);
        maxHp = Mathf.Max(1f, snapshot.MaxHp);
        currentHp = maxHp;

        PlayerStatus status = GetPlayerStatus();
        if (status != null)
            status.ApplyPersistentStatSnapshot(snapshot.PersistentStats);

        BaseSkillData[] skillDatas = LoadSkillDatas();

        UIPauseSkillInfoData[] equippedSkills = new UIPauseSkillInfoData[3];

        for (int i = 0; i < equippedSkills.Length; i++)
        {
            equippedSkills[i] = CreateEmptyPauseSkillData();
        }

        List<UIPauseSkillInfoData> ownedSkills = new();

        PrototypeSkillState[] skillStates = snapshot.Skills ?? System.Array.Empty<PrototypeSkillState>();

        foreach (PrototypeSkillState skillState in skillStates)
        {
            BaseSkillData skillData =
                skillDatas.FirstOrDefault(
                    data => data != null &&
                            data.Id == skillState.SkillId);

            if (skillData == null)
            {
                Debug.LogWarning(
                    $"SkillId {skillState.SkillId}에 해당하는 " +
                    "BaseSkillData를 찾지 못했습니다.");

                continue;
            }

            bool hasValidSlot =
                skillState.SlotIndex >= 0 &&
                skillState.SlotIndex < equippedSkills.Length;

            UIPauseSkillInfoData uiData =
                new UIPauseSkillInfoData(
                    skillData.Icon,
                    skillData.SkillName,
                    Mathf.Max(1, skillState.Level),
                    skillData.Desc,
                    hasValidSlot,
                    skillData.Id);

            if (hasValidSlot &&
                equippedSkills[skillState.SlotIndex].SkillId < (int)ACTIVE_SKILL_ID.Start + 1)
            {
                equippedSkills[skillState.SlotIndex] = uiData;
            }
            else
            {
                ownedSkills.Add(
                    new UIPauseSkillInfoData(
                        skillData.Icon,
                        skillData.SkillName,
                        Mathf.Max(1, skillState.Level),
                        skillData.Desc,
                        false,
                        skillData.Id));
            }
        }

        cachedEquippedActiveSkills = equippedSkills;
        cachedOwnedSkills = ownedSkills.ToArray();

        bossSpawned = false;
        bossCurrentHp = bossMaxHp;

        EventBus<TestRestoreSkillCheckpointEvent>.Publish(
            new TestRestoreSkillCheckpointEvent(
                CloneSkills(cachedEquippedActiveSkills),
                CloneSkills(cachedOwnedSkills),
                snapshot.OwnedSkillOrder));

        EventBus<UIChangeScreenEvent>.Publish(
            new UIChangeScreenEvent(UIScreenState.InGame));

    }

    // 2026.08.10_UI 정리: Current 챕터 UI 전환을 완료 처리한다.
    private void CompleteCurrentChapter()
    {
        int clearedChapterId = currentChapterId;

        PrototypeGameSession.CommitChapterClear(clearedChapterId, CapturePrototypeSnapshot());

        highestClearedChapterId = PrototypeGameSession.HighestClearedChapterId;
        
        bool hasNextChapter = highestClearedChapterId < maxChapterId;

        EventBus<UISetChapterProgressEvent>.Publish(
            new UISetChapterProgressEvent(highestClearedChapterId));

        EventBus<UISetChapterClearEvent>.Publish(
            new UISetChapterClearEvent(hasNextChapter));

        EventBus<UIChangeScreenEvent>.Publish(
            new UIChangeScreenEvent(UIScreenState.ChapterClear));
    }

    // 보스 클리어 영상이 끝난 뒤에만 기존 챕터 클리어 저장과 결과 화면을 실행합니다.
    private void HandleBossClearVideoFinished(UIBossClearVideoFinishedEvent eventData)
    {
        CompleteCurrentChapter();
    }

    // 2026.08.10_UI 정리: 이 메서드의 UI 처리 역할을 수행한다.
    private void GainExp()
    {
        PlayerExperienceTracker experienceTracker = GetPlayerExperienceTracker();
        if (experienceTracker == null) return;

        experienceTracker.AddExperience(expGainAmount);
    }

    // 2026.08.10_UI 정리: 레벨 Up UI 요소를 표시한다.
    private void ShowLevelUp()
    {
        PlayerExperienceTracker experienceTracker = GetPlayerExperienceTracker();
        if (experienceTracker == null) return;

        experienceTracker.AddExperience(
            Mathf.Max(0f, experienceTracker.RequiredExp - experienceTracker.CurrentExp));

        OpenLevelUpOverlay();
    }

    // 2026.08.10_UI 정리: 이 메서드의 UI 처리 역할을 수행한다.
    private void DamagePlayer()
    {
        PlayerStatus status = GetPlayerStatus();

        if (status == null)
        {
            Debug.LogWarning("PlayerStatus가 연결되지 않았습니다.", this);
            return;
        }

        status.TakeDamage(hpDamageAmount);
    }

    // 2026.08.10_UI 정리: 게임 Over UI 요소를 표시한다.
    private void ShowGameOver()
    {
        bool canUseCheckpoint =
            canLoadCheckPoint && PrototypeGameSession.HasCheckpoint;

        EventBus<UISetGameOverEvent>.Publish(
            new UISetGameOverEvent(
                canUseCheckpoint,
                currentLife > 0));

        EventBus<UIChangeScreenEvent>.Publish(
            new UIChangeScreenEvent(UIScreenState.GameOver));
    }

    // 2026.08.10_UI 정리: 이 메서드의 UI 처리 역할을 수행한다.
    private void SpawnBoss()
    {
        bossSpawned = true;
        bossCurrentHp = bossMaxHp;

        EventBus<UISetBossHudDataEvent>.Publish(
            new UISetBossHudDataEvent(bossName, bossCurrentHp, bossMaxHp));

        EventBus<UISetBossHudVisibleEvent>.Publish(
            new UISetBossHudVisibleEvent(true));
    }

    // 2026.08.10_UI 정리: 이 메서드의 UI 처리 역할을 수행한다.
    private void DamageBoss()
    {
        if (!bossSpawned)
            return;

        bossCurrentHp = Mathf.Max(0f, bossCurrentHp - bossDamageAmount);

        EventBus<UISetBossHudDataEvent>.Publish(
            new UISetBossHudDataEvent(bossName, bossCurrentHp, bossMaxHp));

        if (bossCurrentHp <= 0f)
        {
            KillBossAndClearChapter();
        }
    }

    // 2026.08.10_UI 정리: 이 메서드의 UI 처리 역할을 수행한다.
    private void KillBossAndClearChapter()
    {
        if(!bossSpawned)
        {
            bossSpawned = true;
        }

        bossCurrentHp = 0f;

        EventBus<UISetBossHudDataEvent>.Publish(
            new UISetBossHudDataEvent(bossName, bossCurrentHp, bossMaxHp));

        EventBus<UISetBossHudVisibleEvent>.Publish(
           new UISetBossHudVisibleEvent(false));

        bossSpawned = false;

        CompleteCurrentChapter();
    }

    // 2026.08.10_UI 정리: 챕터 Select UI 요소를 표시한다.
    private void ShowChapterSelect()
    {
        highestClearedChapterId = PrototypeGameSession.HighestClearedChapterId;

        EventBus<UIChangeScreenEvent>.Publish(
            new UIChangeScreenEvent(UIScreenState.ChapterSelect));

        EventBus<UISetChapterProgressEvent>.Publish(
            new UISetChapterProgressEvent(highestClearedChapterId));
    }

    // 2026.08.10_UI 정리: 이 메서드의 UI 처리 역할을 수행한다.
    private void SaveCheckpoint()
    {
        PrototypeGameSession.SaveCheckpoint(CapturePrototypeSnapshot());

        Debug.Log("저장 완료");
    }

    // 2026.08.10_UI 정리: 갱신 UI 관련 입력 또는 EventBus 요청을 처리한다.
    private void HandleRefreshUI(RefreshUIEvent eventData)
    {
        cachedEquippedActiveSkills = CloneSkills(eventData.EquippedSkills);
        cachedOwnedSkills = CloneSkills(eventData.OwnedSkills);
        cachedOwnedSkillOrder = eventData.OwnedSkillOrder;
    }

    // 2026.08.10_UI 정리: 플레이어 레벨 Up 관련 입력 또는 EventBus 요청을 처리한다.
    private void HandlePlayerLevelUp(PlayerLevelUpEvent eventData)
    {
        playerLevel = Mathf.Max(1, eventData.CurrentLevel);
        OpenLevelUpOverlay();
    }

    // 2026.08.10_UI 정리: 이 메서드의 UI 처리 역할을 수행한다.
    private void OpenLevelUpOverlay()
    {
        UILevelUpSkillOptionData[] options = CreateLevelUpOptionsFromRealSkills();

        EventBus<UIOpenOverlayEvent>.Publish(
            new UIOpenOverlayEvent(UIOverlayState.LevelUp));

        EventBus<UISetLevelUpOptionsEvent>.Publish(
            new UISetLevelUpOptionsEvent(options));
    }

    // 2026.08.10_UI 정리: 현재 플레이어 경험치 Tracker 값을 반환한다.
    private PlayerExperienceTracker GetPlayerExperienceTracker()
    {
        if (playerExperienceTracker == null)
            playerExperienceTracker = ServiceLocator.Get<PlayerExperienceTracker>();

        if (playerExperienceTracker != null)
            return playerExperienceTracker;

        Debug.LogWarning("PlayerExperienceTracker가 연결되지 않았습니다.", this);
        return null;
    }

    // 2026.08.10_UI 정리: 현재 플레이어 전체현황 값을 반환한다.
    private PlayerStatus GetPlayerStatus()
    {
        if (playerStatus == null)
            playerStatus = ServiceLocator.Get<PlayerStatus>();

        return playerStatus;
    }

    // 2026.08.10_UI 정리: 레벨 Up 스킬 선택 관련 입력 또는 EventBus 요청을 처리한다.
    private void HandleLevelUpSkillSelected(UILevelUpSkillSelectedEvent eventData)
    {
        EventBus<UICloseOverlayEvent>.Publish(
            new UICloseOverlayEvent(UIOverlayState.LevelUp));
    }

    // 2026.08.10_UI 정리: 게임 Over Restart 챕터 Requested 관련 입력 또는 EventBus 요청을 처리한다.
    private void HandleGameOverRestartChapterRequested(UIGameOverRestartChapterRequestedEvent eventData)
    {
        Time.timeScale = 1f;

        PrototypeGameSession.ClearCheckpoint();

        UnityEngine.SceneManagement.SceneManager.LoadScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
    }

    // 2026.08.10_UI 정리: 게임 Over Load Check Point Requested 관련 입력 또는 EventBus 요청을 처리한다.
    private void HandleGameOverLoadCheckPointRequested(UIGameOverLoadCheckpointRequestedEvent eventData)
    {
        PlayerStatus status = GetPlayerStatus();
        if (status == null)
        {
            EventBus<UIShowAlertPopupEvent>.Publish(
                new UIShowAlertPopupEvent(
                    UITextManager.Get("Common.NoticeTitle"),
                    UITextManager.Get("Test.NoSaveData")));

            return;
        }

        if (!status.TryReviveAtCheckpoint())
        {
            EventBus<UIShowAlertPopupEvent>.Publish(
                new UIShowAlertPopupEvent(
                    UITextManager.Get("Test.CannotReviveTitle"),
                    UITextManager.Get("Test.CannotReviveMessage")));

            return;
        }
        Time.timeScale = 1f;

        EventBus<UIChangeScreenEvent>.Publish(
            new UIChangeScreenEvent(UIScreenState.InGame));
    }

    // 2026.08.10_UI 정리: 게임 Over 메인 메뉴 Requested 관련 입력 또는 EventBus 요청을 처리한다.
    private void HandleGameOverMainMenuRequested(UIGameOverMainMenuRequestedEvent eventData)
    {
        Time.timeScale = 1f;

        PrototypeGameSession.ReturnToMainMenu();

        UnityEngine.SceneManagement.SceneManager.LoadScene(titleSceneName);
    }

    // 2026.08.10_UI 정리: 챕터 클리어 다음 Requested 관련 입력 또는 EventBus 요청을 처리한다.
    private void HandleChapterClearNextRequested(UIChapterClearNextRequestedEvent eventData)
    {
        EventBus<UIShowAlertPopupEvent>.Publish(
            new UIShowAlertPopupEvent(
                UITextManager.Get("Common.NoticeTitle"),
                UITextManager.Get("Test.PublicChapterEndMessage"),
                () =>
                {
                    EventBus<UIChapterClearMainMenuRequestedEvent>.Publish(
                        new UIChapterClearMainMenuRequestedEvent());
                }));
    }

    // 2026.08.10_UI 정리: 챕터 클리어 메인 메뉴 Requested 관련 입력 또는 EventBus 요청을 처리한다.
    private void HandleChapterClearMainMenuRequested(UIChapterClearMainMenuRequestedEvent eventData)
    {
        Time.timeScale = 1f;

        PrototypeGameSession.ReturnToMainMenu();

        UnityEngine.SceneManagement.SceneManager.LoadScene(titleSceneName);
    }

    // 2026.08.10_UI 정리: 챕터 클리어 종료 게임 Requested 관련 입력 또는 EventBus 요청을 처리한다.
    private void HandleChapterClearQuitGameRequested(UIChapterClearQuitGameRequestedEvent eventData)
    {
        Time.timeScale = 0f;

        Debug.Log("게임 종료됨");
    }

    // 2026.08.07_psb수정
    // 챕터 클리어 화면의 종료 요청을 현재 인게임 흐름에서 실제 게임 종료로 처리한다.
    private void HandleChapterClearQuitRequested(
        UIChapterClearQuitGameRequestedEvent eventData)
    {
        Time.timeScale = 1f;

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // 2026.08.10_UI 정리: Test 상태 상태를 기본값으로 초기화한다.
    private void ResetTestState()
    {
        Time.timeScale = 1f;

        PrototypeGameSession.ResetAll();

        currentChapterId = PrototypeGameSession.CurrentChapterId;

        highestClearedChapterId = PrototypeGameSession.HighestClearedChapterId;

        currentLife = maxLife;

        bossSpawned = false;
        bossCurrentHp = bossMaxHp;

        EventBus<UICloseOverlayEvent>.Publish(
            new UICloseOverlayEvent(UIOverlayState.LevelUp));

        EventBus<UISetBossHudVisibleEvent>.Publish(
            new UISetBossHudVisibleEvent(false));

        EventBus<UISetChapterProgressEvent>.Publish(
            new UISetChapterProgressEvent(highestClearedChapterId));

        ApplyPrototypeSnapshot(PrototypeGameSession.GetChapterStart());
    }

    // 2026.08.10_UI 정리: 스킬 Datas UI 표시용 데이터를 불러온다.
    private BaseSkillData[] LoadSkillDatas()
    {
        if (cachedActiveSkillDatas != null)
            return cachedActiveSkillDatas;

        List<BaseSkillData> results = new();
        SkillDatabase.FindDatasByChapter((CHAPTER_TYPE)(currentChapterId - 1), results);

        cachedActiveSkillDatas = results.ToArray();

        return cachedActiveSkillDatas;
    }

    // 2026.08.10_UI 정리: 필요한 레벨 Up 옵션 From Real 스킬 데이터를 생성한다.
    private UILevelUpSkillOptionData[] CreateLevelUpOptionsFromRealSkills()
    {
        const int maxSkillLevel = 3;

        skillController.Presenter.GetCanEnhanceSkillUIDatas(canEnhanceSkillDatas);

        List<UIPauseSkillInfoData> candidates = canEnhanceSkillDatas
            .GroupBy(skill => skill.SkillId)
            .Select(group => group.First())
            .OrderBy(_ => UnityEngine.Random.value)
            .Take(3)
            .ToList();

        UILevelUpSkillOptionData[] options = new UILevelUpSkillOptionData[candidates.Count];

        for (int i = 0; i < candidates.Count; i++)
        {
            UIPauseSkillInfoData skill = candidates[i];

            BaseSkillData skillData = LoadSkillDatas().FirstOrDefault(data => data != null && data.Id == skill.SkillId);

            string comparisonText = BuildLevelUpComparison(skillData, skill.Level, Mathf.Min(skill.Level + 1, skillData.MaxLevel));

            options[i] = new UILevelUpSkillOptionData(
                skill.SkillId,
                skill.Icon,
                skill.SkillName,
                skill.Level,
                Mathf.Min(skill.Level + 1, maxSkillLevel),
                comparisonText,
                string.Empty,
                false);
        }

        return options;
    }

    //참조 안함
    //private UILevelUpSkillOptionData[] CreateFallbackLevelUpOptionsFromResource()
    //{
    //    BaseSkillData[] skillDatas = LoadSkillDatas();
    //    int optionCount = Mathf.Min(3, skillDatas.Length);

    //    UILevelUpSkillOptionData[] options = new UILevelUpSkillOptionData[optionCount];

    //    for (int i = 0; i < optionCount; i++)
    //    {
    //        BaseSkillData data = skillDatas[i];

    //        options[i] = new UILevelUpSkillOptionData(
    //            data.Id,
    //            data.Icon,
    //            data.SkillName,
    //            Mathf.Max(1, playerLevel - 1),
    //            playerLevel,
    //            data.Desc,
    //            data.Desc,
    //            false);
    //    }

    //    return options;
    //}

    private UIPauseSkillInfoData CreateEmptyPauseSkillData()
    {
        return new UIPauseSkillInfoData(
            null,
            string.Empty,
            0,
            string.Empty,
            false,
            -1);
    }

    // 2026.08.10_UI 정리: 이 메서드의 UI 처리 역할을 수행한다.
    private UIPauseSkillInfoData[] CloneSkills(UIPauseSkillInfoData[] source)
    {
        return source == null
            ? System.Array.Empty<UIPauseSkillInfoData>()
            : source.ToArray();
    }

    // 2026.08.10_UI 정리:  사용할 데이터를 생성한다.
    private string BuildLevelUpComparison(BaseSkillData skillData, int currentLevel, int nextLevel)
    {
        if (skillData == null)
            return string.Empty;

        List<string> lines = new();

        switch(skillData.Type)
        {
            case SKILL_TYPE.Passive:
                SetPassiveSkillDataText(skillData.AsPassiveData, currentLevel, nextLevel, lines);
                return string.Join("\n", lines);
            // 액티브 스킬이라면
            case SKILL_TYPE.Active:
                SetActiveSkillDataText(skillData.AsActiveData, currentLevel, nextLevel, lines);
                return string.Join("\n", lines);
            // 그 외
            default:
                return string.Empty;
        }
    }

    // 2026.08.10_UI 정리: 패시브 스킬 데이터 텍스트 표시 값을 반영한다.
    private void SetPassiveSkillDataText(PassiveSkillData data, int curLevel, int nextLevel, List<string> results)
    {
        if (results == null)
            return;

        results.Clear();
        var curData = data.GetLevelData(curLevel).GetAppliedStats();
        var nextData = data.GetLevelData(nextLevel).GetAppliedStats();

        // 다음 레벨의 스탯의 수만큼
        for (int i = 0; i < nextData.Count; i++)
        {
            if (curData[i].modify == MODIFY_TYPE.Multiplier)
                // 스탯 종류, 변화량, 수식 종류
                AddComparisonLine(results, curData[i].stat.ToKoreanString(),
                                    FormatNumber(curData[i].amount) + curData[i].modify.ToKoreanString(),
                                        FormatNumber(nextData[i].amount) + nextData[i].modify.ToKoreanString());
            else
                // 스탯 종류, 변화량
                AddComparisonLine(results, curData[i].stat.ToKoreanString(),
                                            FormatNumber(curData[i].amount), FormatNumber(nextData[i].amount));
        }
    }

    // 2026.08.10_UI 정리: 액티브 스킬 데이터 텍스트 표시 값을 반영한다.
    private void SetActiveSkillDataText(ActiveSkillData data, int curLevel, int nextLevel, List<string> results)
    {
        if (results == null)
            return;

        results.Clear();
        AddComparisonLine(results, "데미지", FormatNumber(data.GetDamage(curLevel)),
                                                            FormatNumber(data.GetDamage(nextLevel)));
    }

    // 2026.08.10_UI 정리: 표시 목록에 Comparison Line 항목을 추가한다.
    private void AddComparisonLine(List<string> lines, string label, string currentValue, string nextValue)
    {
        if (currentValue == nextValue) return;

        Debug.Log(label);

        lines.Add($"{label}: {currentValue} -> {nextValue}");
    }

    // 2026.08.10_UI 정리: Number 값을 UI 문구 형식으로 변환한다.
    private string FormatNumber(float value)
    {
        return value.ToString("0.##");
    }

    // 2026.08.10_UI 정리: Seconds 값을 UI 문구 형식으로 변환한다.
    private string FormatSeconds(float value)
    {
        return $"{FormatNumber(value)}초";
    }
}

public struct TestRestoreSkillCheckpointEvent
{
    public UIPauseSkillInfoData[] EquippedSkills { get; private set; }
    public UIPauseSkillInfoData[] OwnedSkills { get; private set; }
    public int[] OwnedSkillOrder { get; private set;  }

    // 2026.08.10_UI 정리: 테스트 입력 이벤트 데이터를 초기화한다.
    public TestRestoreSkillCheckpointEvent(
        UIPauseSkillInfoData[] equippedSkills,
        UIPauseSkillInfoData[] ownedSkills,
        int[] ownedSkillOrder)
    {
        EquippedSkills = equippedSkills;
        OwnedSkills = ownedSkills;
        OwnedSkillOrder = ownedSkillOrder;

    }
}

public struct TestPlayerSkillUsedEvent
{
    public int SlotIndex { get; private set; }

    // 2026.08.10_UI 정리: 테스트 입력 이벤트 데이터를 초기화한다.
    public TestPlayerSkillUsedEvent(int slotIndex)
    {
        SlotIndex = slotIndex;
    }
}
