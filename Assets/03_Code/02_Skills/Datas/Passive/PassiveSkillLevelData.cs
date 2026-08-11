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
    public override void ApplyEffect(GameObject owner, int id, IReadOnlyList<StatAdjustment> prevStats)
    {
        // 소유자가 없다면
        if (owner == null)
            return;

        // 스탯이 없다면
        if (!owner.TryGetComponent<PlayerStatus>(out var ownerStat))
            return;

        #region 계산하는 부분을 클래스로 분리할 때 참고
        //// 발사체 스킬 인터페이스가 없다면
        //else if (owner.GetComponentInChildren<IProjectileSkill>(true) is not IProjectileSkill executer)
        //{
        //    Debug.Log($"[Error | Skill] 발사체 스킬 실행 실패 => 발사체 스킬 인터페이스 : 없음");
        //    return;
        //}
        //// 발사체 스킬 인터페이스가 있다면
        //else
        //    // 스킬 실행
        //    executer.ExecuteSkill(id, this);
        #endregion

        // 변화량을 저장할 변수
        float amount;

        // 바꿀 스탯의 수만큼
        foreach (var stat in appliedStats)
        {
            // 변화량 구하기
            amount = stat.modify == MODIFY_TYPE.Addition || stat.modify == MODIFY_TYPE.Multiplier ?
                                                                                stat.amount : -stat.amount;

            // 이전 레벨 스탯이 있다면
            if(prevStats != null)
            {
                // 이전 변화량을 저장할 변수
                float prevAmount = 0;

                // 바꿨던 스탯의 수만큼
                foreach(var prev in prevStats)
                    // 같은 스탯을 찾았다면
                    if (prev.stat == stat.stat)
                    {
                        // 이전 변화량 구하기
                        prevAmount = prev.modify == MODIFY_TYPE.Addition
                                        || prev.modify == MODIFY_TYPE.Multiplier ? prev.amount : -prev.amount;
                        break;
                    }

                // 이전 변화량이 있다면
                if (prevAmount != 0)
                    // 변화량에 반영
                    amount -= prevAmount;
            }

            // 스탯 종류에 따라서
            switch (stat.stat)
            {
                // 체력이라면
                case STAT_TYPE.Health:
                    // 수식 종류가 곱하기라면
                    if (stat.modify == MODIFY_TYPE.Multiplier)
                    {
                        // 변화량이 100 + N%로 작성되었다면
                        if (amount >= 1f)
                            // 100% 제거
                            amount -= 1f;

                        // 실제 변화량 구하기
                        amount = ownerStat.Status.MaxHP.BaseValue * amount;
                    }

                    // 최대 체력 변경
                    ownerStat.AddMaxHPValue(amount);
                    break;
                // 공격력이라면
                case STAT_TYPE.AtkPower:
                    if (stat.modify == MODIFY_TYPE.Multiplier)
                    {
                        if (amount >= 1f)
                            amount -= 1f;

                        amount = ownerStat.Status.AttackPower.BaseValue * amount;
                    }

                    // 공격력 변경
                    ownerStat.AddAttackPowerValue(amount);
                    break;
                // 이동 속도라면
                case STAT_TYPE.MoveSpeed:
                    if (stat.modify == MODIFY_TYPE.Multiplier)
                    {
                        if (amount >= 1f)
                            amount -= 1f;

                        amount = ownerStat.Status.MoveSpeed.BaseValue * amount;
                    }

                    // 이동 속도 변경
                    ownerStat.AddMoveSpeedValue(amount);
                    break;
                // 공격 속도라면
                case STAT_TYPE.AtkSpeed:
                    if (stat.modify == MODIFY_TYPE.Multiplier)
                    {
                        if (amount >= 1f)
                            amount -= 1f;

                        amount = ownerStat.Status.AttackSpeed.BaseValue * amount;
                    }

                    // 공격 속도 변경
                    ownerStat.AddAttackSpeedValue(amount);
                    break;
            }
        }
    }

    // 스킬 효과 적용 해제 함수
    public override void RemoveEffect(GameObject owner)
    {
        // 소유자가 없다면
        if (owner == null)
            return;

        // 스탯이 없다면
        if (!owner.TryGetComponent<PlayerStatus>(out var ownerStat))
            return;

        #region 계산하는 부분을 클래스로 분리할 때 참고
        //// 발사체 스킬 인터페이스가 없다면
        //else if (owner.GetComponentInChildren<IProjectileSkill>(true) is not IProjectileSkill executer)
        //{
        //    Debug.Log($"[Error | Skill] 발사체 스킬 실행 실패 => 발사체 스킬 인터페이스 : 없음");
        //    return;
        //}
        //// 발사체 스킬 인터페이스가 있다면
        //else
        //    // 스킬 실행
        //    executer.ExecuteSkill(id, this);
        #endregion

        // 변화량을 저장할 변수
        float amount;

        // 바꿀 스탯의 수만큼
        foreach (var stat in appliedStats)
        {
            // 변화량 구하기
            amount = stat.modify == MODIFY_TYPE.Addition || stat.modify == MODIFY_TYPE.Multiplier ?
                                                                                -stat.amount : stat.amount;

            // 스탯 종류에 따라서
            switch (stat.stat)
            {
                // 체력이라면
                case STAT_TYPE.Health:
                    // 수식 종류가 곱하기라면
                    if (stat.modify == MODIFY_TYPE.Multiplier)
                    {
                        // 변화량이 100 + N%로 작성되었다면
                        if (amount <= -1f)
                            // 100% 제거
                            amount += 1f;

                        // 실제 변화량 구하기
                        amount = ownerStat.Status.MaxHP.BaseValue * amount;
                    }

                    // 최대 체력 변경
                    ownerStat.AddMaxHPValue(amount);
                    break;
                // 공격력이라면
                case STAT_TYPE.AtkPower:
                    if (stat.modify == MODIFY_TYPE.Multiplier)
                    {
                        if (amount <= -1f)
                            amount += 1f;

                        amount = ownerStat.Status.AttackPower.BaseValue * amount;
                    }

                    // 공격력 변경
                    ownerStat.AddAttackPowerValue(amount);
                    break;
                // 이동 속도라면
                case STAT_TYPE.MoveSpeed:
                    if (stat.modify == MODIFY_TYPE.Multiplier)
                    {
                        if (amount <= -1f)
                            amount += 1f;

                        amount = ownerStat.Status.MoveSpeed.BaseValue * amount;
                    }

                    // 이동 속도 변경
                    ownerStat.AddMoveSpeedValue(amount);
                    break;
                // 공격 속도라면
                case STAT_TYPE.AtkSpeed:
                    if (stat.modify == MODIFY_TYPE.Multiplier)
                    {
                        if (amount <= -1f)
                            amount += 1f;

                        amount = ownerStat.Status.AttackSpeed.BaseValue * amount;
                    }

                    // 공격 속도 변경
                    ownerStat.AddAttackSpeedValue(amount);
                    break;
            }
        }
    }
}