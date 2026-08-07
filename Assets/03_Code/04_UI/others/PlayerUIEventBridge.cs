using System.Collections;
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
    private Coroutine initialStateSyncRoutine;

    private UIPauseSkillInfoData[] currentEquippedActiveSkills = System.Array.Empty<UIPauseSkillInfoData>();
    private UIPauseSkillInfoData[] currentEquippedPassiveSkills = System.Array.Empty<UIPauseSkillInfoData>();

    public int Priority => (int)InitOrder.PlayerUIBridge;

    public void Init()
    {
        if (isInitialized) return;

        SubscribeEvents();
        isInitialized = true;
        RequestInitialPlayerStateSync();
    }

    private void OnDestroy()
    {
        if (!isInitialized) return;

        if (initialStateSyncRoutine != null)
            StopCoroutine(initialStateSyncRoutine);

        UnsubscribeEvents();
        isInitialized = false;
    }

    private void SubscribeEvents()
    {
        EventBus<PlayerHealthChangedEvent>.action += HandlePlayerHealthChanged;
        EventBus<PlayerLifeChangedEvent>.action += HandlePlayerLifeChanged;
        EventBus<PlayerHealItemCountChangedEvent>.action += HandlePlayerHealItemCountChanged;
        EventBus<PlayerExperienceChangedEvent>.action += HandlePlayerExperienceChanged;
        EventBus<PlayerDeathPresentationFinishedEvent>.action +=
            HandlePlayerDeathPresentationFinished;
        EventBus<UIChangeScreenEvent>.action += HandleChangeScreen;
        EventBus<RefreshUIEvent>.action += HandleRefreshUI;
    }

    private void UnsubscribeEvents()
    {
        EventBus<PlayerHealthChangedEvent>.action -= HandlePlayerHealthChanged;
        EventBus<PlayerLifeChangedEvent>.action -= HandlePlayerLifeChanged;
        EventBus<PlayerHealItemCountChangedEvent>.action -= HandlePlayerHealItemCountChanged;
        EventBus<PlayerExperienceChangedEvent>.action -= HandlePlayerExperienceChanged;
        EventBus<PlayerDeathPresentationFinishedEvent>.action -=
            HandlePlayerDeathPresentationFinished;
        EventBus<UIChangeScreenEvent>.action -= HandleChangeScreen;
        EventBus<RefreshUIEvent>.action -= HandleRefreshUI;
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

    private void HandlePlayerDeathPresentationFinished(
        PlayerDeathPresentationFinishedEvent eventData)
    {
        // 2026.08.07_psb수정
        // 비활성 상태의 GameOverView가 상태 이벤트를 놓치지 않도록 화면을 먼저 연다.
        EventBus<UIChangeScreenEvent>.Publish(
            new UIChangeScreenEvent(UIScreenState.GameOver));

        EventBus<UISetGameOverEvent>.Publish(
            new UISetGameOverEvent(
                CheckpointRuntimeSession.HasActiveCheckpoint,
                currentLife > 0));
    }

    private void HandleChangeScreen(UIChangeScreenEvent eventData)
    {
        if (eventData.ScreenState != UIScreenState.InGame)
            return;

        RequestInitialPlayerStateSync();
    }

    private void HandleRefreshUI(RefreshUIEvent eventData)
    {
        if(eventData.IsActiveSkill)
            currentEquippedActiveSkills = eventData.EquippedSkills ??
                                                            System.Array.Empty<UIPauseSkillInfoData>();
        else
            currentEquippedPassiveSkills = eventData.EquippedSkills ??
                                                            System.Array.Empty<UIPauseSkillInfoData>();

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
                currentEquippedPassiveSkills));
    }

    // 2026.08.07_psb수정
    // 인게임 진입 직후 실제 플레이어 컴포넌트를 읽어 상태 변경 전에도 Pause 정보를 만든다.
    private void RequestInitialPlayerStateSync()
    {
        if (initialStateSyncRoutine != null)
            StopCoroutine(initialStateSyncRoutine);

        initialStateSyncRoutine = StartCoroutine(SyncInitialPlayerState());
    }

    // 2026.08.07_psb수정
    // 플레이어 재생성 순서를 기다린 뒤 HP·경험치·목숨·회복 아이템의 현재 값을 한 번 발행한다.
    private IEnumerator SyncInitialPlayerState()
    {
        const int maxRetryFrameCount = 30;

        for (int frame = 0; frame < maxRetryFrameCount; frame++)
        {
            PlayerStatus playerStatus = FindFirstObjectByType<PlayerStatus>();
            PlayerExperienceTracker experienceTracker =
                FindFirstObjectByType<PlayerExperienceTracker>();
            PlayerLifeTracker lifeTracker =
                FindFirstObjectByType<PlayerLifeTracker>();
            PlayerHealItemInventory healItemInventory =
                FindFirstObjectByType<PlayerHealItemInventory>();

            if (playerStatus != null &&
                experienceTracker != null &&
                lifeTracker != null &&
                healItemInventory != null)
            {
                currentHp = playerStatus.GetCurrentHP();
                maxHp = playerStatus.GetMaxHP();
                hasHealthState = true;

                currentLevel = experienceTracker.CurrentLevel;
                currentExp = experienceTracker.CurrentExp;
                requiredExp = experienceTracker.RequiredExp;
                hasExperienceState = true;

                currentLife = lifeTracker.CurrentLifeCount;
                maxLife = lifeTracker.StartLifeCount;
                hasLifeState = true;

                currentHealItemCount = healItemInventory.CurrentCount;
                maxHealItemCount = healItemInventory.MaxCount;
                hasHealItemState = true;

                PublishCurrentPlayerHudState();
                initialStateSyncRoutine = null;
                yield break;
            }

            yield return null;
        }

        // 플레이어가 없는 특수 씬에서는 기존에 수신한 값만 다시 발행한다.
        PublishCurrentPlayerHudState();
        initialStateSyncRoutine = null;
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
