using UnityEngine;

public class BossStatus : MonoBehaviour, IInitializable
{
    [SerializeField] private BossStatusData status;

    public int Priority => (int)InitOrder.Boss;

    public void Init()
    {
        Debug.Log($"{Priority}번 BossStatus의 Init()호출");
        status.ResetAllModifiers(); //계산식 먼저 초기화
        status.Init();
        TestPrint();
    }

    public void TakeDamage(float amount)
    {
        status.SubCurrentHP(amount);
    }

    public void TestPrint()
    {
        Debug.Log(status.CurrentHP);
    }
}
