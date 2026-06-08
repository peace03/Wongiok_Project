using UnityEngine;
using System.Collections.Generic;

// 레벨이 있는 스킬 기본 정보
public abstract class LevelBasedSkillData<T> : BaseSkillData where T : BaseSkillLevelData
{
    [SerializeReference] protected List<T> levelDatas;      // 레벨별 정보들

    public IReadOnlyList<T> LevelDatas => levelDatas;

    // 스킬 사용 함수
    public override void ExecuteSkill(GameObject target, int level) => GetLevelData(level)?.ApplyEffect(target);

    // 스킬 취소 함수
    public override void CancelSkill(GameObject target, int level) => GetLevelData(level)?.RemoveEffect(target);

    // 레벨 정보 반환 함수
    public T GetLevelData(int level)
    {
        // 범위에서 벗어난 레벨이라면
        if (level < 1 || level > MaxLevel)
        {
            Debug.Log($"[Error | Skill] 해당하는 {typeof(T)} 없음 ⇒ 입력 - 레벨 : {level} / 범위 : 1 ~ {MaxLevel}");
            return null;
        }

        return levelDatas[level - 1];
    }
}