using UnityEngine;
using System.Collections.Generic;

// 레벨이 있는 스킬 기본 정보
public abstract class LevelBasedSkillData<T> : BaseSkillData where T : BaseSkillLevelData
{
    [SerializeReference, SubclassSelector] protected List<T> levelDatas;      // 레벨별 정보들

    public IReadOnlyList<T> LevelDatas => levelDatas;

    // 스킬 사용 함수
    public override void ExecuteSkill(GameObject owner, int level)
        => GetLevelData(level)?.ApplyEffect(owner, level > 1 ? GetLevelData(level - 1)?.GetAppliedStats() : null);

    // 스킬 취소 함수
    public override void CancelSkill(GameObject owner, int level) => GetLevelData(level)?.RemoveEffect(owner);

    // 레벨 정보 반환 함수
    public T GetLevelData(int level)
    {
        // 리스트가 없다면
        if (levelDatas == null) 
        {
            Debug.LogError($"[Error | Skill] 레벨별 정보(리스트) 없음");
            return null;
        }
        // 범위에서 벗어난 레벨이라면
        else if (level < 1 || level > MaxLevel)
        {
            Debug.LogError($"[Error | Skill] 해당하는 {typeof(T)} 없음 => " +
                            $"입력 - 레벨 : {level} / 범위 : 1 ~ {MaxLevel} / 리스트 크기 : {levelDatas.Count}");
            return null;
        }

        // 레벨 정보 반환
        return levelDatas[level - 1];
    }
}