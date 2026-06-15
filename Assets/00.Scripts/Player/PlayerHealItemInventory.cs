using UnityEngine;

public class PlayerHealItemInventory : MonoBehaviour
{
    [Header("Heal Item")]
    [SerializeField] private int startCount = 1;
    [SerializeField] private int maxCount = 3;
    [SerializeField] private float healAmount = 30f;
    [SerializeField] private float useDuration = 1f;

    private PlayerStatus playerStatus;
    private int currentCount;
    private bool isInitialized;

    public int CurrentCount => currentCount;
    public int MaxCount => maxCount;
    public float UseDuration => useDuration;

    public void Initialize(PlayerStatus status)
    {
        // PlayerInitializer에서 전달받은 스탯 참조를 저장하고 설정값을 안전한 범위로 보정합니다.
        playerStatus = status != null ? status : GetComponent<PlayerStatus>();
        maxCount = Mathf.Max(0, maxCount);
        healAmount = Mathf.Max(0f, healAmount);
        useDuration = Mathf.Max(0f, useDuration);
        currentCount = Mathf.Clamp(startCount, 0, maxCount);
        isInitialized = true;
    }

    public void PublishInitialCount()
    {
        // UI가 시작 보유량을 받을 수 있도록 초기화 마지막에 보유량 이벤트를 발행합니다.
        PublishCountChanged();
    }

    public bool CanStartUse()
    {
        EnsureInitialized();

        // 아이템이 없거나 플레이어 스탯을 찾지 못하면 사용을 시작할 수 없습니다.
        if (currentCount <= 0) return false;
        if (playerStatus == null) return false;

        // 죽은 상태에서는 회복 아이템 사용을 막습니다.
        if (playerStatus.GetCurrentHP() <= 0f) return false;

        // 최대 체력 상태에서는 효과가 없으므로 아이템 사용을 시작하지 않습니다.
        if (playerStatus.GetCurrentHP() >= playerStatus.GetMaxHP()) return false;

        return true;
    }

    public bool TryAdd(int amount = 1)
    {
        EnsureInitialized();

        // 최대 보유량에 도달했다면 픽업을 소비하지 않습니다.
        if (currentCount >= maxCount) return false;

        int nextCount = Mathf.Clamp(currentCount + Mathf.Max(0, amount), 0, maxCount);
        if (nextCount == currentCount) return false;

        currentCount = nextCount;
        PublishCountChanged();
        return true;
    }

    public bool TryCompleteUse()
    {
        EnsureInitialized();

        // 사용 완료 시점에 다시 조건을 확인해 만피/중복 소모를 막습니다.
        if (!CanStartUse()) return false;

        currentCount--;
        playerStatus.Heal(healAmount);
        PublishCountChanged();
        return true;
    }

    private void EnsureInitialized()
    {
        if (isInitialized) return;

        // 예외적으로 PlayerInitializer 전에 접근되어도 테스트가 깨지지 않도록 보강합니다.
        Initialize(GetComponent<PlayerStatus>());
    }

    private void PublishCountChanged()
    {
        EventBus<PlayerHealItemCountChangedEvent>.Publish(
            new PlayerHealItemCountChangedEvent(
                gameObject,
                currentCount,
                maxCount
            )
        );
    }
}
