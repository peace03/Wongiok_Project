using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

public class BulletFactory : MonoBehaviour, IInitializable
{
    [Header("총알 컨테이너")]
    [Tooltip("생성한 총알들을 모와두는 곳, 없으면 하이어라키에 바로 생성됨, 필요없으면 null로 두시면 됨")]
    [SerializeField] private Transform container;           // 총알 컨테이너
    [Header("총알 프리팹")]
    [Tooltip("오브젝트 풀링으로 생성할 오브젝트, 해당 오브젝트에는 IPoolable을 상속하는 스크립트가 컴포넌트로 있어야 함")]
    [SerializeField] private GameObject bulletPrefab;       // 총알 프리팹
    [Header("최대 총알 개수")]
    [Tooltip("오브젝트 풀링의 최대 크기, 1000보다 작게 해도 됨, 총알이라서 혹시 몰라 1000으로 잡아 둠")]
    [SerializeField] private int maxCount = 1000;           // 최대 총알 개수

    private IObjectPool<GameObject> bullets;                // 총알 오브젝트 풀

    public IObjectPool<GameObject> Bullets => bullets;

    public int Priority => (int)InitOrder.Skill;            // 중요도

    // 임시 초기화
    private void Awake() => Init();

    // 초기화 함수
    public void Init()
    {
        // 총알 프리팹이 없다면
        if(bulletPrefab == null)
        {
            Debug.Log($"[Error | Bullet] 총알 공장 생성 실패 => 입력 - 총알 프리팹 : 없음", this);
            return;
        }

        // 총알 오브젝트 풀 생성
        bullets = CustomObjectPool.CreatePool(bulletPrefab, maxCount, container);

        // 총알 스크립트가 있다면
        if(bulletPrefab.TryGetComponent<Bullet>(out var bullet))
        {
            // 타격/피격 이펙트들을 저장할 리스트
            List<EffectAddData> effects = new();

            // 타격/피격 이펙트들의 수만큼
            foreach (var effect in bullet.HitEffects)
                // 이펙트 프리팹이 있다면
                if (effect != null)
                    // 타격/피격 이펙트들 리스트에 추가
                    effects.Add(new EffectAddData(effect));

            // 추가할 타격/피격 이펙트들이 있다면
            if(effects.Count != 0)
                // 타격/피격 이펙트 추가하기
                EventBus<EffectAddDatas>.Publish(new EffectAddDatas(effects));
        }
    }

    /// <summary>
    /// 총알 가져오는 함수
    /// </summary>
    /// <param name="isVisible">총알 외형의 활성화 여부(생략 가능, 기본값 : 활성화)</param>
    public Bullet GetBullet(bool isVisible = true)
    {
        // 총알 오브젝트 풀에서 받아오기
        var prefab = bullets.Get();

        // 총알 프리팹이 없다면
        if(prefab == null)
        {
            Debug.Log($"[Error | Bullet] 총알 가져오기 실패 => 입력 - 총알 프리팹 : 없음", this);
            return null;
        }
        // 총알 스크립트가 없다면
        else if (!prefab.TryGetComponent<Bullet>(out var bullet))
        {
            Debug.Log($"[Error | Bullet] 총알 가져오기 실패 => 입력 - 총알 스크립트 없음", prefab);
            return null;
        }
        // 총알 스크립트가 있다면
        else
        {
            // 반납 주소 설정
            bullet.SetPoolRef(bullets);

            // 총알 외형을 비활성화 해야한다면
            if(!isVisible)
            {
                // 총알 외형을 그려주는 렌더러들 받아오기
                var renderers = bullet.GetComponentsInChildren<Renderer>();

                // 렌더러들이 있고 비어있지 않다면
                if (renderers != null && renderers.Length > 0)
                    // 렌더러들의 수만큼
                    foreach (var renderer in renderers)
                        // 렌더러 비활성화
                        renderer.enabled = false;
            }

            return bullet;
        }
    }
}