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
        EventBus<ParryEvent>.Publish(new ParryEvent(true));
    }
    public void DisableParry()
    {
        //Debug.Log("패링 불가능!");
        EventBus<ParryEvent>.Publish(new ParryEvent(false));
    }
    public void OnCollider()
    {
        //Debug.Log("공격 콜라이더 온!");
        EventBus<ColliderEvent>.
            Publish(new ColliderEvent(bossPatternLogic.GetAttackType(), true));
    }
    public void OffCollider()
    {
        //Debug.Log("공격 콜라이더 오프!");
        EventBus<ColliderEvent>.
            Publish(new ColliderEvent(bossPatternLogic.GetAttackType(), false));
    }
}
