using UnityEngine;
using System.Collections.Generic;

public class BossStatus : MonoBehaviour, IInitializable, IDamageable
{
    public int Priority => (int)InitOrder.Boss;

    [Header("SFX")]
    [SerializeField] private List<AudioClip> SFX_TakeDamages;
    [SerializeField, Range(0f, 1f)] private float takeDamageSfxVolume = 1f;

    [Header("Stats")]
    [SerializeField] private BossStatusData status;

    [Header("플레이어 최대체력 (PlayerStatus.cs)")]
    [SerializeField] private PlayerStatus playerMaxHP; //병합할 때 플레이어 체력 ServiceLocator로 가져와서 넣어주면 됨

    public Stat BossMaxHP => status.MaxHP;

    public void Init()
    {
        Debug.Log($"{Priority}번 BossStatus의 Init()호출");
        //병합할 때 플레이어 체력 ServiceLocator로 가져와서 넣어주면 됨
        status.ResetAllModifiers(); //계산식 먼저 초기화
        status.Init();
        TestPrint();
    }

    //그로기시 데미지 배율 설정
    public void SetGroggyDamageMultiplierActive(bool state) { status.SetGroggyDamageMultiplierActive(state); }
    public void TakeDamage(float amount)
    {
        status.SubCurrentHP(amount);
        // 랜덤 보스 피격음에 Inspector에서 설정한 볼륨을 적용한다.
        EventBus<Play2DSoundEvent>.Publish(
            new Play2DSoundEvent(clips: SFX_TakeDamages, volume: takeDamageSfxVolume));
    }

    public float GetBossCurHP()
    {
        return status.CurrentHP;
    }
    public float GetAtkPower(AttackType type)
    {
        return status.GetAtkPower(type, playerMaxHP.GetMaxHP());
    }

    public void TestPrint()
    {
        Debug.Log(status.CurrentHP);
    }

}
