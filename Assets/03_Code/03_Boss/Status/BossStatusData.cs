using UnityEngine;
using System;

[Serializable]
public class BossStatusData
{
    [Header("스탯")]
    [SerializeField] private Stat maxHP;                    //최대 체력
    [SerializeField] private Stat attackSpeed;              //공속 배율
    [SerializeField] private Stat telegraphSpeed;           //사전 신호 표시 시간 배율
    [Tooltip("공격력 퍼센티지")][SerializeField] private Stat attackAPower;      //패턴A 공격력
    [Tooltip("공격력 퍼센티지")][SerializeField] private Stat attackBPower;      //패턴B 공격력
    [Tooltip("공격력 퍼센티지")][SerializeField] private Stat attackCPower;      //패턴C 공격력
    [Tooltip("공격력 퍼센티지")][SerializeField] private Stat attackC_2Power;    //패턴C 공격력
    [Tooltip("공격력 퍼센티지")][SerializeField] private Stat attackDPower;      //궁극기 공격력
    [Header("궁극기 체력 임계치")]
    [SerializeField] private float[] hpThresholds;            //궁극기 체력 임계치
    [Header("데미지 배율")]
    [Tooltip("그로기 피격 데미지 배율")][SerializeField] private float groggyDamageMultiplier;

    private int index = 0;                          //궁극기 임계치 인덱스(인덱스 마지막은 0으로)
    private bool isGroggyState = false;                     //궁극기 상태인지 확인

    public Stat MaxHP => maxHP;

    private float currentHP;
    public float CurrentHP => currentHP;

    private bool isDead => currentHP <= 0f;
    public bool IsDead => isDead;

    public void Init()
    {
        currentHP = maxHP.FinalValue;
        index = 0;
    }

    public void ResetAllModifiers()
    {
        maxHP.ResetModifiers();
        attackSpeed.ResetModifiers();
        telegraphSpeed.ResetModifiers();
        attackAPower.ResetModifiers();
        attackBPower.ResetModifiers();
        attackCPower.ResetModifiers();
        attackC_2Power.ResetModifiers();
        attackDPower.ResetModifiers();
    }

    public void SetGroggyDamageMultiplierActive(bool active) { isGroggyState = active; }

    public void SubCurrentHP(float amount)
    {
        if (IsDead)
            return;

        if (isGroggyState)
        {
            amount *= groggyDamageMultiplier;
            Debug.Log("오 실행된다");
        }
        currentHP = Mathf.Clamp(currentHP - amount, 0f, maxHP.FinalValue);
        EventBus<BossHPChangedEvent>.Publish(new BossHPChangedEvent(currentHP)); //UI bridge
        Debug.Log("보스 체력: "+currentHP);

        if (IsDead)
        {
            EventBus<BossDeadEvent>.Publish(default);
            return;
        }

        if (hpThresholds != null &&
            index < hpThresholds.Length &&
            currentHP < maxHP.FinalValue * hpThresholds[index])  //궁극기 체력 임계치 검사
        {
            EventBus<UltimateInvokeEvent>.Publish(default); //UI bridge
            index++;
        }
    }

    public float GetAtkPower(AttackType type, float playerMaxHP)
    {
        return type switch
        {
            AttackType.A => attackAPower.FinalValue / 100 * playerMaxHP,
            AttackType.B => attackBPower.FinalValue / 100 * playerMaxHP,
            AttackType.C => attackCPower.FinalValue / 100 * playerMaxHP,
            AttackType.C_2 => attackC_2Power.FinalValue / 100 * playerMaxHP,
            AttackType.D => attackDPower.FinalValue / 100 * playerMaxHP,
            _ => 0f
        };
    }
}
