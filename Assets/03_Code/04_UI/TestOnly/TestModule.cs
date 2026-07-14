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

    private BaseSkillData[] cachedActiveSkillDatas;
    private UIPauseSkillInfoData[] cachedEquippedActiveSkills;
    private UIPauseSkillInfoData[] cachedOwnedSkills;
    [SerializeField] private PlayerStatus playerStatus;
    [SerializeField] private PlayerExperienceTracker playerExperienceTracker;

    private void Start()
    {
        PrototypeGameSession.EnsureInitialized();

        currentChapterId = PrototypeGameSession.CurrentChapterId;
        highestClearedChapterId = PrototypeGameSession.HighestClearedChapterId;

        currentLife = maxLife;

        ApplyPrototypeSnapshot(PrototypeGameSession.GetChapterStart());
    }

    private void OnEnable()
    {
        EventBus<RefreshUIEventT>.action += HandleRefreshUI;
        EventBus<UILevelUpSkillSelectedEvent>.action += HandleLevelUpSkillSelected;
        EventBus<PlayerLevelUpEvent>.action += HandlePlayerLevelUp;
        
        EventBus<UIGameOverRestartChapterRequestedEvent>.action += HandleGameOverRestartChapterRequested;
        EventBus<UIGameOverLoadCheckpointRequestedEvent>.action += HandleGameOverLoadCheckPointRequested;
        EventBus<UIGameOverMainMenuRequestedEvent>.action += HandleGameOverMainMenuRequested;

        EventBus<UIChapterClearNextRequestedEvent>.action += HandleChapterClearNextRequested;
        EventBus<UIChapterClearMainMenuRequestedEvent>.action += HandleChapterClearMainMenuRequested;
        EventBus<UIChapterClearQuitGameRequestedEvent>.action += HandleChapterClearQuitGameRequested;
    }

    private void OnDisable()
    {
        EventBus<RefreshUIEventT>.action -= HandleRefreshUI;
        EventBus<UILevelUpSkillSelectedEvent>.action -= HandleLevelUpSkillSelected;
        EventBus<PlayerLevelUpEvent>.action -= HandlePlayerLevelUp;
        
        EventBus<UIGameOverRestartChapterRequestedEvent>.action -= HandleGameOverRestartChapterRequested;
        EventBus<UIGameOverLoadCheckpointRequestedEvent>.action -= HandleGameOverLoadCheckPointRequested;
        EventBus<UIGameOverMainMenuRequestedEvent>.action -= HandleGameOverMainMenuRequested;

        EventBus<UIChapterClearNextRequestedEvent>.action -= HandleChapterClearNextRequested;
        EventBus<UIChapterClearMainMenuRequestedEvent>.action -= HandleChapterClearMainMenuRequested;
        EventBus<UIChapterClearQuitGameRequestedEvent>.action -= HandleChapterClearQuitGameRequested;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
            CompleteCurrentChapter();

        if (Input.GetKeyDown(KeyCode.Alpha2))
            GainExp();

        if (Input.GetKeyDown(KeyCode.Alpha3))
            ShowLevelUp();

        if (Input.GetKeyDown(KeyCode.Alpha4))
            DamagePlayer();

        if (Input.GetKeyDown(KeyCode.Alpha5))
            ShowGameOver();

        if (Input.GetKeyDown(KeyCode.Alpha6))
            SpawnBoss();

        if (Input.GetKeyDown(KeyCode.Alpha7))
            DamageBoss();

        if (Input.GetKeyDown(KeyCode.Alpha8))
            KillBossAndClearChapter();

        if (Input.GetKeyDown(KeyCode.Alpha9))
            ShowChapterSelect();

        if (Input.GetKeyDown(KeyCode.Alpha0))
            ResetTestState();

        if (Input.GetKeyDown(KeyCode.Period))
            SaveCheckpoint();

        if (Input.GetKeyDown(KeyCode.A))
            EventBus<TestPlayerSkillUsedEvent>.Publish(
                new TestPlayerSkillUsedEvent(0));

        if (Input.GetKeyDown(KeyCode.S))
            EventBus<TestPlayerSkillUsedEvent>.Publish(
                new TestPlayerSkillUsedEvent(1));

        if (Input.GetKeyDown(KeyCode.D))
            EventBus<TestPlayerSkillUsedEvent>.Publish(
                new TestPlayerSkillUsedEvent(2));
    }

    private PrototypeProgressSnapshot CapturePrototypeSnapshot()
    {
        PlayerStatus status = GetPlayerStatus();
        List<PrototypeSkillState> skills = new();

        if (cachedEquippedActiveSkills != null)
        {
            for (int i = 0; i < cachedEquippedActiveSkills.Length; i++)
            {
                UIPauseSkillInfoData skill = cachedEquippedActiveSkills[i];

                if (skill.SkillId >= 0)
                    skills.Add(new PrototypeSkillState(skill.SkillId, skill.Level, i));
            }
        }

        if (cachedOwnedSkills != null)
        {
            foreach (UIPauseSkillInfoData skill in cachedOwnedSkills)
            {
                if (skill.SkillId >= 0)
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
            Skills = skills.ToArray()
        };
    }

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
                equippedSkills[skillState.SlotIndex].SkillId < 0)
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
                CloneSkills(cachedOwnedSkills)));

        EventBus<UISetBossHudVisibleEvent>.Publish(
            new UISetBossHudVisibleEvent(false));

        EventBus<UIChangeScreenEvent>.Publish(
            new UIChangeScreenEvent(UIScreenState.InGame));

    }

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

    private void GainExp()
    {
        PlayerExperienceTracker experienceTracker = GetPlayerExperienceTracker();
        if (experienceTracker == null) return;

        experienceTracker.AddExperience(expGainAmount);
    }

    private void ShowLevelUp()
    {
        PlayerExperienceTracker experienceTracker = GetPlayerExperienceTracker();
        if (experienceTracker == null) return;

        experienceTracker.AddExperience(
            Mathf.Max(0f, experienceTracker.RequiredExp - experienceTracker.CurrentExp));

        OpenLevelUpOverlay();
    }

    //private void DamagePlayer()
    //{
    //    currentHp = Mathf.Max(0f, currentHp - hpDamageAmount);

    //    if (currentHp > 0f)
    //    {
    //        PublishPlayerState();
    //        return;
    //    }
    //    HandlePlayerDeath();
    //}
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

    private void ShowGameOver()
    {
        bool canUseCheckpoint =
            canLoadCheckPoint && PrototypeGameSession.HasCheckpoint;

        EventBus<UISetGameOverEvent>.Publish(
            new UISetGameOverEvent(canUseCheckpoint));

        EventBus<UIChangeScreenEvent>.Publish(
            new UIChangeScreenEvent(UIScreenState.GameOver));
    }

    private void SpawnBoss()
    {
        bossSpawned = true;
        bossCurrentHp = bossMaxHp;

        EventBus<UISetBossHudDataEvent>.Publish(
            new UISetBossHudDataEvent(bossName, bossCurrentHp, bossMaxHp));

        EventBus<UISetBossHudVisibleEvent>.Publish(
            new UISetBossHudVisibleEvent(true));
    }

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

    private void ShowChapterSelect()
    {
        highestClearedChapterId = PrototypeGameSession.HighestClearedChapterId;

        EventBus<UIChangeScreenEvent>.Publish(
            new UIChangeScreenEvent(UIScreenState.ChapterSelect));

        EventBus<UISetChapterProgressEvent>.Publish(
            new UISetChapterProgressEvent(highestClearedChapterId));
    }

    private void SaveCheckpoint()
    {
        PrototypeGameSession.SaveCheckpoint(CapturePrototypeSnapshot());

        Debug.Log("저장 완료");
    }

    private void HandlePlayerDeath()
    {
        ShowGameOver();
    }

    private void HandleRefreshUI(RefreshUIEventT eventData)
    {
        cachedEquippedActiveSkills = CloneSkills(eventData.EquippedActiveSkills);
        cachedOwnedSkills = CloneSkills(eventData.OwnedSkills);
    }

    private void HandlePlayerLevelUp(PlayerLevelUpEvent eventData)
    {
        playerLevel = Mathf.Max(1, eventData.CurrentLevel);
        OpenLevelUpOverlay();
    }

    private void OpenLevelUpOverlay()
    {
        UILevelUpSkillOptionData[] options = CreateLevelUpOptionsFromRealSkills();

        EventBus<UISetLevelUpOptionsEvent>.Publish(
            new UISetLevelUpOptionsEvent(options));

        EventBus<UIOpenOverlayEvent>.Publish(
            new UIOpenOverlayEvent(UIOverlayState.LevelUp));
    }

    private PlayerExperienceTracker GetPlayerExperienceTracker()
    {
        if (playerExperienceTracker == null)
            playerExperienceTracker = ServiceLocator.Get<PlayerExperienceTracker>();

        if (playerExperienceTracker != null)
            return playerExperienceTracker;

        Debug.LogWarning("PlayerExperienceTracker가 연결되지 않았습니다.", this);
        return null;
    }

    private PlayerStatus GetPlayerStatus()
    {
        if (playerStatus == null)
            playerStatus = ServiceLocator.Get<PlayerStatus>();

        return playerStatus;
    }

    private void HandleLevelUpSkillSelected(UILevelUpSkillSelectedEvent eventData)
    {
        EventBus<UICloseOverlayEvent>.Publish(
            new UICloseOverlayEvent(UIOverlayState.LevelUp));
    }

    private void HandleGameOverRestartChapterRequested(UIGameOverRestartChapterRequestedEvent eventData)
    {
        Time.timeScale = 1f;

        PrototypeGameSession.ClearCheckpoint();

        UnityEngine.SceneManagement.SceneManager.LoadScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
    }

    private void HandleGameOverLoadCheckPointRequested(UIGameOverLoadCheckpointRequestedEvent eventData)
    {
        PlayerStatus status = GetPlayerStatus();
        if (status == null)
        {
            EventBus<UIShowAlertPopupEvent>.Publish(
                new UIShowAlertPopupEvent(
                    "알림",
                    "저장된 데이터가 없습니다."));

            return;
        }

        if (!status.TryReviveAtCheckpoint())
        {
            EventBus<UIShowAlertPopupEvent>.Publish(
                new UIShowAlertPopupEvent(
                    "부활 불가",
                    "남은 목숨 모두 소진"));

            return;
        }
        Time.timeScale = 1f;

        EventBus<UIChangeScreenEvent>.Publish(
            new UIChangeScreenEvent(UIScreenState.InGame));
    }

    private void HandleGameOverMainMenuRequested(UIGameOverMainMenuRequestedEvent eventData)
    {
        Time.timeScale = 1f;

        PrototypeGameSession.ReturnToMainMenu();

        UnityEngine.SceneManagement.SceneManager.LoadScene(titleSceneName);
    }

    private void HandleChapterClearNextRequested(UIChapterClearNextRequestedEvent eventData)
    {
        EventBus<UIShowAlertPopupEvent>.Publish(
            new UIShowAlertPopupEvent(
                "알림",
                "플레이 해주셔서 감사합니다. \n현재 공개된 챕터는 여기까지입니다.",
                () =>
                {
                    EventBus<UIChapterClearMainMenuRequestedEvent>.Publish(
                        new UIChapterClearMainMenuRequestedEvent());
                }));
    }

    private void HandleChapterClearMainMenuRequested(UIChapterClearMainMenuRequestedEvent eventData)
    {
        Time.timeScale = 1f;

        PrototypeGameSession.ReturnToMainMenu();

        UnityEngine.SceneManagement.SceneManager.LoadScene(titleSceneName);
    }

    private void HandleChapterClearQuitGameRequested(UIChapterClearQuitGameRequestedEvent eventData)
    {
        Time.timeScale = 0f;

        Debug.Log("게임 종료됨");
    }

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

    private BaseSkillData[] LoadSkillDatas()
    {
        if (cachedActiveSkillDatas != null)
            return cachedActiveSkillDatas;

        cachedActiveSkillDatas = Resources.LoadAll<BaseSkillData>("Datas/Skills")
            .Where(data => data != null)
            .OrderBy(data => data.Id)
            .ToArray();

        return cachedActiveSkillDatas;
    }

    private UILevelUpSkillOptionData[] CreateLevelUpOptionsFromRealSkills()
    {
        const int maxSkillLevel = 3;

        IEnumerable<UIPauseSkillInfoData> allOwnedSkills =
            (cachedEquippedActiveSkills ?? System.Array.Empty<UIPauseSkillInfoData>())
            .Concat(cachedOwnedSkills ?? System.Array.Empty<UIPauseSkillInfoData>());

        List<UIPauseSkillInfoData> candidates = allOwnedSkills
            .Where(skill => skill.SkillId >= 0 && skill.Level < maxSkillLevel && skill.SkillId != 1004 && skill.SkillId != 1005)
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

            string comparisonText = BuildLevelUpComparison(skillData, skill.Level, Mathf.Min(skill.Level + 1, maxSkillLevel));

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

    private UIPauseSkillInfoData[] CloneSkills(UIPauseSkillInfoData[] source)
    {
        return source == null
            ? System.Array.Empty<UIPauseSkillInfoData>()
            : source.ToArray();
    }

    private string BuildLevelUpComparison(BaseSkillData skillData, int currentLevel, int nextLevel)
    {
        if (skillData is not ActiveSkillData activeSkillData) return string.Empty;

        if (activeSkillData.GetLevelData(currentLevel)
            is not ProjectileSkillLevelData currentData ||
            activeSkillData.GetLevelData(nextLevel)
            is not ProjectileSkillLevelData nextData)
        {
            return string.Empty;
        }

        List<string> lines = new();

        AddComparisonLine(lines, "쿨타임", FormatSeconds(currentData.MaxCoolTime), FormatSeconds(nextData.MaxCoolTime));

        AddComparisonLine(lines, "발사 횟수", Mathf.Max(1, currentData.ProjectileCount).ToString(), Mathf.Max(1, nextData.ProjectileCount).ToString());

        AddComparisonLine(lines, "차징 시간", FormatSeconds(currentData.MaxChargingTime), FormatSeconds(nextData.MaxChargingTime));

        return string.Join("\n", lines);
    }

    private void AddComparisonLine(List<string> lines, string label, string currentValue, string nextValue)
    {
        if (currentValue == nextValue) return;

        lines.Add($"{label}: {currentValue} -> {nextValue}");
    }

    private string FormatNumber(float value)
    {
        return value.ToString("0.##");
    }

    private string FormatSeconds(float value)
    {
        return $"{FormatNumber(value)}초";
    }

    private string FormatPenetration(int value)
    {
        return value < 0 ? "무한" : value.ToString();
    }
}

public struct TestRestoreSkillCheckpointEvent
{
    public UIPauseSkillInfoData[] EquippedSkills { get; private set; }
    public UIPauseSkillInfoData[] OwnedSkills { get; private set; }

    public TestRestoreSkillCheckpointEvent(
        UIPauseSkillInfoData[] equippedSkills,
        UIPauseSkillInfoData[] ownedSkills)
    {
        EquippedSkills = equippedSkills;
        OwnedSkills = ownedSkills;
    }
}

public struct TestPlayerSkillUsedEvent
{
    public int SlotIndex { get; private set; }

    public TestPlayerSkillUsedEvent(int slotIndex)
    {
        SlotIndex = slotIndex;
    }
}
