using UnityEngine;

public class PlayerLifeTracker : MonoBehaviour
{
    [Header("Life")]
    [SerializeField] private int startLifeCount = 3;

    private int currentLifeCount;
    private bool isInitialized;
    private bool isLifeDepletedPublished;

    public int CurrentLifeCount => currentLifeCount;
    public int StartLifeCount => startLifeCount;
    public bool HasRemainingLife
    {
        get
        {
            EnsureInitialized();
            return currentLifeCount > 0;
        }
    }

    public void Initialize()
    {
        if (isInitialized) return;

        // 시작 목숨은 음수가 되지 않도록 보정하고 현재 목숨으로 복사합니다.
        startLifeCount = Mathf.Max(0, startLifeCount);
        currentLifeCount = startLifeCount;
        isLifeDepletedPublished = currentLifeCount == 0;
        isInitialized = true;
    }

    public void PublishInitialLife()
    {
        // UI가 시작 목숨을 받을 수 있도록 초기화 마지막에 목숨 이벤트를 발행합니다.
        PublishLifeChanged();
    }

    public bool ConsumeLifeOnDeath()
    {
        EnsureInitialized();

        // 목숨은 0 아래로 내려가지 않으며, 실제 감소가 없으면 변경 이벤트도 내지 않습니다.
        if (currentLifeCount <= 0) return false;

        currentLifeCount--;
        PublishLifeChanged();

        if (currentLifeCount == 0 && !isLifeDepletedPublished)
        {
            isLifeDepletedPublished = true;
            EventBus<PlayerLifeDepletedEvent>.Publish(new PlayerLifeDepletedEvent(gameObject));
        }

        return true;
    }

    private void EnsureInitialized()
    {
        if (isInitialized) return;

        Initialize();
    }

    private void PublishLifeChanged()
    {
        EventBus<PlayerLifeChangedEvent>.Publish(
            new PlayerLifeChangedEvent(
                gameObject,
                currentLifeCount,
                startLifeCount
            )
        );
    }
}
