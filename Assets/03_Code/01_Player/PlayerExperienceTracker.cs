using UnityEngine;

public class PlayerExperienceTracker : MonoBehaviour
{
    [Header("Experience")]
    // 플레이어가 시작할 레벨입니다.
    [SerializeField] private int startLevel = 1;

    // 레벨업에 필요한 고정 경험치입니다.
    [SerializeField] private float expPerLevel = 100f;

    // 현재 레벨에서 보유 중인 경험치입니다.
    [SerializeField] private float currentExp = 0f;

    private int currentLevel;
    private bool isInitialized;

    public int CurrentLevel => currentLevel;
    public float CurrentExp => currentExp;
    public float RequiredExp => Mathf.Max(1f, expPerLevel);

    private void OnEnable()
    {
        // 몬스터 사망 알림을 받아 경험치 보상을 처리합니다.
        EventBus<MonsterDeadEvent>.action += OnMonsterDead;
    }

    private void OnDisable()
    {
        // 오브젝트 비활성화 시 중복 구독을 방지합니다.
        EventBus<MonsterDeadEvent>.action -= OnMonsterDead;
    }

    public void Initialize()
    {
        // PlayerInitializer 흐름에서 시작 레벨과 경험치를 확정합니다.
        currentLevel = Mathf.Max(1, startLevel);
        currentExp = Mathf.Max(0f, currentExp);
        isInitialized = true;
    }

    public void PublishInitialExperience()
    {
        // UI나 스킬 선택 시스템이 현재 성장 상태를 받을 수 있게 초기 이벤트를 발행합니다.
        PublishExperienceChanged();
    }

    public void AddExperience(float amount)
    {
        // 0 이하 보상은 성장 상태를 바꾸지 않습니다.
        if (amount <= 0f) return;

        EnsureInitialized();

        currentExp += amount;

        while (currentExp >= RequiredExp)
        {
            currentExp -= RequiredExp;
            LevelUp();
        }

        PublishExperienceChanged();
    }

    public void RestoreProgress(int level, float experience)
    {
        EnsureInitialized();

        currentLevel = Mathf.Max(1, level);
        currentExp = Mathf.Clamp(experience, 0f, RequiredExp);
        PublishExperienceChanged();
    }

    private void OnMonsterDead(MonsterDeadEvent eventData)
    {
        if (eventData.MonsterObject == null) return;

        MonsterExpReward reward = eventData.MonsterObject.GetComponent<MonsterExpReward>();
        if (reward == null) return;

        AddExperience(reward.ExpReward);
    }

    private void LevelUp()
    {
        int previousLevel = currentLevel;
        currentLevel++;

        // 스킬 선택 UI는 이 이벤트를 구독해서 나중에 진입합니다.
        EventBus<PlayerLevelUpEvent>.Publish(
            new PlayerLevelUpEvent(
                gameObject,
                previousLevel,
                currentLevel
            )
        );
    }

    private void PublishExperienceChanged()
    {
        EventBus<PlayerExperienceChangedEvent>.Publish(
            new PlayerExperienceChangedEvent(
                gameObject,
                currentLevel,
                currentExp,
                RequiredExp
            )
        );
    }

    private void EnsureInitialized()
    {
        if (isInitialized) return;

        Initialize();
    }
}
