using UnityEngine;
using System;
using System.Collections.Generic;

[Serializable]
// 범위 액티브 스킬 레벨 정보
public class AreaSkillLevelData : ActiveSkillLevelData
{
    [Header("스킬 단계")]
    [SerializeField] private List<AreaSkillStageData> stages;       // 스킬 단계

    public IReadOnlyList<AreaSkillStageData> Stages => stages;

    // 액티브 스킬 종류 반환 프로퍼티
    public override ACTIVE_SKILL_TYPE ActiveType => ACTIVE_SKILL_TYPE.Area;

    // 데미지 반환 함수
    public override float GetDamage(int stage)
    {
        // 범위에서 벗어난 단계라면
        if(stage < 1 || stage > stages.Count)
            return 0f;

        // 단계에 해당하는 데미지 반환
        return stages[stage - 1].damage;
    }

    // 스킬 효과 적용 함수
    public override void ApplyEffect(GameObject owner, int id, IReadOnlyList<StatAdjustment> prevStats)
    {
        // 스킬 실행기를 담을 변수
        IAreaSkill executer;

        // 소유자가 없거나, 범위 스킬 인터페이스가 없다면
        if (owner == null || (executer = owner.GetComponentInChildren<IAreaSkill>()) == null)
            return;

        // 스킬 실행
        executer.ExecuteSkill(id, this);
    }
}