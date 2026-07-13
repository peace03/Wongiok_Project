using UnityEngine;

public class Inventory : MonoBehaviour
{
    [Header("아이템 공장")]
    [SerializeField] private ItemFactory itemFactory;       // 아이템 공장

    private void Start()
    {
        var item = itemFactory.GetItem();
        item.Init(101, "사과", "빨간 사과~");
        item.UseItem();
    }
}