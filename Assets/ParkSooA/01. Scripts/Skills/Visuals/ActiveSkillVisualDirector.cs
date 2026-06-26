using UnityEngine;
using System.Collections.Generic;

public class ActiveSkillVisualDirector : MonoBehaviour
{
    [Header("무기 컨테이너")]
    [Tooltip("무기 모와두는 곳")]
    [SerializeField] private Transform weaponContainer;                      // 무기 컨테이너
    [Header("이펙트 컨테이너")]
    [Tooltip("이펙트 모와두는 곳")]
    [SerializeField] private Transform effectContainer;                     // 이펙트 컨테이너

    private Dictionary<int, GameObject> weapons = new();                    // 모든 무기들
    private Dictionary<int, List<ActiveSkillEffect>> effects = new();       // 모든 이펙트들

    // 액티브 스킬 이펙트 실행 이벤트 구독
    private void OnEnable() => EventBus<ExecuteActiveSkillEffect>.action += ExecuteEffect;

    // 액티브 스킬 이펙트 실행 이벤트 구독 해제
    private void OnDisable() => EventBus<ExecuteActiveSkillEffect>.action -= ExecuteEffect;

    public void AddWeaponAndEffect(ActiveSkillData data)
    {
        if(!weapons.ContainsKey(data.Id))
        {
            var weapon = Instantiate(data.Weapon, weaponContainer);
            weapons[data.Id] = weapon;
            weapon.SetActive(false);
        }

        if(!effects.ContainsKey(data.Id))
        {
            GameObject effectPrefab;
            List<ActiveSkillEffect> effectList = new();

            foreach(var effect in data.Effects)
            {
                effectPrefab = Instantiate(effect.prefab, effectContainer);
                ActiveSkillEffect skillEffect = effect;
                skillEffect.prefab = effectPrefab;
                effectPrefab.SetActive(false);
                effectList.Add(skillEffect);
            }

            effects[data.Id] = effectList;
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
            // 이펙트 종류가 같다면
            if (effect.type == execute.type)
            {
                // 타겟(과녁) 이펙트가 아니라면
                if (effect.type != ACTIVE_SKILL_EFFECT_TYPE.Target)
                {
                    // 이펙트 오브젝트 활성화
                    effect.prefab.SetActive(true);        // 나중에 이펙트 스크립트의 실행 함수로 바꾸기
                    Debug.Log($"[Effect] {effect.type.ToKoreanString()} 실행");
                }
                // 이펙트 실행 위치가 비어있다면
                else if (execute.position == null)
                    continue;
                // 타겟(과녁) 이펙트라면
                else
                {
                    // 실행 위치 설정
                    effect.prefab.transform.position = (Vector3)execute.position;
                    // 오브젝트 활성화
                    effect.prefab.SetActive(true);     // 나중에 이펙트 스크립트의 실행 함수로 바꾸기
                    Debug.Log($"[Effect] {effect.type.ToKoreanString()} 실행");
                }
            }
    }
}