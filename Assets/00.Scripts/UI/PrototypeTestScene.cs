using UnityEngine;

public class PrototypeTestScene : MonoBehaviour
{
    private void Start()
    {
        EventBus<UIChangeScreenEvent>.Publish(
            new UIChangeScreenEvent(UIScreenState.InGame));

        EventBus<UISetPlayerLevelEvent>.Publish(
            new UISetPlayerLevelEvent(1));

        EventBus<UISetPlayerExpEvent>.Publish(
            new UISetPlayerExpEvent(0f, 100f));

        EventBus<UISetPlayerHpEvent>.Publish(
            new UISetPlayerHpEvent(100f, 100f));

        EventBus<UISetPlayerLifeEvent>.Publish(
            new UISetPlayerLifeEvent(3, 3));

        EventBus<UISetPlayerHealItemEvent>.Publish(
            new UISetPlayerHealItemEvent(3, 3));
    }
}
