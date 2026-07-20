using UnityEngine;

[DisallowMultipleComponent]
public sealed class MonsterBulletLauncher : MonoBehaviour
{
    private const string EnemyLayerName = "Enemy";
    private const string BossLayerName = "Boss";

    [Header("References")]
    [SerializeField] private BulletFactory bulletFactory;

    private LayerMask ownerLayerMask;
    private int enemyLayer = -1;
    private int bossLayer = -1;

    private bool hasLoggedMissingLayer;
    private bool hasLoggedInvalidOwnerLayer;
    private bool hasLoggedMissingFactory;
    private bool hasLoggedUninitializedFactory;

    // 일반몹 탄환에 사용할 아군 LayerMask를 준비합니다
    private void Awake()
    {
        CacheOwnerLayerMask(true);
    }

    // 컴포넌트가 추가될 때 기본 LayerMask를 준비합니다
    private void Reset()
    {
        CacheOwnerLayerMask(false);
    }

    // Inspector 값이 변경될 때 Layer 설정을 다시 확인합니다
    private void OnValidate()
    {
        CacheOwnerLayerMask(false);
    }

    // 지정한 위치와 방향으로 공용 Bullet을 발사합니다
    public bool TryFire(
        Vector3 firePosition,
        Vector3 fireDirection,
        float damage,
        int penetrationCount)
    {
        if (!TryPrepareFire())
        {
            return false;
        }

        if (fireDirection.sqrMagnitude <= 0.0001f)
        {
            Debug.LogWarning(
                "탄환 발사 방향이 올바르지 않습니다.",
                this
            );

            return false;
        }

        Bullet bullet = bulletFactory.GetBullet();

        if (bullet == null)
        {
            return false;
        }

        Transform bulletTransform = bullet.transform;

        Quaternion fireRotation = Quaternion.FromToRotation(
            Vector3.forward,
            fireDirection.normalized
        );

        bulletTransform.SetPositionAndRotation(
            firePosition,
            fireRotation
        );

        bullet.StartFire(
            bulletTransform,
            ownerLayerMask,
            damage,
            penetrationCount
        );

        return true;
    }

    // 탄환을 발사하기 전에 Layer와 Factory 상태를 확인합니다
    private bool TryPrepareFire()
    {
        if (ownerLayerMask.value == 0 &&
            !CacheOwnerLayerMask(true))
        {
            return false;
        }

        if (gameObject.layer != enemyLayer)
        {
            if (!hasLoggedInvalidOwnerLayer)
            {
                Debug.LogWarning(
                    "MonsterBulletLauncher가 있는 일반몹 루트의 " +
                    "Layer를 Enemy로 설정해야 합니다.",
                    this
                );

                hasLoggedInvalidOwnerLayer = true;
            }

            return false;
        }

        return TryResolveBulletFactory();
    }

    // Enemy와 Boss Layer를 일반몹 탄환의 아군 마스크로 저장합니다
    private bool CacheOwnerLayerMask(bool logWarning)
    {
        enemyLayer = LayerMask.NameToLayer(
            EnemyLayerName
        );

        bossLayer = LayerMask.NameToLayer(
            BossLayerName
        );

        if (enemyLayer < 0 || bossLayer < 0)
        {
            ownerLayerMask = 0;

            if (logWarning &&
                !hasLoggedMissingLayer)
            {
                Debug.LogWarning(
                    "Enemy 또는 Boss Layer가 없습니다. " +
                    "Tags and Layers 설정을 확인해주세요.",
                    this
                );

                hasLoggedMissingLayer = true;
            }

            return false;
        }

        ownerLayerMask =
            (1 << enemyLayer) |
            (1 << bossLayer);

        hasLoggedMissingLayer = false;
        return true;
    }

    // Inspector 또는 ServiceLocator에서 공용 BulletFactory를 가져옵니다
    private bool TryResolveBulletFactory()
    {
        if (bulletFactory == null)
        {
            bulletFactory =
                ServiceLocator.Get<BulletFactory>();
        }

        if (bulletFactory == null)
        {
            if (!hasLoggedMissingFactory)
            {
                Debug.LogWarning(
                    "공용 BulletFactory를 찾을 수 없습니다.",
                    this
                );

                hasLoggedMissingFactory = true;
            }

            return false;
        }

        hasLoggedMissingFactory = false;

        if (bulletFactory.Bullets == null)
        {
            if (!hasLoggedUninitializedFactory)
            {
                Debug.LogWarning(
                    "BulletFactory의 오브젝트 풀이 " +
                    "아직 초기화되지 않았습니다.",
                    bulletFactory
                );

                hasLoggedUninitializedFactory = true;
            }

            return false;
        }

        hasLoggedUninitializedFactory = false;
        return true;
    }
}