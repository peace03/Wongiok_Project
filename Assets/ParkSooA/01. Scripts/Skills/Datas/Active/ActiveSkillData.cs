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

    /// <summary>
    /// 스킬 객체 생성 함수
    /// </summary>
    /// <param name="owner">스킬 소유자</param>
    public override SkillInstance CreateInstance(GameObject owner) => new(owner, this);

    /// <summary>
    /// 최대 쿨타임 반환 함수
    /// </summary>
    /// <param name="level">스킬 레벨</param>
    public override float GetMaxCoolTime(int level)
    {
        // 레벨에 맞는 정보 가져오기
        var data = GetLevelData(level);

        // 정보가 없다면
        if (data == null)
        {
            Debug.Log($"[Error | Skill] 스킬 쿨타임 받아오기 실패 => 입력 - 레벨 : {level} / 정보 : 없음");
            return base.GetMaxCoolTime(level);
        }

        // 최대 쿨타임 반환
        return data.MaxCoolTime;
    }

    /// <summary>
    /// 최대 지속 시간 반환 함수
    /// </summary>
    /// <param name="level">스킬 레벨</param>
    public override float GetMaxDuration(int level)
    {
        // 레벨에 맞는 정보 가져오기
        var data = GetLevelData(level);

        // 정보가 없다면
        if (data == null)
        {
            Debug.Log($"[Error | Skill] 스킬 지속 시간 받아오기 실패 => 입력 - 레벨 : {level} / 정보 : 없음");
            return base.GetMaxDuration(level);
        }

        // 최대 지속 시간 반환
        return data.MaxDuration;
    }

    /// <summary>
    /// 최대 차징 시간 반환 함수
    /// </summary>
    /// <param name="level">스킬 레벨</param>
    public override float GetMaxChargingTime(int level)
    {
        // 레벨에 맞는 정보 가져오기
        var data = GetLevelData(level);

        // 정보가 없다면
        if (data == null)
        {
            Debug.Log($"[Error | Skill] 스킬 차징 시간 받아오기 실패 => 입력 - 레벨 : {level} / 정보 : 없음");
            return base.GetMaxChargingTime(level);
        }
        // 발사체 정보가 아니라면
        else if (data is not ProjectileSkillLevelData levelData)
            return base.GetMaxChargingTime(level);
        // 발사체 정보라면
        else
            // 최대 지속 시간 반환
            return levelData.MaxChargingTime;
    }

    /// <summary>
    /// 데미지 반환 함수
    /// </summary>
    /// <param name="level">스킬 레벨</param>
    /// <param name="stage">스킬 단계</param>
    public float GetDamageByLevel(int level, int stage = 0)
    {
        // 레벨에 맞는 정보 가져오기
        var data = GetLevelData(level);

        // 정보가 없다면
        if (data == null)
        {
            Debug.Log($"[Error | Skill] 데미지 받아오기 실패 => 입력 - 레벨 : {level} / 정보 : 없음");
            return 0f;
        }

        // 데미지 반환
        return data.GetDamage(stage);
    }

    /// <summary>
    /// 이펙트 정렬 함수
    /// </summary>
    public void SortEffects()
    {
        // 이펙트가 없거나, 이펙트의 개수가 없다면
        if (effects == null || effects.Count == 0)
            return;

        // 이펙트 정렬 시작
        effects.Sort((a, b) =>
        {
            // 이펙트 종류 비교 결과값 받아오기
            int typeValue = a.type.CompareTo(b.type);
            // 같은 종류가 아니라면 종류별 오름차순(작 -> 큰) 반환 : 같은 종류라면 중요도별 내림차순(큰 -> 작) 반환
            return typeValue != 0 ? typeValue : b.priority.CompareTo(a.priority);
        });
    }
}