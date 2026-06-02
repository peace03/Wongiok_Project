using UnityEngine;

// 스킬 기본 정보
public abstract class BaseSkillData : ScriptableObject
{
    [SerializeField] private int id;                    // ID
    [SerializeField] private Sprite icon;               // 아이콘
    [SerializeField] private string skillName;          // 이름
    [SerializeField] private SKILL_TYPE type;           // 스킬 종류
    [SerializeField] private CHAPTER_TYPE unlock;       // 해금 챕터 종류
    [SerializeField] private int maxLevel;              // 최대 레벨
    [SerializeField] private string desc;               // 설명

    public int Id => id;
    public Sprite Icon => icon;
    public string SkillName => skillName;
    public SKILL_TYPE Type => type;
    public CHAPTER_TYPE Unlock => unlock;
    public int MaxLevel => maxLevel;
    public string Desc => desc;

    // 객체 생성 함수
    public abstract SkillInstance CreateInstance();
}