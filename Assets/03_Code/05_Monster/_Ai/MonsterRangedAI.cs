using System.Collections;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(MonsterGroundMotor))]
[RequireComponent(typeof(MonsterBulletLauncher))]
public class MonsterRangedAI : MonsterBase
{
    [Header("Ranged")]
    [SerializeField] private float attackRange = 6f;
    [SerializeField]
    private MonsterBulletLauncher bulletLauncher;
    [SerializeField] private Transform firePoint;
    [SerializeField]
    private Vector3 firePointOffset =
        new Vector3(0.7f, 0.4f, 0f);
    [SerializeField]
    private float fallbackProjectileDamage = 10f;
    [SerializeField]
    private int projectilePenetrationCount = 0;
    [SerializeField]
    private float attackCooldown = 1.5f;
    [SerializeField]
    private float attackWindup = 0.25f;
    [SerializeField]
    private bool aimAtTarget = false;

    private float lastAttackTime = -999f;
    private bool isAttacking;
    private bool hasLoggedMissingLauncher;

    // 원거리 일반몹에 필요한 컴포넌트 참조를 준비합니다
    protected override void Awake()
    {
        base.Awake();
        CacheBulletLauncher();
    }

    // 김연호 : 풀에서 다시 활성화될 때 공격 쿨다운과 공격 진행 상태를 초기화합니다
    private void OnEnable()
    {
        lastAttackTime = -999f;
        isAttacking = false;
    }

    // 김연호 : 풀 반환이나 비활성화 시 진행 중인 원거리 공격 Coroutine을 정리합니다
    private void OnDisable()
    {
        StopAllCoroutines();
        isAttacking = false;
    }

    // 컴포넌트가 추가될 때 탄환 발사기 참조를 준비합니다
    private void Reset()
    {
        bulletLauncher =
            GetComponent<
                MonsterBulletLauncher>();
    }

    // 원거리 일반몹의 추적과 발사 판단을 처리합니다
    protected override void TickMonster()
    {
        if (!IsTargetInDetectionRange())
        {
            return;
        }

        FaceTarget();

        if (IsTargetInRange(
                attackRange,
                VerticalTolerance))
        {
            TryStartAttack();
            return;
        }

        if (!isAttacking)
        {
            ChaseTarget();
        }
    }

    // 원거리 공격이 가능하면 공격 코루틴을 시작합니다
    private void TryStartAttack()
    {
        if (isAttacking)
        {
            return;
        }

        if (Time.time <
            lastAttackTime +
            attackCooldown)
        {
            return;
        }

        StartCoroutine(
            ShootRoutine()
        );
    }

    // 공격 선딜 후 공용 Bullet을 발사합니다
    private IEnumerator ShootRoutine()
    {
        isAttacking = true;

        StopHorizontalMovement();

        yield return
            new WaitForSeconds(
                attackWindup
            );

        if (!IsDead)
        {
            ShootProjectile();
        }

        lastAttackTime =
            Time.time;

        isAttacking = false;
    }

    // 바라보는 방향 또는 플레이어 방향으로 공용 Bullet을 발사합니다
    private void ShootProjectile()
    {
        if (!TryGetBulletLauncher(
                out MonsterBulletLauncher
                    launcher))
        {
            return;
        }

        Vector3 spawnPosition =
            GetFirePosition();

        Vector3 shootDirection =
            aimAtTarget
                ? GetDirectionToTarget(
                    spawnPosition
                )
                : GetFacingDirectionVector();

        if (shootDirection.sqrMagnitude <=
            0.0001f)
        {
            shootDirection =
                GetFacingDirectionVector();
        }

        float damage =
            GetAttackPower(
                fallbackProjectileDamage
            );

        launcher.TryFire(
            spawnPosition,
            shootDirection,
            damage,
            projectilePenetrationCount
        );
    }

    // 일반몹에 연결된 공용 탄환 발사기를 가져옵니다
    private bool TryGetBulletLauncher(
        out MonsterBulletLauncher launcher)
    {
        if (bulletLauncher == null)
        {
            CacheBulletLauncher();
        }

        launcher =
            bulletLauncher;

        if (launcher != null)
        {
            hasLoggedMissingLauncher =
                false;

            return true;
        }

        if (!hasLoggedMissingLauncher)
        {
            Debug.LogWarning(
                "MonsterBulletLauncher가 없습니다.",
                this
            );

            hasLoggedMissingLauncher =
                true;
        }

        return false;
    }

    // 같은 오브젝트에 있는 탄환 발사기를 저장합니다
    private void CacheBulletLauncher()
    {
        if (bulletLauncher == null)
        {
            bulletLauncher =
                GetComponent<
                    MonsterBulletLauncher>();
        }
    }

    // 탄환이 생성될 위치를 계산합니다
    private Vector3 GetFirePosition()
    {
        if (firePoint != null)
        {
            return FixDepthVector(
                firePoint.position
            );
        }

        Vector3 offset =
            GetFacingOffset(
                firePointOffset
            );

        return FixDepthVector(
            transform.position +
            offset
        );
    }

    // 김연호 : 사망 상태에 들어갈 때 공격 Coroutine과 공격 진행 상태를 즉시 정리합니다
    protected override void
        OnDeadStateEntered()
    {
        StopAllCoroutines();
        isAttacking = false;

        base.OnDeadStateEntered();
    }

    // Scene 뷰에서 감지 범위와 공격 범위를 표시합니다
    private void OnDrawGizmosSelected()
    {
        DrawDetectionGizmo();

        Gizmos.color = Color.cyan;

        Gizmos.DrawWireCube(
            transform.position,
            new Vector3(
                attackRange * 2f,
                VerticalTolerance * 2f,
                1f
            )
        );

        Gizmos.color = Color.red;

        Gizmos.DrawSphere(
            GetFirePositionForGizmo(),
            0.12f
        );
    }

    // Gizmo 표시용 탄환 생성 위치를 계산합니다
    private Vector3
        GetFirePositionForGizmo()
    {
        if (firePoint != null)
        {
            return firePoint.position;
        }

        float direction =
            transform.localScale.x >= 0f
                ? 1f
                : -1f;

        Vector3 offset =
            new Vector3(
                firePointOffset.x *
                direction,
                firePointOffset.y,
                firePointOffset.z
            );

        return transform.position +
               offset;
    }
}