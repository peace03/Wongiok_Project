using UnityEngine;
using System.Collections.Generic;

public class SkillSystemModel
{
    [SerializeField] private CHAPTER_TYPE curChapter = CHAPTER_TYPE.First;      // 현재 챕터
    [SerializeField] private int maxActiveCount = 3;                            // 액티브 스킬 최대 장착 개수
    [SerializeField] private List<SkillInstance> equippedActives = new();       // 장착된 액티브 스킬들
    [SerializeField] private int maxPassiveCount = 4;                           // 패시브 스킬 최대 장착 개수
    [SerializeField] private List<SkillInstance> equippedPassives = new();      // 장착된 패시브 스킬들
    [SerializeField] private List<SkillInstance> unequippedActives = new();     // 미장착 액티브 스킬들

    private Dictionary<int, SkillInstance> allSkills = new();                   // 모든 스킬들

    public SkillSystemModel()
    {
        //SkillDatabase.
    }
}