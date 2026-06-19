using UnityEngine;
using UnityEngine.Pool;

public class BulletFactory : MonoBehaviour, IInitializable
{
    [Header("총알 컨테이너(모와두는 곳)")]
    [SerializeField] private Transform container;       // 총알 컨테이너
    [Header("총알 프리팹")]
    [SerializeField] private GameObject bullet;         // 총알 프리팹
    [Header("최대 총알 개수")]
    [SerializeField] private int maxCount = 1000;       // 최대 총알 개수

    private IObjectPool<GameObject> bullets;            // 총알 오브젝트 풀
    public IObjectPool<GameObject> Bullets => bullets;

    public int Priority => (int)InitOrder.Skill;        // 중요도

    // 임시 초기화
    private void Start() => Init();

    // 초기화 함수(총알 오브젝트 풀 생성)
    public void Init() => bullets = CustomObjectPool.CreatePool(bullet, maxCount, container);

    // 총알 가져오는 함수
    public void GetBullet(LayerMask owner, float damage, int count = 1)
    {
        // 총알 가져오기
        var obj = bullets.Get();

        // 반납 주소 설정자가 없다면
        if (!obj.TryGetComponent<IPoolable>(out var setter))
        {
            Debug.LogError($"[Error | Bullet] 반납 주소 설정 실패 => 입력 - 오브젝트 풀 인터페이스 : 없음", obj);
            return;
        }

        // 반납 주소 설정
        setter.SetPoolRef(bullets);

        // 총알 스크립트가 없다면
        if(!obj.TryGetComponent<Bullet>(out var bullet))
        {
            Debug.LogError($"[Error | Bullet] 총알 정보 설정 실패 => 입력 - 총알 스크립트 : 없음", obj);
            return;
        }

        // 총알 정보 설정
        bullet.SetInfo(owner, damage, count);
    }
}