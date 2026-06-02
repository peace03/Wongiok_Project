using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "New ActiveSkillData", menuName = "Data/Skill/Active", order = 1)]
// 액티브 스킬 정보
public class ActiveSkillData : LevelBasedSkillData<ActiveSkillLevelData>
{
    [SerializeField] private GameObject weapon;                         // 무기 프리팹
    [SerializeField] private List<GameObject> effects;                  // 효과 프리팹들

    public GameObject Weapon => weapon;
    public IReadOnlyList<GameObject> Effects => effects;

    // 객체 생성 함수
    public override SkillInstance CreateInstance()
    {
        return null;
    }

    // 레벨별 데미지 반환 함수
    public float GetDamageByLevel(int level, int stage = 0) => GetLevelData(level).GetDamage(stage);
}