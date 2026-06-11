using UnityEngine;

public class BossAnimationKeyReceiver : MonoBehaviour, IInitializable
{
    public int Priority => 3;
    private IBossLogics bossPatternLogic;


    public void Init()
    {
        bossPatternLogic = ServiceLocator.Get<IBossLogics>();
    }

    public void EnableParry()
    {
        Debug.Log("패링 가능!");
    }
    public void DisableParry()
    {
        Debug.Log("패링 불가능!");
    }
    public void OnCollider()
    {
        Debug.Log("공격 콜라이더 온!");
    }
    public void OffCollider()
    {
        Debug.Log("공격 콜라이더 오프!");
    }
}
