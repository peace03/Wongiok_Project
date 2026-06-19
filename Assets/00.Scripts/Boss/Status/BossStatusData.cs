using System;
using UnityEngine;

[Serializable]
public class BossStatusData
{
    [Header("Status")]
    [SerializeField] private BossStat maxHP;
    [SerializeField] private BossStat damageTakenMultiplier;
    [SerializeField] private BossStat attackSpeed;
    [SerializeField] private BossStat telegraphSpeed;
    [SerializeField] private BossStat attackAPower;
    [SerializeField] private BossStat attackBPower;
    [SerializeField] private BossStat attackCPower;
    [SerializeField] private BossStat attackDPower;

    [Header("Ultimate")]
    // 보스 체력이 각 비율 아래로 내려가면 궁극기 전환 이벤트를 발행합니다.
    [SerializeField] private float[] hpThresholds;

    private int thresholdIndex;
    private float currentHP;

    public float CurrentHP => currentHP;
    public bool IsDead => currentHP <= 0f;

    public void Init()
    {
        thresholdIndex = 0;
        currentHP = maxHP.FinalValue;
    }

    public void ResetAllModifiers()
    {
        maxHP.ResetModifiers();
        damageTakenMultiplier.ResetModifiers();
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
        if (currentHP < 0f)
            currentHP = 0f;

        TryPublishUltimateThreshold();
    }

    public float GetAtkPower(BossAttackType type)
    {
        return type switch
        {
            BossAttackType.A => attackAPower.FinalValue,
            BossAttackType.B => attackBPower.FinalValue,
            BossAttackType.C => attackCPower.FinalValue,
            _ => attackAPower.FinalValue
        };
    }

    private void TryPublishUltimateThreshold()
    {
        // 임계값 배열이 비어 있거나 모두 사용된 경우에는 추가 이벤트를 발행하지 않습니다.
        if (hpThresholds == null) return;
        if (thresholdIndex >= hpThresholds.Length) return;

        if (currentHP >= maxHP.FinalValue * hpThresholds[thresholdIndex]) return;

        EventBus<UltimateInvoke>.Publish(default);
        thresholdIndex++;
    }
}
