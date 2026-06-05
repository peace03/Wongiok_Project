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
    public override SkillInstance CreateInstance() => new(this);

    // 최대 쿨타임 반환 함수
    public override float GetMaxCoolTime(int level) => GetLevelData(level).MaxCoolTime;

    // 최대 지속 시간 반환 함수
    public override float GetMaxDuration(int level) => GetLevelData(level).MaxDuration;

    // 최대 차징 시간 반환 함수
    public override float GetMaxChargingTime(int level) => GetLevelData(level).MaxChargingTime;

    // 레벨 데미지 반환 함수
    public float GetDamageByLevel(int level, int stage = 0)
    {
        // 레벨에 맞는 데이터 가져오기
        var data = GetLevelData(level);

        // 데이터가 없다면
        if (data == null)
            return 0f;

        return data.GetDamage(stage);
    }
}