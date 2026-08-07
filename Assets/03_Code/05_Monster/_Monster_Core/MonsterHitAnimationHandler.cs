using UnityEngine;

public class MonsterHitAnimationHandler : MonoBehaviour
{
    [Header("Animation")]
    [SerializeField] private Animator animator;
    [SerializeField] private string hitTriggerName = "Hit";

    private int hitTriggerHash;

    // 김연호 : Animator 참조를 찾고 Hit Trigger의 해시 값을 초기화합니다
    private void Awake()
    {
        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }

        hitTriggerHash = Animator.StringToHash(hitTriggerName);
    }

    // 김연호 : 몬스터가 실제 피해를 받은 시점을 감지하도록 피격 이벤트를 구독합니다
    private void OnEnable()
    {
        EventBus<MonsterDamagedEvent>.action += HandleMonsterDamaged;
    }

    // 김연호 : 비활성화된 몬스터가 이벤트를 받지 않도록 구독을 해제합니다
    private void OnDisable()
    {
        EventBus<MonsterDamagedEvent>.action -= HandleMonsterDamaged;
    }

    // 김연호 : 자신에게 발생한 비치명적 피격에서만 Hit 애니메이션을 실행합니다
    private void HandleMonsterDamaged(MonsterDamagedEvent damagedEvent)
    {
        if (damagedEvent.MonsterObject == null)
        {
            return;
        }

        if (!IsSameObjectOrChild(damagedEvent.MonsterObject))
        {
            return;
        }

        if (damagedEvent.CurrentHP <= 0f)
        {
            return;
        }

        PlayHitAnimation();
    }

    // 김연호 : 전달된 몬스터가 이 컴포넌트의 오브젝트 또는 부모 자식 관계인지 확인합니다
    private bool IsSameObjectOrChild(GameObject monsterObject)
    {
        if (monsterObject == gameObject)
        {
            return true;
        }

        Transform monsterTransform = monsterObject.transform;

        return monsterTransform.IsChildOf(transform)
            || transform.IsChildOf(monsterTransform);
    }

    // 김연호 : Animator에 설정된 Hit Trigger를 실행해 피격 애니메이션을 재생합니다
    private void PlayHitAnimation()
    {
        if (animator == null)
        {
            return;
        }

        if (string.IsNullOrEmpty(hitTriggerName))
        {
            return;
        }

        animator.ResetTrigger(hitTriggerHash);
        animator.SetTrigger(hitTriggerHash);
    }
}