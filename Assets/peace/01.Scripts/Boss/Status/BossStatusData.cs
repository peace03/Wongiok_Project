using UnityEngine;
using System;

[Serializable]
public class BossStatusData
{
    [SerializeField] private Stat maxHP;                    //최대 체력
    [SerializeField] private Stat damageTakenMultiplier;    //피격 데미지 배율
    [SerializeField] private Stat moveSpeed;                //이동속도
    [SerializeField] private Stat attackSpeed;              //공속 배율
    [SerializeField] private Stat telegraphSpeed;           //사전 신호 표시 시간 배율
    [SerializeField] private Stat attackAPower;             //패턴A 공격력
    [SerializeField] private Stat attackBPower;             //패턴B 공격력
    [SerializeField] private Stat attackCPower;             //패턴C 공격력
    [SerializeField] private Stat attackDPower;             //궁극기 공격력

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
        moveSpeed.ResetModifiers();
        attackSpeed.ResetModifiers();
        telegraphSpeed.ResetModifiers();
        attackAPower.ResetModifiers();
        attackBPower.ResetModifiers();
        attackCPower.ResetModifiers();
        attackDPower.ResetModifiers();
    }
    
}
