using UnityEngine;
using UnityEngine.Pool;

public class BulletFactory : MonoBehaviour, IInitializable
{
    [Header("총알 컨테이너")]
    [Tooltip("생성한 총알들을 모와두는 곳, 없으면 하이어라키에 바로 생성됨, 필요없으면 null로 두시면 됨")]
    [SerializeField] private Transform container;       // 총알 컨테이너
    [Header("총알 프리팹")]
    [Tooltip("오브젝트 풀링으로 생성할 오브젝트, 해당 오브젝트에는 IPoolable을 상속하는 스크립트가 컴포넌트로 있어야 함")]
    [SerializeField] private GameObject bullet;         // 총알 프리팹
    [Header("최대 총알 개수")]
    [Tooltip("오브젝트 풀링의 최대 크기, 1000보다 작게 해도 됨, 총알이라서 혹시 몰라 1000으로 잡아 둠")]
    [SerializeField] private int maxCount = 1000;       // 최대 총알 개수

    private IObjectPool<GameObject> bullets;            // 총알 오브젝트 풀
    public IObjectPool<GameObject> Bullets => bullets;

    public int Priority => (int)InitOrder.Skill;        // 중요도

    // 임시 초기화
    private void Awake() => Init();

    // 초기화 함수
    public void Init()
    {
        // 총알 프리팹이 없다면
        if(bullet == null)
        {
            Debug.LogError($"[Error | Bullet] 총알 공장 생성 실패 => 입력 - 총알 프리팹 : 없음", this);
            return;
        }

        // 총알 오브젝트 풀 생성
        bullets = CustomObjectPool.CreatePool(bullet, maxCount, container);
    }

    /// <summary>
    /// 총알 가져오는 함수
    /// </summary>
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