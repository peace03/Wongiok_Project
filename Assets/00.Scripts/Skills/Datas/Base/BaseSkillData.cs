using UnityEngine;

// 스킬 기본 정보
public abstract class BaseSkillData : ScriptableObject
{
    [SerializeField] private int id;                            // ID
    [SerializeField] private Sprite icon;                       // 아이콘
    [SerializeField] private string skillName;                  // 이름
    [SerializeField] private SKILL_TYPE type;                   // 스킬 종류
    [SerializeField] private CHAPTER_TYPE unlockChapter;        // 해금 챕터 종류
    [SerializeField] private int maxLevel;                      // 최대 레벨
    [SerializeField] private string desc;                       // 설명

    public int Id => id;
    public Sprite Icon => icon;
    public string SkillName => skillName;
    public SKILL_TYPE Type => type;
    public CHAPTER_TYPE UnlockChapter => unlockChapter;
    public int MaxLevel => maxLevel;
    public string Desc => desc;

    // 객체 생성 함수
    public abstract SkillInstance CreateInstance(GameObject owner);

    // 스킬 실행 함수
    public abstract void ExecuteSkill(GameObject owner, int level);

    // 스킬 취소 함수
    public abstract void CancelSkill(GameObject owner, int level);

    // 최대 쿨타임 반환 함수
    public virtual float GetMaxCoolTime(int level) => 0f;

    // 최대 지속 시간 반환 함수
    public virtual float GetMaxDuration(int level) => 0f;

    // 최대 차징 시간 반환 함수
    public virtual float GetMaxChargingTime(int level) => 0f;
}