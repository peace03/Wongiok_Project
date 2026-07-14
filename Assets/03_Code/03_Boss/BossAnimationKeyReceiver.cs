using UnityEngine;

public class BossAnimationKeyReceiver : MonoBehaviour, IInitializable
{
    public int Priority => (int)InitOrder.Boss+3;
    private IBossLogics bossPatternLogic;


    public void Init()
    {
        bossPatternLogic = ServiceLocator.Get<IBossLogics>();
    }

    //애니메이션 키로 이벤트 발생
    public void EnableParry()
    {
        //Debug.Log("패링 가능!");
        EventBus<CanParryEvent>.Publish(new CanParryEvent(true));
    }
    public void DisableParry()
    {
        //Debug.Log("패링 불가능!");
        EventBus<CanParryEvent>.Publish(new CanParryEvent(false));
    }
    public void OnCollider()
    {
        //방어코드
        if (bossPatternLogic.IsParryed) return; //패링 쳤는지
        if (!bossPatternLogic.IsAttacking()) return; //트랜지션 중인지

        //Debug.Log("공격 콜라이더 온!");
        EventBus<ColliderToggleEvent>.
            Publish(new ColliderToggleEvent(bossPatternLogic.GetAttackId(), true));
    }
    public void OffCollider()
    {
        //Debug.Log("공격 콜라이더 오프!");
        EventBus<ColliderToggleEvent>.
            Publish(new ColliderToggleEvent(bossPatternLogic.GetAttackId(), false));
    }
}
