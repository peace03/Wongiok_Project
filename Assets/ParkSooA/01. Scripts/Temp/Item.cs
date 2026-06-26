using UnityEngine;
using UnityEngine.Pool;

public class Item : MonoBehaviour, IPoolable
{
    private int id;
    private string itemName;
    private string desc;
    private IObjectPool<GameObject> returnRef;      // 반납할 오브젝트 풀링 주소

    // 반납할 오브젝트 풀링 주소 설정 함수(인터페이스 함수)
    public void SetPoolRef(IObjectPool<GameObject> poolRef) => returnRef = poolRef;

    // 초기화 함수(정보 설정 함수)
    public void Init(int id, string name, string desc)
    {
        this.id = id;
        itemName = name;
        this.desc = desc;
    }

    // 아이템 사용 함수
    public void UseItem()
    {
        Debug.Log("아이템 사용");
    }
}