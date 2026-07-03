using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

public class EffectManager : MonoBehaviour
{
    // 모든 이펙트 오브젝트 풀 딕셔너리
    private readonly Dictionary<GameObject, IObjectPool<GameObject>> effects = new();

    /*
    private void OnEnable()
    {
        // 액티브 스킬 장착 이벤트 구독
        EventBus<EquippedActiveSkill>.action += AddActiveSkillEffects;
        // 이펙트 실행 이벤트 구독
        EventBus<ExecuteActiveSkillEffect>.action += ExecuteEffects;
        // 이펙트 종료 이벤트 구독
        EventBus<StopActiveSkillEffect>.action += StopEffects;
        // 이펙트 초기화 이벤트 구독
        EventBus<ResetActiveSkillEffect>.action += ResetEffects;
    }

    private void OnDisable()
    {
        // 액티브 스킬 장착 이벤트 구독 해제
        EventBus<EquippedActiveSkill>.action -= AddActiveSkillEffects;
        // 이펙트 실행 이벤트 구독 해제
        EventBus<ExecuteActiveSkillEffect>.action -= ExecuteEffects;
        // 이펙트 종료 이벤트 구독 해제
        EventBus<StopActiveSkillEffect>.action -= StopEffects;
        // 이펙트 초기화 이벤트 구독 해제
        EventBus<ResetActiveSkillEffect>.action -= ResetEffects;
    }

    /// <summary>
    /// 이펙트 오브젝트 풀 생성 및 딕셔너리 저장 함수
    /// </summary>
    private void AddActiveSkillEffects(EquippedActiveSkill skill)
    {
        // 이펙트 프리팹을 저장할 변수
        GameObject effect;

        // 이펙트들의 수만큼
        for (int i = 0; i < skill.effects.Count; i++)
        {
            // 이펙트 프리팹 저장
            effect = skill.effects[i].prefab;

            // 이펙트가 없다면
            if (effect == null)
            {
                Debug.Log($"[Effect] 이펙트 추가 실패 => 입력 - 스킬 ID : {skill.id} / " +
                            $"이펙트 종류 : {skill.effects[i].type.ToKoreanString()} / 이펙트 : 없음");
                continue;
            }
            // 이펙트 오브젝트 풀이 있다면
            else if (effects.ContainsKey(effect))
            {
                Debug.Log($"[Effect] 이펙트 추가 실패 => 입력 - 스킬 ID : {skill.id} / " +
                            $"이펙트 종류 : {skill.effects[i].type.ToKoreanString()} / " +
                            $"이펙트 : {effect} / 오브젝트 풀 : 있음");
                continue;
            }

            // 이펙트를 모와둘 곳 생성
            Transform effectContainer = new GameObject($"{effect.name}_그룹").transform;
            // 이펙트 오브젝트 풀 생성 및 딕셔너리에 저장
            effects[effect] = CustomObjectPool.CreatePool(effect, 100, effectContainer);
        }
    }

    /// <summary>
    /// 이펙트 실행 함수
    /// </summary>
    private void ExecuteEffects(ExecuteActiveSkillEffect execute)
    {
        // 실행할 이펙트가 없다면
        if (!effects.TryGetValue(execute.id, out var skillEffect))
            return;

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
                // 메인(총알, 범위) 이펙트 라면
                case ACTIVE_SKILL_EFFECT_TYPE.Main:
                    // 이펙트 실행
                    executer.ExecuteEffect();
                    break;
                // 타겟(과녁) 이펙트라면
                case ACTIVE_SKILL_EFFECT_TYPE.Target:
                    // 이펙트 실행 위치가 비어있지 않다면
                    if (execute.pos != null)
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
    private void StopEffects(StopActiveSkillEffect stop)
    {
        // 종료할 이펙트가 없다면
        if (!effects.TryGetValue(stop.id, out var skillEffect))
            return;

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
    private void ResetEffects(ResetActiveSkillEffect reset)
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
    */
}