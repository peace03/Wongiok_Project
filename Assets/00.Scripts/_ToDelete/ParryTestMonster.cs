using UnityEngine;

// 패링 테스트용 원거리 몬스터입니다.
// 일정 주기로 플레이어를 향해 투사체만 발사하며, 돌진 공격은 사용하지 않습니다.
[RequireComponent(typeof(CapsuleCollider))]
[RequireComponent(typeof(MonsterStatus))]
public class ParryTestMonster : MonoBehaviour
{
    [Header("Target")]
    // 공격 대상으로 삼을 플레이어입니다. 비워두면 PlayerStatus를 가진 오브젝트를 자동으로 찾습니다.
    [SerializeField] private Transform target;

    // 투사체가 플레이어 발밑이 아니라 몸통 쪽으로 날아가도록 더하는 보정값입니다.
    [SerializeField] private Vector3 targetAimOffset = Vector3.up;

    [Header("Ranged Attack")]
    // 발사할 몬스터 투사체 프리팹입니다.
    [SerializeField] private BulletProjectile bulletPrefab;

    // 투사체 발사 위치입니다. 비워두면 몬스터 위치에서 발사합니다.
    [SerializeField] private Transform firePoint;

    // 활성화된 뒤 첫 원거리 공격까지 기다리는 시간입니다.
    [SerializeField] private float firstRangedAttackDelay = 0.5f;

    // 원거리 공격 반복 주기입니다.
    [SerializeField] private float rangedAttackInterval = 1.5f;

    // 투사체가 요청하는 데미지입니다.
    [SerializeField] private float rangedAttackDamage = 5f;

    // 다음 원거리 공격까지 남은 시간입니다.
    private float rangedAttackTimer;

    private void Awake()
    {
        // 기존 씬 오브젝트에 스크립트를 붙여도 필요한 상태 컴포넌트가 보장되게 합니다.
        EnsureMonsterStatus();
    }

    private void OnEnable()
    {
        // 활성화될 때마다 첫 발사 대기 시간을 다시 적용합니다.
        rangedAttackTimer = firstRangedAttackDelay;
    }

    private void Update()
    {
        // 대상이 비어 있으면 씬에서 PlayerStatus를 가진 오브젝트를 찾아 사용합니다.
        FindTargetIfNeeded();
        if (target == null) return;

        FaceTarget();
        UpdateRangedAttack();
    }

    private void UpdateRangedAttack()
    {
        // 원거리 공격 타이머가 끝나면 투사체를 발사하고 다음 주기를 다시 설정합니다.
        rangedAttackTimer -= Time.deltaTime;
        if (rangedAttackTimer > 0f) return;

        FireRangedAttack();
        rangedAttackTimer = rangedAttackInterval;
    }

    private void FireRangedAttack()
    {
        // 프리팹이 없으면 테스트 몬스터는 공격하지 않습니다.
        if (bulletPrefab == null) return;

        Vector3 spawnPosition = firePoint != null ? firePoint.position : transform.position;
        Vector3 direction = (target.position + targetAimOffset) - spawnPosition;

        if (direction.sqrMagnitude <= Mathf.Epsilon)
            direction = transform.forward;

        BulletProjectile bullet = Instantiate(
            bulletPrefab,
            spawnPosition,
            Quaternion.LookRotation(direction.normalized)
        );

        // 탄환은 충돌 시 DamageRequestEvent를 발행합니다.
        bullet.Init(direction, rangedAttackDamage, gameObject);
    }

    private void FaceTarget()
    {
        // y축 차이는 무시하고 수평 방향으로만 바라봅니다.
        Vector3 direction = GetFlatDirectionToTarget();
        if (direction.sqrMagnitude <= Mathf.Epsilon) return;

        transform.rotation = Quaternion.LookRotation(direction);
    }

    private Vector3 GetFlatDirectionToTarget()
    {
        // 현재 프로젝트의 플레이어 이동 축과 맞게 수평 방향만 계산합니다.
        Vector3 direction = target.position - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude <= Mathf.Epsilon)
            return transform.forward;

        return direction.normalized;
    }

    private void FindTargetIfNeeded()
    {
        // 인스펙터에서 대상을 직접 넣었다면 자동 탐색은 하지 않습니다.
        if (target != null) return;

        // Unity 6 기준 API로 씬의 플레이어를 찾습니다.
        PlayerStatus playerStatus = FindFirstObjectByType<PlayerStatus>();
        if (playerStatus != null)
            target = playerStatus.transform;
    }

    private void EnsureMonsterStatus()
    {
        // RequireComponent는 새로 붙일 때만 보장되므로 기존 오브젝트를 위해 한 번 더 확인합니다.
        if (GetComponent<MonsterStatus>() != null) return;

        gameObject.AddComponent<MonsterStatus>();
    }
}
