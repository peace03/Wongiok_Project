using UnityEngine;

public class BossStatus : MonoBehaviour, IInitializable
{
    [SerializeField] private BossStatusData status;
    private BossStatus bs;

    public int Priority => (int)InitOrder.Boss;

    public void Init()
    {
        Debug.Log($"{Priority}번 BossStatus의 Init()호출");
        status.ResetAllModifiers(); //계산식 먼저 초기화
        status.Init();
        TestPrint();

        //ServiceLocator를 사용해 참조
        bs = ServiceLocator.Get<BossStatus>();
    }

    public void TestPrint()
    {
        Debug.Log(status.CurrentHP);
    }
}
