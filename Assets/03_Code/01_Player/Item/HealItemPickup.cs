using UnityEngine;

[RequireComponent(typeof(Collider))]
public class HealItemPickup : MonoBehaviour
{
    [SerializeField] private int addCount = 1;

    private void Reset()
    {
        // 회복 아이템은 플레이어가 닿았을 때 획득되도록 Trigger Collider를 기본값으로 둡니다.
        Collider itemCollider = GetComponent<Collider>();
        if (itemCollider != null)
            itemCollider.isTrigger = true;
    }

    private void Awake()
    {
        // Trigger가 꺼져 있으면 배치 실수를 빠르게 확인할 수 있게 경고만 남깁니다.
        Collider itemCollider = GetComponent<Collider>();
        if (itemCollider != null && !itemCollider.isTrigger)
            Debug.LogWarning($"{name} 회복 아이템 Collider의 Is Trigger가 꺼져 있습니다.");
    }

    private void OnTriggerEnter(Collider other)
    {
        PlayerHealItemInventory inventory = other.GetComponentInParent<PlayerHealItemInventory>();
        if (inventory == null) return;

        // 보유량이 가득 찬 경우에는 아이템을 남겨두고, 획득 성공 시에만 제거합니다.
        if (!inventory.TryAdd(addCount)) return;

        Destroy(gameObject);
    }
}
