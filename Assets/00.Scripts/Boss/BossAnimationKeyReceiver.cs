using UnityEngine;

public class BossAnimationKeyReceiver : MonoBehaviour, IInitializable
{
    public int Priority => (int)InitOrder.Boss+3;
    private IBossLogics bossPatternLogic;


    public void Init()
    {
        bossPatternLogic = ServiceLocator.Get<IBossLogics>();
    }

    //?좊땲硫붿씠???ㅻ줈 ?대깽??諛쒖깮
    public void EnableParry()
    {
        //Debug.Log("?⑤쭅 媛??");
        EventBus<CanParryEvent>.Publish(new CanParryEvent(true));
    }
    public void DisableParry()
    {
        //Debug.Log("?⑤쭅 遺덇???");
        EventBus<CanParryEvent>.Publish(new CanParryEvent(false));
    }
    public void OnCollider()
    {
        //諛⑹뼱肄붾뱶
        if (bossPatternLogic.IsParryed) return; //?⑤쭅 爾ㅻ뒗吏
        if (!bossPatternLogic.IsAttacking()) return; //?몃옖吏??以묒씤吏

        //Debug.Log("怨듦꺽 肄쒕씪?대뜑 ??");
        EventBus<ColliderToggleEvent>.
            Publish(new ColliderToggleEvent(bossPatternLogic.GetAttackType(), true));
    }
    public void OffCollider()
    {
        //Debug.Log("怨듦꺽 肄쒕씪?대뜑 ?ㅽ봽!");
        EventBus<ColliderToggleEvent>.
            Publish(new ColliderToggleEvent(bossPatternLogic.GetAttackType(), false));
    }
}
