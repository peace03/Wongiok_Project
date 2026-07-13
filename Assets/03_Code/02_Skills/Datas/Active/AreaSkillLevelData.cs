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
        {
            Debug.Log($"[Error | Skill] 해당 데이터 없음 => 입력 : 단계 :{stage} / " +
                        $"스킬 단계 : 1 ~ {stages.Count}");
            return 0f;
        }

        // 단계에 해당하는 데미지 반환
        return stages[stage - 1].damage;
    }

    // 스킬 효과 적용 함수
    public override void ApplyEffect(GameObject owner, int id, IReadOnlyList<StatAdjustment> prevStats)
    {
        // 소유자가 없다면
        if (owner == null)
        {
            Debug.Log($"[Error | Skill] 범위 스킬 실행 실패 => 소유자 : 없음");
            return;
        }
        // 범위 스킬 인터페이스가 없다면
        else if (owner.GetComponentInChildren<IAreaSkill>(true) is not IAreaSkill executer)
        {
            Debug.Log($"[Error | Skill] 범위 스킬 실행 실패 => 범위 스킬 인터페이스 : 없음");
            return;
        }
        // 범위 스킬 인터페이스가 있다면
        else
            // 스킬 실행
            executer.ExecuteSkill(id, this);
    }
}