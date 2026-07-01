using UnityEngine;

public class BossStatus : MonoBehaviour, IInitializable
{
    public int Priority => (int)InitOrder.Boss;

    [SerializeField] public BossStatusData status;

    private float playerMaxHP; //병합할 때 플레이어 체력 ServiceLocator로 가져와서 넣어주면 됨

    public Stat_Y BossMaxHP => status.MaxHP;

    public void Init()
    {
        Debug.Log($"{Priority}번 BossStatus의 Init()호출");
        //병합할 때 플레이어 체력 ServiceLocator로 가져와서 넣어주면 됨
        playerMaxHP = 120;
        status.ResetAllModifiers(); //계산식 먼저 초기화
        status.Init();
        TestPrint();
    }

    //그로기시 데미지 배율 설정
    public void SetGroggyDamageMultiplierActive(bool state) { status.SetGroggyDamageMultiplierActive(state); }
    public void TakeDamage(float amount)
    {
        status.SubCurrentHP(amount);
        //Debug.Log($"보스 현재 체력: {status.CurrentHP}");
    }

    public float GetAtkPower(AttackType type)
    {
        return status.GetAtkPower(type, playerMaxHP);
    }

    public void TestPrint()
    {
        Debug.Log(status.CurrentHP);
    }
}
