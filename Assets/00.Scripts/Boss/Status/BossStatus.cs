using UnityEngine;

public class BossStatus : MonoBehaviour, IInitializable
{
    [SerializeField] private BossStatusData status;

    public int Priority => (int)InitOrder.Boss;

    public void Init()
    {
        // 보스 스탯 보정값을 초기화한 뒤 현재 체력을 최대 체력으로 맞춥니다.
        status.ResetAllModifiers();
        status.Init();
        TestPrint();
    }

    public void TakeDamage(float amount)
    {
        status.SubCurrentHP(amount);
    }

    public float GetAtkPower(BossAttackType type)
    {
        return status.GetAtkPower(type);
    }

    public void TestPrint()
    {
        Debug.Log(status.CurrentHP);
    }
}
