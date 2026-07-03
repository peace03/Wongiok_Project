using NUnit.Framework.Internal;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

public class EffectSingletonManager : MonoBehaviour
{
    public static EffectSingletonManager Instance { get; private set; }        // 싱글톤 인스턴스

    // 모든 이펙트 오브젝트 풀 딕셔너리
    private readonly Dictionary<GameObject, IObjectPool<GameObject>> effectPools = new();

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(this);
    }

    /// <summary>
    /// 이펙트 추가 함수
    /// </summary>
    /// <param name="prefab">추가할 이펙트 프리팹</param>
    /// <param name="size">이펙트 최대 생성 개수(생략 가능, 기본값 : 100)</param>
    public void AddEffect(GameObject prefab, int size = 100)
    {
        // 프리팹이 없다면
        if (prefab == null)
        {
            Debug.Log($"[Error | Effect] 이펙트 추가 실패 => 입력 - 이펙트 프리팹 : 없음");
            return;
        }
        // 이펙트 오브젝트 풀이 있다면
        else if (effectPools.ContainsKey(prefab))
            return;

        // 이펙트 컨테이너 생성
        Transform container = new GameObject($"{prefab.name}_그룹").transform;
        // 이펙트 오브젝트 풀 생성 및 딕셔너리에 저장
        effectPools[prefab] = CustomObjectPool.CreatePool(prefab, size, container);
    }

    /// <summary>
    /// 이펙트들 추가 함수
    /// </summary>
    /// <param name="prefabs">추가할 이펙트 프리팹들</param>
    /// <param name="size">이펙트 최대 생성 개수(생략 가능, 기본값 : 100)</param>
    public void AddEffects(List<GameObject> prefabs, int size = 100)
    {
        // 리스트가 없거나, 비어있다면
        if(prefabs == null || prefabs.Count == 0)
        {
            Debug.Log($"[Error | Effect] 이펙트 추가 실패 => 입력 - 이펙트 프리팹들 : 없음");
            return;
        }

        // 프리팹들의 수만큼
        foreach (var prefab in prefabs)
            // 이펙트 추가
            AddEffect(prefab, size);
    }

    /// <summary>
    /// 이펙트 실행 함수
    /// </summary>
    /// <param name="prefab">이펙트 프리팹</param>
    /// <param name="pos">위치</param>
    /// <param name="rot">각도</param>
    /// <param name="duration">지속 시간</param>
    /// <param name="parent">따라다닐 대상</param>
    public void PlayEffect(GameObject prefab, Vector3 pos, Quaternion rot,
                                float? duration = null, Transform parent = null)
    {
        // 실행할 이펙트가 없다면
        if (prefab == null)
            return;

        // 이펙트 오브젝트 풀이 없다면
        if (!effectPools.TryGetValue(prefab, out var effectPool))
        {
            // 이펙트 추가
            AddEffect(prefab);
            // 생성한 이펙트 오브젝트 풀 주소 지정
            effectPool = effectPools[prefab];
        }

        // 이펙트 가져오기
        var effect = GetEffect(effectPool);

        // 실행할 이펙트가 없다면
        if (effect == null)
            return;

        // 실행자 인터페이스가 없다면
        if (!effect.TryGetComponent<IEffectExecuter>(out var executer))
        {
            Debug.Log($"[Error | Effect] 이펙트 실행 실패 => 입력 - 이펙트 실행자 : 없음", effect.gameObject);
            return;
        }

        // 이펙트의 위치와 각도 설정
        effect.transform.SetPositionAndRotation(pos, rot);

        // 따라다닐 대상이 있다면
        if (parent != null)
            // 따라다닐 대상 설정(위치, 각도 유지)
            effect.transform.SetParent(parent, true);

        // 이펙트 지속 시간이 있다면
        if (duration != null)
            // 지속 시간 후 자동으로 꺼지는 이펙트 실행
            executer.ExecuteEffect((float)duration);
        // 이펙트 지속 시간이 없다면
        else
            // 꺼지지 않는 이펙트 실행
            executer.ExecuteEffect();
    }

    /// <summary>
    /// 이펙트 가져오는 함수
    /// </summary>
    /// <param name="effectPool">이펙트 오브젝트 풀 주소</param>
    private Effect GetEffect(IObjectPool<GameObject> effectPool)
    {
        // 이펙트 오브젝트 풀에서 받아오기
        var prefab = effectPool.Get();

        // 이펙트 프리팹이 없다면
        if (prefab == null)
        {
            Debug.Log($"[Error | Effect] 이펙트 가져오기 실패 => 입력 - 이펙트 프리팹 : 없음");
            return null;
        }
        // 이펙트 스크립트가 없다면
        else if (!prefab.TryGetComponent<Effect>(out var effect))
        {
            Debug.Log($"[Error | Effect] 이펙트 가져오기 실패 => 입력 - 이펙트 스크립트 : 없음", prefab);
            return null;
        }
        // 이펙트 스크립트가 있다면
        else
        {
            // 반납 주소 설정
            effect.SetPoolRef(effectPool);
            return effect;
        }
    }

    /// <summary>
    /// 구조체로 하는 이펙트 실행 함수
    /// </summary>
    /// <param name="data">실행할 이펙트 정보 구조체</param>
    public void PlayEffect(EffectPlayData data)
        => PlayEffect(data.prefab, data.position, data.rotation, data.duration, data.parent);

    /// <summary>
    /// 이펙트들 실행 함수
    /// </summary>
    /// <param name="datas">실행할 이펙트 정보 구조체들</param>
    public void PlayEffects(List<EffectPlayData> datas)
    {
        // 리스트가 없거나, 비어있다면
        if (datas == null || datas.Count == 0)
            return;

        // 이펙트들의 수만큼
        for (int i = 0; i < datas.Count; i++)
            // 이펙트 실행
            PlayEffect(datas[i]);
    }

    /*
    // 종료 함수를 만들려면 Play시 Effect를 반환하게 만들어야 함
    /// <summary>
    /// 이펙트 종료 함수
    /// </summary>
    private void StopEffect(GameObject prefab)
    {
        // 종료할 이펙트가 없다면
        if (!effectPools.TryGetValue(prefab, out var effectPool))
            return;

        // 실행자 인터페이스가 없다면
        if (!effect.prefab.TryGetComponent<IEffectExecuter>(out var executer))
        {
            Debug.Log($"[Effect] 이펙트 종료 실패 => " +
                        $"입력 - 이펙트 종류 : {effect.type.ToKoreanString()} / " +
                        $"이펙트 실행자 : 없음", effect.prefab);
            continue;
        }
    }

    /// <summary>
    /// 이펙트 초기화 함수
    /// </summary>
    private void ResetEffects(ResetActiveSkillEffect reset)
    {
        // 초기화할 이펙트가 없다면
        if (!effectPools.TryGetValue(reset.id, out var skillEffect))
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