using UnityEngine;
using System;

[Serializable]
public class BossStatusData
{
    [Header("스탯")]
    [SerializeField] private Stat_Y maxHP;                    //최대 체력
    [SerializeField] private Stat_Y damageTakenMultiplier;    //피격 데미지 배율
    //[SerializeField] private Stat moveSpeed;                //이동속도 (speed값이 많이서 필요없는듯?)
    [SerializeField] private Stat_Y attackSpeed;              //공속 배율
    [SerializeField] private Stat_Y telegraphSpeed;           //사전 신호 표시 시간 배율
    [SerializeField] private Stat_Y attackAPower;             //패턴A 공격력
    [SerializeField] private Stat_Y attackBPower;             //패턴B 공격력
    [SerializeField] private Stat_Y attackCPower;             //패턴C 공격력
    [SerializeField] private Stat_Y attackDPower;             //궁극기 공격력
    [Header("궁극기 체력 임계치")]
    [SerializeField] private float[] hpThresholds;            //궁극기 체력 임계치
    int index = 0;                          //궁극기 임계치 인덱스(인덱스 마지막은 0으로)

    private float currentHP;
    public float CurrentHP => currentHP;

    private bool isDead => currentHP <= 0f;
    public bool IsDead => isDead;

    public void Init()
    {
        currentHP = maxHP.FinalValue;
    }

    public void ResetAllModifiers()
    {
        maxHP.ResetModifiers();
        damageTakenMultiplier.ResetModifiers();
        //moveSpeed.ResetModifiers();
        attackSpeed.ResetModifiers();
        telegraphSpeed.ResetModifiers();
        attackAPower.ResetModifiers();
        attackBPower.ResetModifiers();
        attackCPower.ResetModifiers();
        attackDPower.ResetModifiers();
    }

    public void SubCurrentHP(float amount)
    {
        currentHP -= amount;
        if (currentHP < 0) currentHP = 0f;  //사망 검사
        if(currentHP < maxHP.FinalValue * hpThresholds[index])  //궁극기 체력 임계치 검사
        {
            EventBus<UltimateInvoke>.Publish(default);
            index++;
        }
    }

    public float GetAtkPower(AttackType type)
    {
        return type switch
        {
            AttackType.A => attackAPower.FinalValue,
            AttackType.B => attackBPower.FinalValue,
            AttackType.C => attackCPower.FinalValue,
            AttackType.C_2 => attackCPower.FinalValue
        };
    }
}
