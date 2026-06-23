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
    public Bullet GetBullet()
    {
        // 총알 오브젝트 풀에서 받아오기
        var obj = bullets.Get();

        // 총알 프리팹이 없다면
        if(obj == null)
        {
            Debug.LogError($"[Error | Bullet] 총알 가져오기 실패 => 입력 - 총알 프리팹 : 없음", this);
            return null;
        }
        // 총알 스크립트가 없다면
        else if (!obj.TryGetComponent<Bullet>(out var bullet))
        {
            Debug.LogError($"[Error | Bullet] 총알 가져오기 실패 => 입력 - 총알 스크립트 없음", obj);
            return null;
        }
        // 총알 스크립트가 있다면
        else
        {
            // 반납 주소 설정
            bullet.SetPoolRef(bullets);
            return bullet;
        }
    }
}