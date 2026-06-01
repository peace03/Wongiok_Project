using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "New PassiveSkillData", menuName = "Data/Skill/Passive", order = 2)]
// 패시브 스킬 정보
public class PassiveSkillData : BaseSkillData
{
    [SerializeField] private List<PassiveLevelData> levelDatas;     // 레벨별 정보들

    // 객체 생성 함수
    public override SkillInstance CreateInstance()
    {
        return null;
    }

    // 레벨 정보 반환 함수
    public PassiveLevelData GetLevelData(int level)
    {
        return levelDatas[level];
    }
}