using UnityEngine;
using UnityEngine.Pool;
using System.Collections.Generic;

public class EffectManager : MonoBehaviour
{
    public static EffectManager Instance { get; private set; }        // 싱글톤 인스턴스

    // 모든 이펙트 오브젝트 풀 딕셔너리
    private readonly Dictionary<GameObject, IObjectPool<GameObject>> effectPools = new();

    private void OnEnable()
    {
        // 이펙트 추가 이벤트 구독
        EventBus<EffectAddData>.action += AddEffect;
        EventBus<EffectAddDatas>.action += AddEffects;
        // 이펙트 실행 이벤트 구독
        EventBus<EffectPlayData>.action += OnPlayEffectEvent;
    }

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(this);
    }

    private void OnDisable()
    {
        // 이펙트 추가 이벤트 구독 해제
        EventBus<EffectAddData>.action -= AddEffect;
        EventBus<EffectAddDatas>.action -= AddEffects;
        // 이펙트 실행 이벤트 구독 해제
        EventBus<EffectPlayData>.action -= OnPlayEffectEvent;
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
            return;
        // 이펙트 오브젝트 풀이 있다면
        else if (effectPools.ContainsKey(prefab))
            return;

        // 이펙트 컨테이너 생성
        Transform container = new GameObject($"{prefab.name}_그룹").transform;
        // 이펙트 오브젝트 풀 생성 및 딕셔너리에 저장
        effectPools[prefab] = CustomObjectPool.CreatePool(prefab, size, container);
    }

    /// <summary>
    /// [구조체 | 이벤트] 이펙트 추가 함수
    /// </summary>
    /// <param name="data">추가할 이펙트 정보 구조체</param>
    public void AddEffect(EffectAddData data) => AddEffect(data.prefab, data.size);

    /// <summary>
    /// [구조체] 이펙트들 추가 함수
    /// </summary>
    /// <param name="datas">추가할 이펙트 정보 구조체들</param>
    public void AddEffects(List<EffectAddData> datas)
    {
        // 리스트가 없거나, 비어있다면
        if (datas == null || datas.Count == 0)
            return;

        // 이펙트들의 수만큼
        for (int i = 0; i < datas.Count; i++)
            // 이펙트 추가
            AddEffect(datas[i]);
    }

    /// <summary>
    /// [구조체 | 이벤트] 이펙트들 추가 함수
    /// </summary>
    /// <param name="eventData">추가할 이펙트 정보들 구조체</param>
    public void AddEffects(EffectAddDatas eventData) => AddEffects(eventData.datas);

    /// <summary>
    /// 이펙트 실행 후 실행한 이펙트 반환하는 함수
    /// </summary>
    /// <param name="prefab">이펙트 프리팹</param>
    /// <param name="pos">위치</param>
    /// <param name="rot">각도</param>
    /// <param name="duration">지속 시간</param>
    /// <param name="parent">따라다닐 대상</param>
    public Effect PlayEffect(GameObject prefab, Vector3 pos, Quaternion rot,
                                float? duration = null, Transform parent = null)
    {
        // 실행할 이펙트가 없다면
        if (prefab == null)
            return null;

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
            return null;

        // 실행기 인터페이스가 없다면
        if (!effect.TryGetComponent<IEffectExecuter>(out var executer))
        {
            Debug.Log($"[Error | Effect] 이펙트 실행 실패 => 입력 - 이펙트 실행기 : 없음", effect.gameObject);
            return null;
        }

        // 이펙트의 로컬 위치와 각도 설정
        effect.transform.SetLocalPositionAndRotation(pos, rot);

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

        // 실행한 이펙트 반환
        return effect;
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
    /// [구조체] 이펙트 실행 후 실행한 이펙트 반환하는 함수
    /// </summary>
    /// <param name="data">실행할 이펙트 정보 구조체</param>
    public Effect PlayEffect(EffectPlayData data)
        => PlayEffect(data.prefab, data.position, data.rotation, data.duration, data.parent);

    /// <summary>
    /// [이벤트] 이펙트 실행 함수
    /// </summary>
    /// <param name="data">실행할 이펙트 정보 구조체</param>
    private void OnPlayEffectEvent(EffectPlayData data)
    {
        // 이펙트 실행 후 받아오기
        var effect = PlayEffect(data.prefab, data.position, data.rotation, data.duration, data.parent);

        // 이펙트가 비어있거나, 지속시간이 비어있지 않다면
        if (effect == null || data.duration != null)
            return;

        // 실행기 인터페이스가 없다면
        if(!effect.TryGetComponent<IEffectExecuter>(out var executer))
            return;

        // 최대 이펙트 시간 후 자동으로 꺼지는 이펙트 실행으로 변경
        executer.ExecuteEffect(effect.MaxEffectTime);
    }

    /// <summary>
    /// [구조체] 이펙트들 실행 후 실행한 이펙트를 리스트에 저장하는 함수
    /// </summary>
    /// <param name="datas">실행할 이펙트 정보 구조체들</param>
    /// <param name="results">실행할 이펙트들을 담을 리스트</param>
    public void PlayEffects(List<EffectPlayData> datas, List<Effect> results)
    {
        // 리스트가 없거나, 비어있다면
        if (datas == null || datas.Count == 0)
            return;

        // 리스트 초기화
        results.Clear();

        // 이펙트들의 수만큼
        for (int i = 0; i < datas.Count; i++)
            // 이펙트 실행 후 결과 리스트에 추가
            results.Add(PlayEffect(datas[i]));
    }

    /// <summary>
    /// 이펙트 종료 함수
    /// </summary>
    /// <param name="effect">종료할 이펙트</param>
    /// <param name="immediately">즉시 종료 여부(생략 가능, 기본값 : 즉시 종료 안함)</param>
    public void StopEffect(Effect effect, bool immediately = false)
    {
        // 종료할 이펙트가 없다면
        if (effect == null)
            return;

        // 이펙트 종료
        effect.StopEffect(immediately);
    }

    /// <summary>
    /// [구조체] 이펙트 종료 함수
    /// </summary>
    /// <param name="data">종료할 이펙트 정보 구조체</param>
    public void StopEffect(EffectStopData data) => StopEffect(data.effect, data.immediately);

    /// <summary>
    /// [구조체] 이펙트들 종료 함수
    /// </summary>
    /// <param name="datas">종료할 이펙트 정보 구조체들</param>
    public void StopEffects(List<EffectStopData> datas)
    {
        // 리스트가 없거나, 비어있다면
        if (datas == null || datas.Count == 0)
            return;

        // 이펙트들의 수만큼
        for (int i = 0; i < datas.Count; i++)
            // 이펙트 종료
            StopEffect(datas[i]);
    }

    /// <summary>
    /// 이펙트 초기화 함수
    /// </summary>
    /// <param name="effect">초기화할 이펙트</param>
    public void ResetEffect(Effect effect)
    {
        // 초기화할 이펙트가 없다면
        if (effect == null)
            return;

        // 이펙트 초기화
        effect.ResetEffect();
    }

    /// <summary>
    /// [구조체] 이펙트 초기화 함수
    /// </summary>
    /// <param name="data">초기화할 이펙트 정보 구조체</param>
    public void ResetEffect(EffectResetData data) => ResetEffect(data.effect);

    /// <summary>
    /// [구조체] 이펙트들 초기화 함수
    /// </summary>
    /// <param name="datas">초기화할 이펙트 정보 구조체들</param>
    public void ResetEffects(List<EffectResetData> datas)
    {
        // 리스트가 없거나, 비어있다면
        if (datas == null || datas.Count == 0)
            return;

        // 이펙트들의 수만큼
        for (int i = 0; i < datas.Count; i++)
            // 이펙트 초기화
            ResetEffect(datas[i]);
    }
}