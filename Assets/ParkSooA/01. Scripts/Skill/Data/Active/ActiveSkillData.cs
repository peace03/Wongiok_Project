using UnityEngine;
using System.Collections.Generic;

public class ActiveSkillData : BaseSkillData
{
    [SerializeField] private GameObject weapon;                         // 무기 프리팹
    [SerializeField] private List<GameObject> effects;                  // 효과 프리팹들
    [SerializeField] private ACTIVE_SKILL_TYPE activeType;              // 액티브 스킬 종류
    [SerializeReference] private List<ActiveSkillLevelData> levels;     // 레벨별 정보들

    public override SkillInstance CreateInstance()
    {
        return null;
    }

    //public float GetDamage(int level, int stage = 0)
    //{

    //}
}