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

    // 이펙트 종류별 정보(범위) 딕셔너리(시작 위치, 개수)
    private readonly Dictionary<ACTIVE_SKILL_EFFECT_TYPE, (int index, int count)> effectRanges = new();

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

        // 이펙트 종류별 정보(범위) 설정하기
        SetEffectRanges();
    }

    /// <summary>
    /// 이펙트 종류별 정보(범위) 설정 함수
    /// </summary>
    private void SetEffectRanges()
    {
        // 이펙트 종류별 정보(범위) 초기화
        effectRanges.Clear();

        // 이펙트가 없거나, 비어있다면
        if (effects == null || effects.Count == 0)
            return;

        // 이펙트 첫번째 종류 받아오기
        ACTIVE_SKILL_EFFECT_TYPE curType = effects[0].type;
        // 이펙트 위치 변수
        int index = 0;
        // 이펙트 개수 변수
        int count = 0;

        // 이펙트들의 수만큼
        for(int i = 0; i < effects.Count; i++)
        {
            if (effects[i].prefab == null)
                Debug.LogWarning($"[Skill] 이펙트 프리팹 없음 => 입력 - 스킬 ID : {Id} / " +
                                    $"스킬 이름 : {SkillName} / " +
                                    $"이펙트 종류 : {effects[i].type.ToKoreanString()}");

            // 이펙트 종류가 같다면
            if (effects[i].type == curType)
                // 이펙트 개수 증가
                count++;
            // 이펙트 종류가 다르다면
            else
            {
                // 현재 이펙트 종류의 시작 위치와 개수 저장하기
                effectRanges[curType] = (index, count);
                // 이펙트 종류 변경
                curType = effects[i].type;
                // 시작 위치 변경
                index = i;
                // 개수 초기화
                count = 1;
            }
        }

        // 마지막 이펙트 종류의 시작 위치와 개수 저장하기
        effectRanges[curType] = (index, count);
    }

    /// <summary>
    /// 이펙트 종류별 이펙트들 반환 함수
    /// </summary>
    /// <param name="type">이펙트 종류</param>
    /// <param name="results">이펙트 프리팹이 들어갈 리스트</param>
    public void GetEffectsByEffectType(ACTIVE_SKILL_EFFECT_TYPE type, List<GameObject> results)
    {
        // 리스트가 없다면
        if(results == null)
        {
            Debug.Log($"[Error | Skill] 액티브 스킬 이펙트 반환 실패 => 입력 - 리스트 : 없음");
            return;
        }
        // 스킬 이펙트 리스트가 없거나, 비어있다면
        else if(effects == null || effects.Count == 0)
        {
            Debug.Log($"[Error | Skill] 액티브 스킬 이펙트 반환 실패 => 입력 - 스킬 ID : {Id} / " +
                        $"스킬 이름 : {SkillName} / 스킬 이펙트 : 없음");
            return;
        }
        
        // 리스트 초기화
        results.Clear();

        // 이펙트 종류별 정보(범위)가 없다면
        if (!effectRanges.TryGetValue(type, out var range))
            return;

        // 이펙트 종류의 시작 위치에서, 이펙트의 개수만큼
        for (int i = range.index; i < range.index + range.count; i++)
            // 이펙트 프리팹을 결과 리스트에 추가
            results.Add(effects[i].prefab);
    }
}