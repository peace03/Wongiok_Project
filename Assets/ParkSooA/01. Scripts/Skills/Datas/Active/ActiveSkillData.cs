using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "New ActiveSkillData", menuName = "Data/Skill/Active", order = 1)]
// 액티브 스킬 정보
public class ActiveSkillData : LevelBasedSkillData<ActiveSkillLevelData>
{
    [Header("무기 프리팹")]
    [SerializeField] private GameObject weapon;                     // 무기 프리팹
    [Header("스킬 이펙트들")]
    [SerializeField] private List<ActiveSkillEffect> effects;       // 스킬 이펙트들

    public GameObject Weapon => weapon;
    public IReadOnlyList<ActiveSkillEffect> Effects => effects;

    // 객체 생성 함수
    public override SkillInstance CreateInstance(GameObject owner) => new(owner, this);

    // 최대 쿨타임 반환 함수
    public override float GetMaxCoolTime(int level)
    {
        // 레벨에 맞는 정보 가져오기
        var data = GetLevelData(level);

        // 정보가 없다면
        if (data == null)
        {
            Debug.LogError($"[Error | Skill] 스킬 쿨타임 받아오기 실패 => 입력 - 레벨 : {level} / 정보 : 없음");
            return base.GetMaxCoolTime(level);
        }

        // 최대 쿨타임 반환
        return data.MaxCoolTime;
    }

    // 최대 지속 시간 반환 함수
    public override float GetMaxDuration(int level)
    {
        // 레벨에 맞는 정보 가져오기
        var data = GetLevelData(level);

        // 정보가 없다면
        if (data == null)
        {
            Debug.LogError($"[Error | Skill] 스킬 지속 시간 받아오기 실패 => 입력 - 레벨 : {level} / 정보 : 없음");
            return base.GetMaxDuration(level);
        }

        // 최대 지속 시간 반환
        return data.MaxDuration;
    }

    // 최대 차징 시간 반환 함수
    public override float GetMaxChargingTime(int level)
    {
        // 레벨에 맞는 정보 가져오기
        var data = GetLevelData(level);

        // 정보가 없다면
        if (data == null)
        {
            Debug.LogError($"[Error | Skill] 스킬 차징 시간 받아오기 실패 => 입력 - 레벨 : {level} / 정보 : 없음");
            return base.GetMaxChargingTime(level);
        }
        // 발사체 정보가 아니라면
        else if (data is not ProjectileSkillLevelData levelData)
        {
            Debug.LogError($"[Error | Skill] 스킬 차징 시간 받아오기 실패 => 입력 - 레벨 : {level} / 정보 : 발사체 정보 아님");
            return base.GetMaxChargingTime(level);
        }
        // 발사체 정보라면
        else
            // 최대 지속 시간 반환
            return levelData.MaxChargingTime;
    }

    // 데미지 반환 함수
    public float GetDamageByLevel(int level, int stage = 0)
    {
        // 레벨에 맞는 정보 가져오기
        var data = GetLevelData(level);

        // 정보가 없다면
        if (data == null)
        {
            Debug.LogError($"[Error | Skill] 데미지 받아오기 실패 => 입력 - 레벨 : {level} / 정보 : 없음");
            return 0f;
        }

        // 데미지 반환
        return data.GetDamage(stage);
    }
}