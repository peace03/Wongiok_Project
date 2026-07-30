using UnityEngine;

[DisallowMultipleComponent]
public sealed class MonsterHitAnimationHandler : MonoBehaviour
{
    [Header("Hit Animation")]
    [SerializeField] private Animator animator;
    [SerializeField] private MonsterHealth monsterHealth;
    [SerializeField] private MonsterMeleeAI meleeAI;
    [SerializeField] private string hitTriggerName = "Hit";

    // 피격 애니메이션에 필요한 컴포넌트를 준비합니다
    private void Awake()
    {
        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }

        if (monsterHealth == null)
        {
            monsterHealth = GetComponent<MonsterHealth>();
        }

        if (meleeAI == null)
        {
            meleeAI = GetComponent<MonsterMeleeAI>();
        }
    }

    // 데미지 적용 이벤트를 구독합니다
    private void OnEnable()
    {
        EventBus<DamageHitEvent>.action += HandleDamageHit;
    }

    // 데미지 적용 이벤트 구독을 해제합니다
    private void OnDisable()
    {
        EventBus<DamageHitEvent>.action -= HandleDamageHit;
    }

    // 자신이 실제로 피해를 받았을 때 피격 애니메이션을 실행합니다
    private void HandleDamageHit(DamageHitEvent hitEvent)
    {
        if (hitEvent.TargetObject == null)
        {
            return;
        }

        if (!IsSameObjectOrChild(hitEvent.TargetObject))
        {
            return;
        }

        if (monsterHealth != null && monsterHealth.IsDead)
        {
            return;
        }

        if (meleeAI != null)
        {
            meleeAI.InterruptAttackByHit();
        }

        PlayHitAnimation();
    }

    // 전달된 대상이 이 몬스터 또는 자식 오브젝트인지 확인합니다
    private bool IsSameObjectOrChild(GameObject targetObject)
    {
        if (targetObject == gameObject)
        {
            return true;
        }

        Transform targetTransform = targetObject.transform;

        return targetTransform.IsChildOf(transform) ||
               transform.IsChildOf(targetTransform);
    }

    // Animator의 피격 Trigger를 실행합니다
    private void PlayHitAnimation()
    {
        if (animator == null ||
            string.IsNullOrEmpty(hitTriggerName))
        {
            return;
        }

        animator.ResetTrigger(hitTriggerName);
        animator.SetTrigger(hitTriggerName);
    }
}