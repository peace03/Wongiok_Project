using UnityEngine;
using System;
using System.Collections.Generic;

[Serializable]
// 영역 액티브 스킬 레벨 정보
public class AreaSkillLevelData : ActiveSkillLevelData
{
    [SerializeField] private List<AreaSkillStageData> stages;       // 스킬 단계

    public IReadOnlyList<AreaSkillStageData> Stages => stages;

    // 액티브 스킬 종류 반환 프로퍼티
    public override ACTIVE_SKILL_TYPE ActiveType => ACTIVE_SKILL_TYPE.Area;

    // 데미지 반환 함수
    public override float GetDamage(int stage)
    {
        // 범위에서 벗어난 단계라면
        if(stage < 1 || stage > stages.Count)
        {
            Debug.Log($"[Error | Skill] 해당 데이터 없음 => 입력 : 단계({stage}) | 스킬 단계(1 ~ {stages.Count})");
            return 0f;
        }

        // 단계에 해당하는 데미지 반환
        return stages[stage - 1].damage;
    }

    public override void ApplyEffect(GameObject target)
    {
        Debug.Log("[Skill] 영역 액티브 스킬 공격");
    }
}