using UnityEngine;
using System;
using System.Collections.Generic;

[Serializable]
// 패시브 레벨 정보
public class PassiveSkillLevelData : BaseSkillLevelData
{
    [Header("발동 조건")]
    [Tooltip("현재는 상시 적용 패시브 밖에 없지만, 나중에 조건부 패시브를 대비하여 준비한 것이므로 신경쓰지 않으셔도 됨")]
    [SerializeField] private PASSIVE_TRIGGER_TYPE triggerType;      // 발동 조건
    [Header("적용할 스탯들")]
    [SerializeField] private List<StatAdjustment> appliedStats;     // 적용할 스탯들

    public PASSIVE_TRIGGER_TYPE TriggerType => triggerType;

    // 바꿀 스탯 정보들 반환 함수
    public override IReadOnlyList<StatAdjustment> GetAppliedStats() => appliedStats;

    // 스킬 효과 적용 함수
    public override void ApplyEffect(GameObject owner, IReadOnlyList<StatAdjustment> prevStats)
    {
        // 스탯이 없다면
        if (!owner.TryGetComponent<PlayerStatus>(out var trgStat))
        {
            Debug.LogError($"[Error | Skill] 해당하는 {typeof(PlayerStatus)} 없음 ⇒ 입력 - 대상 : {owner.name}\n", owner);
            return;
        }

        // 나중에 밑에 계산하는 부분을 클래스로 분리하기!

        // 변화량을 저장할 변수
        float amount;

        // 바꿀 스탯의 수만큼
        foreach (var stat in appliedStats)
        {
            // 변화량 구하기
            amount = stat.modifyType == MODIFY_TYPE.Addition ? stat.amount : -stat.amount;

            // 이전 레벨 스탯이 있다면
            if(prevStats != null)
            {
                // 이전 변화량을 저장할 변수
                float prevAmount = 0;

                // 바꿨던 스탯의 수만큼
                foreach(var prev in prevStats)
                    // 같은 스탯을 찾았다면
                    if (prev.statType == stat.statType)
                    {
                        // 변화량 구하기
                        prevAmount = prev.modifyType == MODIFY_TYPE.Addition ? prev.amount : -prev.amount;
                        break;
                    }

                // 이전 변화량이 있다면
                if (prevAmount != 0)
                    // 변화량에 반영
                    amount -= prevAmount;
            }

            // 스탯 종류에 따라서
            switch (stat.statType)
            {
                // 체력이라면
                case STAT_TYPE.Health:
                    // 최대 체력 변경
                    trgStat.AddMaxHPValue(amount);
                    break;
                // 공격력이라면
                case STAT_TYPE.AtkPower:
                    // 공격력 변경
                    trgStat.AddAttackPowerValue(amount);
                    break;
                // 이동 속도라면
                case STAT_TYPE.MoveSpeed:
                    // 이동 속도 변경
                    trgStat.AddMoveSpeedValue(amount);
                    break;
                // 공격 속도라면
                case STAT_TYPE.AtkSpeed:
                    // 공격 속도 변경
                    trgStat.AddAttackSpeedValue(amount);
                    break;
            }
        }
    }

    // 스킬 효과 적용 해제 함수
    public override void RemoveEffect(GameObject owner)
    {
        // 스탯이 없다면
        if (!owner.TryGetComponent<PlayerStatus>(out var trgStat))
        {
            Debug.LogError($"[Error | Skill] 해당하는 {typeof(PlayerStatus)} 없음 ⇒ 입력 - 대상 : {owner.name}\n", owner);
            return;
        }

        // 변화량을 저장할 변수
        float amount;

        // 바꿀 스탯의 수만큼
        foreach (var stat in appliedStats)
        {
            // 변화량 구하기
            amount = stat.modifyType == MODIFY_TYPE.Addition ? -stat.amount : stat.amount;

            // 스탯 종류에 따라서
            switch (stat.statType)
            {
                // 체력이라면
                case STAT_TYPE.Health:
                    // 최대 체력 변경
                    trgStat.AddMaxHPValue(amount);
                    break;
                // 공격력이라면
                case STAT_TYPE.AtkPower:
                    // 공격력 변경
                    trgStat.AddAttackPowerValue(amount);
                    break;
                // 이동 속도라면
                case STAT_TYPE.MoveSpeed:
                    // 이동 속도 변경
                    trgStat.AddMoveSpeedValue(amount);
                    break;
                // 공격 속도라면
                case STAT_TYPE.AtkSpeed:
                    // 공격 속도 변경
                    trgStat.AddAttackSpeedValue(amount);
                    break;
            }
        }
    }
}