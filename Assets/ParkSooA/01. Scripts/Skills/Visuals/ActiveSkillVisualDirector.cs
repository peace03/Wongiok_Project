using System.Collections.Generic;
using UnityEngine;

public class ActiveSkillVisualDirector : MonoBehaviour
{
    [Header("무기 컨테이너")]
    [Tooltip("무기 모와두는 곳")]
    [SerializeField] private Transform weaponContainer;                     // 무기 컨테이너
    [Header("이펙트 컨테이너")]
    [Tooltip("이펙트 모와두는 곳")]
    [SerializeField] private Transform effectContainer;                     // 이펙트 컨테이너

    private readonly Dictionary<int, GameObject> weapons = new();           // 모든 무기 딕셔너리
    private readonly Dictionary<int, List<ActiveSkillEffect>> effects       // 모든 이펙트 정보 딕셔너리
                                                                 = new();

    private void OnEnable()
    {
        // 액티브 스킬 장착 이벤트 구독
        EventBus<EquippedActiveSkill>.action += AddWeaponAndEffect;
        // 액티브 스킬 이펙트 실행 이벤트 구독
        EventBus<ExecuteActiveSkillEffect>.action += ExecuteEffect;
        // 액티브 스킬 이펙트 종료 이벤트 구독
        EventBus<StopActiveSkillEffect>.action += StopEffect;
        // 액티브 스킬 이펙트 초기화 이벤트 구독
        EventBus<ResetActiveSkillEffect>.action += ResetEffect;
    }

    private void OnDisable()
    {
        // 액티브 스킬 장착 이벤트 구독 해제
        EventBus<EquippedActiveSkill>.action -= AddWeaponAndEffect;
        // 액티브 스킬 이펙트 실행 이벤트 구독 해제
        EventBus<ExecuteActiveSkillEffect>.action -= ExecuteEffect;
        // 액티브 스킬 이펙트 종료 이벤트 구독 해제
        EventBus<StopActiveSkillEffect>.action -= StopEffect;
        // 액티브 스킬 이펙트 초기화 이벤트 구독 해제
        EventBus<ResetActiveSkillEffect>.action -= ResetEffect;
    }

    /// <summary>
    /// 무기, 이펙트 생성 및 추가 함수
    /// </summary>
    public void AddWeaponAndEffect(EquippedActiveSkill skill)
    {
        // ID에 해당하는 무기가 없다면
        if(!weapons.ContainsKey(skill.id))
        {
            // 무기 프리팹이 있다면
            if(skill.weapon != null)
            {
                // 무기 오브젝트 생성 후 딕셔너리에 저장
                weapons[skill.id] = Instantiate(skill.weapon, weaponContainer);
                // 무기 비활성화
                weapons[skill.id].SetActive(false);
                Debug.Log($"[Weapon] 무기 외형 저장 완료 => 입력 - 스킬 ID : {skill.id}", weaponContainer);
            }
        }

        // ID에 해당하는 이펙트 정보들이 없다면
        if(!effects.ContainsKey(skill.id))
        {
            // 이펙트 오브젝트를 저장할 변수
            GameObject spawnPrefab;
            // 이펙트 정보들을 저장할 리스트
            List<ActiveSkillEffect> effectList = new();

            // 이펙트들의 수만큼
            for(int i = 0; i < skill.effects.Count; i++)
            {
                // 이펙트 오브젝트 생성
                spawnPrefab = Instantiate(skill.effects[i].prefab, effectContainer);
                // 이펙트 비활성화
                spawnPrefab.SetActive(false);
                // 생성한 이펙트 오브젝트를 기준으로 이펙트 정보 설정
                ActiveSkillEffect effect = new()
                {
                    type = skill.effects[i].type,
                    priority = skill.effects[i].priority,
                    prefab = spawnPrefab
                };
                // 이펙트 정보 저장
                effectList.Add(effect);
            }

            // 저장할 이펙트 정보가 있다면
            if(effectList.Count != 0)
            {
                // 이펙트 정보들을 딕셔너리에 저장
                effects[skill.id] = effectList;
                Debug.Log($"[Effect] 이펙트 정보 저장 완료 => 입력 - 스킬 ID : {skill.id}", effectContainer);
            }
        }
    }

    /// <summary>
    /// 이펙트 실행 함수
    /// </summary>
    public void ExecuteEffect(ExecuteActiveSkillEffect execute)
    {
        // 실행할 이펙트가 없다면
        if (!effects.TryGetValue(execute.id, out var skillEffect))
        {
            Debug.Log($"[Effect] 이펙트 실행 실패 => 입력 - 스킬 ID : {execute.id} / " +
                        $"이펙트 종류 : {execute.type.ToKoreanString()} / 실행할 이펙트 : 없음");
            return;
        }

        // 스킬 이펙트들의 수만큼
        foreach (var effect in skillEffect)
        {
            // 실행자 인터페이스가 없다면
            if (!effect.prefab.TryGetComponent<IEffectExecuter>(out var executer))
            {
                Debug.Log($"[Effect] 이펙트 실행 실패 => " +
                            $"입력 - 이펙트 종류 : {effect.type.ToKoreanString()} / " +
                            $"이펙트 실행자 : 없음", effect.prefab);
                continue;
            }
            // 이펙트 종류가 다르다면
            else if (effect.type != execute.type)
                continue;

            // 이펙트 실행 위치가 비어있지 않다면
            if (execute.pos != null)
                // 실행 위치 설정
                effect.prefab.transform.position = (Vector3)execute.pos;

            // 실행할 이펙트의 종류에 따라서
            switch (execute.type)
            {
                // 차징 이펙트라면
                case ACTIVE_SKILL_EFFECT_TYPE.Charging:
                // 궤적 이펙트라면
                case ACTIVE_SKILL_EFFECT_TYPE.Trail:
                // 메인(총알, 영역) 이펙트 라면
                case ACTIVE_SKILL_EFFECT_TYPE.Main:
                // 타겟(과녁) 이펙트라면
                case ACTIVE_SKILL_EFFECT_TYPE.Target:
                    // 이펙트 실행
                    executer.ExecuteEffect();
                    break;
                // 총구 이펙트라면
                case ACTIVE_SKILL_EFFECT_TYPE.Muzzle:
                // 타격(피격) 이펙트라면
                case ACTIVE_SKILL_EFFECT_TYPE.Hit:
                    // 이펙트 실행 후 자동 종료
                    executer.ExecuteEffect(execute.duration == null ? 0.1f
                                                                        : (float)execute.duration);
                    break;
            }
        }
    }

    /// <summary>
    /// 이펙트 종료 함수
    /// </summary>
    public void StopEffect(StopActiveSkillEffect stop)
    {
        // 종료할 이펙트가 없다면
        if (!effects.TryGetValue(stop.id, out var skillEffect))
        {
            Debug.Log($"[Effect] 이펙트 종료 실패 => 입력 - 스킬 ID : {stop.id} / " +
                        $"이펙트 종류 : {stop.type.ToKoreanString()} / 종료할 이펙트 : 없음");
            return;
        }

        // 스킬 이펙트들의 수만큼
        foreach (var effect in skillEffect)
        {
            // 실행자 인터페이스가 없다면
            if (!effect.prefab.TryGetComponent<IEffectExecuter>(out var executer))
            {
                Debug.Log($"[Effect] 이펙트 종료 실패 => " +
                            $"입력 - 이펙트 종류 : {effect.type.ToKoreanString()} / " +
                            $"이펙트 실행자 : 없음", effect.prefab);
                continue;
            }
            // 이펙트 종류가 다르다면
            else if (effect.type != stop.type)
                continue;

            // 이펙트 종료
            executer.StopEffect();
        }
    }

    /// <summary>
    /// 이펙트 초기화 함수
    /// </summary>
    public void ResetEffect(ResetActiveSkillEffect reset)
    {
        // 초기화할 이펙트가 없다면
        if (!effects.TryGetValue(reset.id, out var skillEffect))
        {
            Debug.Log($"[Effect] 이펙트 초기화 실패 => 입력 - 스킬 ID : {reset.id} / " +
                        $"이펙트 종류 : {reset.type.ToKoreanString()} / 초기화할 이펙트 : 없음");
            return;
        }

        // 스킬 이펙트들의 수만큼
        foreach (var effect in skillEffect)
        {
            // 실행자 인터페이스가 없다면
            if (!effect.prefab.TryGetComponent<IEffectExecuter>(out var executer))
            {
                Debug.Log($"[Effect] 이펙트 초기화 실패 => " +
                            $"입력 - 이펙트 종류 : {effect.type.ToKoreanString()} / " +
                            $"이펙트 실행자 : 없음", effect.prefab);
                continue;
            }
            // 이펙트 종류가 다르다면
            else if (effect.type != reset.type)
                continue;

            // 이펙트 초기화
            executer.ResetEffect();
        }
    }
}