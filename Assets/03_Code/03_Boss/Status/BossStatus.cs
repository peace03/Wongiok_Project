using UnityEngine;

public class BossStatus : MonoBehaviour, IInitializable, IDamageable
{
    public int Priority => (int)InitOrder.Boss;

    [SerializeField] private BossStatusData status;

    private float playerMaxHP; //병합할 때 플레이어 체력 ServiceLocator로 가져와서 넣어주면 됨

    public Stat BossMaxHP => status.MaxHP;

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
    public void TakeDamage(float amount)//, Vector3 hitPoint = default)
    {
        status.SubCurrentHP(amount);
        //Debug.Log($"보스 현재 체력: {status.CurrentHP}");
        //if (hitPoint != default)
        //{
        //    이벤트 버스로 이펙트 실행시켜주기
        //}
    }

    public float GetBossCurHP()
    {
        return status.CurrentHP;
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
