using UnityEngine;
using UnityEngine.Pool;

public class ItemFactory : MonoBehaviour, IInitializable
{
    [Header("컨테이너")]
    [SerializeField] private Transform container;       // 아이템을 모와두는 컨테이너(이건 안해도 상관없음)
    [Header("프리팹")]
    [SerializeField] private GameObject prefab;         // 아이템 프리팹
    [Header("최대 개수")]
    [SerializeField] private int maxSize = 1000;        // 최대 개수(이것도 안해도 상관없음)

    private IObjectPool<GameObject> items;              // 아이템 오브젝트 풀링 주소
    private IObjectPool<GameObject> test;
    public IObjectPool<GameObject> Items => items;

    public int Priority => (int)InitOrder.Skill;        // 오브젝트 풀링 생성을 위한 초기화가 필요함

    // 테스트를 위한 (임시) 초기화
    private void Start() => Init();

    public void Init()
    {
        items = CustomObjectPool.CreatePool(prefab, maxSize, container);
        // 컨테이너 및 사이즈를 생략해도 상관없음(컨테이너를 생략하면 하이어라키에 그냥 생성되고, 사이즈는 1000으로 생성됨)
        test = CustomObjectPool.CreatePool(prefab);
    }

    // 아이템 가져오는 함수
    public Item GetItem()
    {
        // 바로 items.Get으로 쓰고, 각자의 스크립트에서 GetComponent로 클래스 형변환 하셔도 상관없음
        GameObject obj = items.Get();

        // 아이템 가져오기에 실패했다면
        if (obj == null)
            return null;
        // Item 스크립트가 없다면(여기서 각자의 클래스 T로 넣으시면 됨)
        else if (!obj.TryGetComponent<Item>(out var item))
            return null;
        // Item 스크립트가 있다면
        else
        {
            // 반납할 오브젝트 풀링 주소 설정
            item.SetPoolRef(items);
            // 아이템 반환
            return item;
        }
    }

    // 위의 아이템 가져오는 함수가 이해가 안된다면?
    // 이 아래가? 따지자면... 원본...?
    // 제네릭을 써서 만든 공용 함수였음... (근데 진짜 필요없어서... 위로 바꿈 ㅎ;;)
    // 객체의 특정 타입 반환 함수
    public T GetObject<T>() where T : Component
    {
        // 오브젝트 풀링 객체 가져오기
        var obj = items.Get();

        // 객체가 없다면
        if (obj == null)
            return null;
        // 객체에 인터페이스가 없다면
        else if (!obj.TryGetComponent<IPoolable>(out var poolable))
            return null;
        // 인터페이스가 있다면
        else
        {
            // 반납할 오브젝트 풀링 주소 설정
            poolable.SetPoolRef(items);
            // 객체의 특정 타입 반환
            return obj.GetComponent<T>();
        }
    }
}