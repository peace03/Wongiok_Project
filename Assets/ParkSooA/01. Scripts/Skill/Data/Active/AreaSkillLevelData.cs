using UnityEngine;
using System.Collections.Generic;

// 영역 액티브 스킬 레벨 정보
public class AreaSkillLevelData : ActiveSkillLevelData
{
    [SerializeField] private List<AreaSkillStageData> stages;       // 스킬 단계
}