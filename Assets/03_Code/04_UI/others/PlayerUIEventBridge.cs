using UnityEngine;

public class PlayerUIEventBridge : MonoBehaviour, IInitializable
{
    private bool isInitialized;
    private int currentLevel = 1;
    private float currentExp;
    private float requiredExp = 1f;
    private float currentHp = 1f;
    private float maxHp = 1f;
    private int currentLife;
    private int maxLife;
    private int currentHealItemCount;
    private int maxHealItemCount;
    private bool hasHealthState;
    private bool hasLifeState;
    private bool hasHealItemState;
    private bool hasExperienceState;

    private UIPauseSkillInfoData[] currentEquippedActiveSkills = System.Array.Empty<UIPauseSkillInfoData>();

    public int Priority => (int)InitOrder.PlayerUIBridge;

    public void Init()
    {
        if (isInitialized) return;

        SubscribeEvents();
        isInitialized = true;
    }

    private void OnDestroy()
    {
        if (!isInitialized) return;

        UnsubscribeEvents();
        isInitialized = false;
    }

    private void SubscribeEvents()
    {
        EventBus<PlayerHealthChangedEvent>.action += HandlePlayerHealthChanged;
        EventBus<PlayerLifeChangedEvent>.action += HandlePlayerLifeChanged;
        EventBus<PlayerHealItemCountChangedEvent>.action += HandlePlayerHealItemCountChanged;
        EventBus<PlayerExperienceChangedEvent>.action += HandlePlayerExperienceChanged;
        EventBus<PlayerDeadEvent>.action += HandlePlayerDead;
        EventBus<UIChangeScreenEvent>.action += HandleChangeScreen;
        EventBus<RefreshUIEventT>.action += HandleRefreshUI;
    }

    private void UnsubscribeEvents()
    {
        EventBus<PlayerHealthChangedEvent>.action -= HandlePlayerHealthChanged;
        EventBus<PlayerLifeChangedEvent>.action -= HandlePlayerLifeChanged;
        EventBus<PlayerHealItemCountChangedEvent>.action -= HandlePlayerHealItemCountChanged;
        EventBus<PlayerExperienceChangedEvent>.action -= HandlePlayerExperienceChanged;
        EventBus<PlayerDeadEvent>.action -= HandlePlayerDead;
        EventBus<UIChangeScreenEvent>.action -= HandleChangeScreen;
        EventBus<RefreshUIEventT>.action -= HandleRefreshUI;
    }

    private void HandlePlayerHealthChanged(PlayerHealthChangedEvent eventData)
    {
        currentHp = eventData.CurrentHP;
        maxHp = eventData.MaxHP;
        hasHealthState = true;

        EventBus<UISetPlayerHpEvent>.Publish(
            new UISetPlayerHpEvent(
                eventData.CurrentHP,
                eventData.MaxHP
            )
        );

        PublishPauseStatus();
    }

    private void HandlePlayerLifeChanged(PlayerLifeChangedEvent eventData)
    {
        currentLife = eventData.CurrentLifeCount;
        maxLife = eventData.MaxLifeCount;
        hasLifeState = true;

        EventBus<UISetPlayerLifeEvent>.Publish(
            new UISetPlayerLifeEvent(
                GetDisplayLifeCount(),
                GetDisplayMaxLifeCount()
            )
        );

        PublishPauseStatus();
    }

    private void HandlePlayerHealItemCountChanged(PlayerHealItemCountChangedEvent eventData)
    {
        currentHealItemCount = eventData.CurrentCount;
        maxHealItemCount = eventData.MaxCount;
        hasHealItemState = true;

        EventBus<UISetPlayerHealItemEvent>.Publish(
            new UISetPlayerHealItemEvent(
                eventData.CurrentCount,
                eventData.MaxCount
            )
        );
    }

    private void HandlePlayerExperienceChanged(PlayerExperienceChangedEvent eventData)
    {
        currentLevel = eventData.CurrentLevel;
        currentExp = eventData.CurrentExp;
        requiredExp = eventData.RequiredExp;
        hasExperienceState = true;

        EventBus<UISetPlayerLevelEvent>.Publish(
            new UISetPlayerLevelEvent(currentLevel));

        EventBus<UISetPlayerExpEvent>.Publish(
            new UISetPlayerExpEvent(currentExp, requiredExp));

        PublishPauseStatus();
    }

    private void HandlePlayerDead(PlayerDeadEvent eventData)
    {
        EventBus<UIChangeScreenEvent>.Publish(
            new UIChangeScreenEvent(UIScreenState.GameOver));
        EventBus<UISetGameOverEvent>.Publish(
            new UISetGameOverEvent(currentLife > 0));
    }

    private void HandleChangeScreen(UIChangeScreenEvent eventData)
    {
        if (eventData.ScreenState != UIScreenState.InGame)
            return;

        PublishCurrentPlayerHudState();
    }

    private void HandleRefreshUI(RefreshUIEventT eventData)
    {
        currentEquippedActiveSkills = eventData.EquippedActiveSkills ?? System.Array.Empty<UIPauseSkillInfoData>();

        PublishPauseStatus();
    }
    private void PublishCurrentPlayerHudState()
    {
        if (hasExperienceState)
        {
            EventBus<UISetPlayerLevelEvent>.Publish(
                new UISetPlayerLevelEvent(currentLevel));
            EventBus<UISetPlayerExpEvent>.Publish(
                new UISetPlayerExpEvent(currentExp, requiredExp));
        }

        if (hasHealthState)
        {
            EventBus<UISetPlayerHpEvent>.Publish(
                new UISetPlayerHpEvent(currentHp, maxHp));
        }

        if (hasLifeState)
        {
            EventBus<UISetPlayerLifeEvent>.Publish(
                new UISetPlayerLifeEvent(
                    GetDisplayLifeCount(),
                    GetDisplayMaxLifeCount()));
        }

        if (hasHealItemState)
        {
            EventBus<UISetPlayerHealItemEvent>.Publish(
                new UISetPlayerHealItemEvent(currentHealItemCount, maxHealItemCount));
        }

        PublishPauseStatus();
    }

    private void PublishPauseStatus()
    {
        EventBus<UISetPauseStatusEvent>.Publish(
            new UISetPauseStatusEvent(
                currentLevel,
                currentExp,
                requiredExp,
                currentHp,
                maxHp,
                GetDisplayLifeCount(),
                GetDisplayMaxLifeCount(),
                currentEquippedActiveSkills,
                System.Array.Empty<UIPauseSkillInfoData>()));
    }

    private int GetDisplayLifeCount()
    {
        return Mathf.Clamp(currentLife - 1, 0, GetDisplayMaxLifeCount());
    }

    private int GetDisplayMaxLifeCount()
    {
        return Mathf.Max(0, maxLife - 1);
    }
}
