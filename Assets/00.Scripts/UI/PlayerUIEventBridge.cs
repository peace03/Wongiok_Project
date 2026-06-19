using UnityEngine;

public class PlayerUIEventBridge : MonoBehaviour, IInitializable
{
    private bool isInitialized;

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
    }

    private void UnsubscribeEvents()
    {
        EventBus<PlayerHealthChangedEvent>.action -= HandlePlayerHealthChanged;
        EventBus<PlayerLifeChangedEvent>.action -= HandlePlayerLifeChanged;
        EventBus<PlayerHealItemCountChangedEvent>.action -= HandlePlayerHealItemCountChanged;
    }

    private void HandlePlayerHealthChanged(PlayerHealthChangedEvent eventData)
    {
        EventBus<UISetPlayerHpEvent>.Publish(
            new UISetPlayerHpEvent(
                eventData.CurrentHP,
                eventData.MaxHP
            )
        );
    }

    private void HandlePlayerLifeChanged(PlayerLifeChangedEvent eventData)
    {
        EventBus<UISetPlayerLifeEvent>.Publish(
            new UISetPlayerLifeEvent(
                eventData.CurrentLifeCount,
                eventData.MaxLifeCount
            )
        );
    }

    private void HandlePlayerHealItemCountChanged(PlayerHealItemCountChangedEvent eventData)
    {
        EventBus<UISetPlayerHealItemEvent>.Publish(
            new UISetPlayerHealItemEvent(
                eventData.CurrentCount,
                eventData.MaxCount
            )
        );
    }
}
