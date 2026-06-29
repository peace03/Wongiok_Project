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
    }

    private void OnDisable()
    {
        // 액티브 스킬 장착 이벤트 구독 해제
        EventBus<EquippedActiveSkill>.action -= AddWeaponAndEffect;
        // 액티브 스킬 이펙트 실행 이벤트 구독 해제
        EventBus<ExecuteActiveSkillEffect>.action -= ExecuteEffect;
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
    public void ExecuteEffect(ExecuteActiveSkillEffect executeEffect)
    {
        // 실행할 이펙트가 없다면
        if (!effects.TryGetValue(executeEffect.id, out var skillEffect))
        {
            Debug.Log($"[Effect] 이펙트 실행 실패 => 입력 - 스킬 ID : {executeEffect.id} / " +
                        $"이펙트 종류 : {executeEffect.type.ToKoreanString()} / 실행할 이펙트 : 없음");
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
            else if (effect.type != executeEffect.type)
                continue;

            // 이펙트 실행 위치가 비어있지 않다면
            if (executeEffect.pos != null)
                // 실행 위치 설정
                effect.prefab.transform.position = (Vector3)executeEffect.pos;

            //이펙트 실행
            executer.Execute(executeEffect.duration);
        }
    }
}