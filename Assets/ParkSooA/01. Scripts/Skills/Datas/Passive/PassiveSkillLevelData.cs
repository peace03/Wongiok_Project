using UnityEngine;
using System;
using System.Collections.Generic;

[Serializable]
// 패시브 레벨 정보
public class PassiveSkillLevelData : BaseSkillLevelData
{
    [SerializeField] private PASSIVE_TRIGGER_TYPE triggerType;      // 발동 조건
    [SerializeField] private List<StatAdjustment> appliedStats;     // 바꿀 스탯 정보들

    public PASSIVE_TRIGGER_TYPE TriggerType => triggerType;

    // 바꿀 스탯 정보들 반환 함수
    public override IReadOnlyList<StatAdjustment> GetAppliedStats() => appliedStats;

    // 스킬 효과 적용 함수
    public override void ApplyEffect(GameObject target)
    {
        // 스탯이 없다면
        if (!target.TryGetComponent<PlayerStatus>(out var trgStat))
        {
            Debug.Log($"[Error | Skill] 해당하는 {typeof(PlayerStatus)} 없음 ⇒ 입력 - 대상 : {target.name}\n", target);
            return;
        }

        // 변화량을 저장할 변수
        float amount;

        // 바꿀 스탯의 수만큼
        foreach (var stat in appliedStats)
        {
            // 변화량 구하기
            amount = stat.modifyType == MODIFY_TYPE.Addition ? stat.amount : -stat.amount;

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
    public override void RemoveEffect(GameObject target)
    {
        // 스탯이 없다면
        if (!target.TryGetComponent<PlayerStatus>(out var trgStat))
        {
            Debug.Log($"[Error | Skill] 해당하는 {typeof(PlayerStatus)} 없음 ⇒ 입력 - 대상 : {target.name}\n", target);
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