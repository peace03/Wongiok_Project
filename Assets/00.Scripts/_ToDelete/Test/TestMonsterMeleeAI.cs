using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 근접 패링 검증을 위한 독립 테스트 몬스터입니다.
[RequireComponent(typeof(CharacterController))]
public class TestMonsterMeleeAI : MonoBehaviour
{
    private const string DefaultPlayerLayerName = "Player";

    [Header("Target")]
    // 비워두면 PlayerStatus를 가진 오브젝트를 자동으로 찾습니다.
    [SerializeField] private Transform target;
    [SerializeField] private float detectionRange = 6f;
    [SerializeField] private float verticalTolerance = 2f;
    [SerializeField] private float moveSpeed = 2.5f;

    [Header("Melee")]
    // 테스트 몬스터의 근접 공격이 맞출 수 있는 플레이어 레이어입니다.
    [SerializeField] private LayerMask playerHitMask;
    [SerializeField] private float attackRange = 1.3f;
    [SerializeField] private float attackDamage = 10f;
    [SerializeField] private float attackCooldown = 1.2f;
    [SerializeField] private float attackWindup = 0.25f;
    [SerializeField] private float attackRecovery = 0.35f;
    [SerializeField] private Vector3 attackBoxSize = new Vector3(1.2f, 1.2f, 1.2f);
    [SerializeField] private Vector3 attackBoxOffset = new Vector3(0.8f, 0f, 0f);

    private CharacterController characterController;
    private float lastAttackEndTime = -999f;
    private bool isAttacking;
    private bool isFacingRight = true;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
        EnsureDefaultPlayerHitMask();
        EnsureTarget();
    }

    private void OnValidate()
    {
        EnsureDefaultPlayerHitMask();
    }

    private void Update()
    {
        EnsureTarget();
        if (target == null) return;
        if (!IsTargetInDetectionRange()) return;

        FaceTarget();

        if (IsTargetInAttackRange())
        {
            TryStartAttack();
            return;
        }

        if (!isAttacking)
            ChaseTarget();
    }

    private void TryStartAttack()
    {
        // 공격 중이거나 쿨타임이 남아 있으면 새 공격을 시작하지 않습니다.
        if (isAttacking) return;
        if (Time.time < lastAttackEndTime + attackCooldown) return;

        StartCoroutine(AttackRoutine());
    }

    private IEnumerator AttackRoutine()
    {
        isAttacking = true;

        yield return new WaitForSeconds(attackWindup);

        DoAttackHitCheck();

        yield return new WaitForSeconds(attackRecovery);

        lastAttackEndTime = Time.time;
        isAttacking = false;
    }

    private void DoAttackHitCheck()
    {
        // 공격 판정 박스 안의 플레이어를 찾고, 같은 플레이어는 한 번만 처리합니다.
        Collider[] hits = Physics.OverlapBox(
            GetAttackBoxCenter(),
            attackBoxSize * 0.5f,
            Quaternion.identity,
            playerHitMask,
            QueryTriggerInteraction.Ignore
        );

        HashSet<PlayerStatus> damagedTargets = new HashSet<PlayerStatus>();

        for (int i = 0; i < hits.Length; i++)
        {
            PlayerStatus playerStatus = hits[i].GetComponentInParent<PlayerStatus>();
            if (playerStatus == null) continue;
            if (damagedTargets.Contains(playerStatus)) continue;

            damagedTargets.Add(playerStatus);
            ApplyDamageToPlayer(playerStatus, hits[i]);
        }
    }

    private void ApplyDamageToPlayer(PlayerStatus playerStatus, Collider hitCollider)
    {
        Vector3 hitPoint = hitCollider.ClosestPoint(transform.position);
        Vector3 hitDirection = GetFacingDirectionVector();

        DamageInfo damageInfo = new DamageInfo(
            playerStatus.gameObject,
            hitCollider,
            gameObject,
            hitPoint,
            hitDirection,
            attackDamage
        );

        // 근접 패링 창이 열려 있으면 데미지를 넣지 않고 이번 공격을 취소합니다.
        PlayerParry playerParry = playerStatus.GetComponent<PlayerParry>();
        if (playerParry != null && playerParry.TryConsumeMeleeParry(damageInfo))
            return;

        playerStatus.TakeDamage(damageInfo);
    }

    private void ChaseTarget()
    {
        Vector3 direction = target.position - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude <= Mathf.Epsilon) return;

        Vector3 movement = direction.normalized * moveSpeed * Time.deltaTime;
        characterController.Move(movement);
    }

    private void FaceTarget()
    {
        Vector3 direction = target.position - transform.position;
        if (Mathf.Approximately(direction.x, 0f)) return;

        isFacingRight = direction.x > 0f;

        Vector3 scale = transform.localScale;
        scale.x = Mathf.Abs(scale.x) * (isFacingRight ? 1f : -1f);
        transform.localScale = scale;
    }

    private bool IsTargetInDetectionRange()
    {
        Vector3 difference = target.position - transform.position;
        if (Mathf.Abs(difference.y) > verticalTolerance) return false;

        difference.y = 0f;
        return difference.sqrMagnitude <= detectionRange * detectionRange;
    }

    private bool IsTargetInAttackRange()
    {
        Vector3 difference = target.position - transform.position;
        if (Mathf.Abs(difference.y) > verticalTolerance) return false;

        difference.y = 0f;
        return difference.sqrMagnitude <= attackRange * attackRange;
    }

    private Vector3 GetAttackBoxCenter()
    {
        return transform.position + GetFacingOffset(attackBoxOffset);
    }

    private Vector3 GetFacingOffset(Vector3 offset)
    {
        float direction = isFacingRight ? 1f : -1f;
        return new Vector3(offset.x * direction, offset.y, offset.z);
    }

    private Vector3 GetFacingDirectionVector()
    {
        return isFacingRight ? Vector3.right : Vector3.left;
    }

    private void EnsureDefaultPlayerHitMask()
    {
        // 인스펙터에서 직접 지정한 마스크가 있으면 유지합니다.
        if (playerHitMask.value != 0 && playerHitMask.value != ~0) return;

        int playerMask = LayerMask.GetMask(DefaultPlayerLayerName);
        if (playerMask == 0) return;

        playerHitMask = playerMask;
    }

    private void EnsureTarget()
    {
        if (target != null) return;

        PlayerStatus playerStatus = FindFirstObjectByType<PlayerStatus>();
        if (playerStatus == null) return;

        target = playerStatus.transform;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(GetAttackBoxCenterForGizmo(), attackBoxSize);
    }

    private Vector3 GetAttackBoxCenterForGizmo()
    {
        float direction = transform.localScale.x >= 0f ? 1f : -1f;
        Vector3 offset = new Vector3(attackBoxOffset.x * direction, attackBoxOffset.y, attackBoxOffset.z);

        return transform.position + offset;
    }
}
