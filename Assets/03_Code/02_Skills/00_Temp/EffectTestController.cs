using System.Collections.Generic;
using UnityEngine;

public class EffectTestController : MonoBehaviour
{
    [SerializeField] private List<GameObject> effectPrefabs;        // 이펙트 프리팹들
    [SerializeField] private GameObject effectPrefab;               // 이펙트 프리팹
    [SerializeField] private Transform target;                      // 따라다닐 대상

    private List<EffectAddData> effectAddDatas = new();             // 추가할 이펙트 정보 리스트
    private List<EffectPlayData> effectPlayDatas = new();           // 실행할 이펙트 정보 리스트
    private List<EffectStopData> effectStopDatas = new();           // 종료할 이펙트 정보 리스트
    private List<EffectResetData> effectResetDatas = new();         // 초기화할 이펙트 정보 리스트

    private List<Effect> playedEffects = new();                     // 실행한 이펙트들
    private Effect playedEffect = null;                             // 실행한 이펙트

    private void Awake()
    {
        #region 싱글톤 방법
        #region 이펙트 추가
        // 1 - 1. 이펙트 추가
        if(effectPrefab != null)
            EffectManager.Instance.AddEffect(effectPrefab);     // 이펙트 프리팹의 오브젝트 풀링 생성
        //EffectManager.Instance.AddEffect(effectPrefab, 200);     // 이펙트 최대 생성 개수 지정 가능

        // 1 - 2. 이펙트들 추가
        // 저는 이걸 따로 변수 선언을 해놨는데, 가끔 쓰시면 여기서 선언하셔도 상관은 없어요.
        effectAddDatas.Clear();      // 추가할 이펙트 정보 리스트 초기화

        for (int i = 0; i < effectPrefabs.Count; i++)      // foreach여도 상관없음
            // 이펙트 프리팹이 있다면
            if (effectPrefabs[i] != null)
                // 추가할 이펙트 정보 리스트에 추가
                effectAddDatas.Add(new EffectAddData(effectPrefabs[i]));
                // 이펙트 최대 생성 개수 지정 가능
                //effectAddDatas.Add(new EffectAddData(effectPrefab, 200));

        EffectManager.Instance.AddEffects(effectAddDatas);       // 이펙트 프리팹마다 오브젝트 풀링 생성
        #endregion
        #region 이펙트 실행
        // 2 - 1. 이펙트 실행
        // 이펙트를 실행할 때는 -> 실행할 이펙트 프리팹, 실행할 위치, 실행할 각도를 넣으면 되고
        // 추가로             -> 지속시간, 이펙트가 따라다녀야 되는 오브젝트 설정(= 부모 설정)을 할 수 있음
        //                      └ 지속시간을 넣으면, 지속시간 후에 자동으로 종료됨(3번 참고)
        // 싱글톤은 실행한 이펙트 객체를 받아올 수 있음
        if (effectPrefab != null)
            playedEffect = EffectManager.Instance.PlayEffect(effectPrefab, Vector3.zero, Quaternion.identity);
            //EffectManager.Instance.PlayEffect(effectPrefab, Vector3.zero, Quaternion.identity, 3f, target);
            //EffectManager.Instance.PlayEffect(effectPrefab, Vector3.zero, Quaternion.identity, 3f);
            //EffectManager.Instance.PlayEffect(effectPrefab, Vector3.zero, Quaternion.identity, parent : target);

        // 2 - 2. 이펙트들 실행
        effectPlayDatas.Clear();        // 실행할 이펙트 정보 리스트 초기화

        foreach(var prefab in effectPrefabs)
            // 이펙트 프리팹이 있다면
            if (prefab != null)
                // 실행할 이펙트 정보 리스트에 추가
                effectPlayDatas.Add(new EffectPlayData(prefab, Vector3.zero, Quaternion.identity));

        // 실행할 이펙트 정보 리스트에 대한 결과가 playedEffects에 담겨짐
        EffectManager.Instance.PlayEffects(effectPlayDatas, playedEffects);
        #endregion
        #region 이펙트 종료
        // 3 - 1. 이펙트 종료
        // 이펙트가 있고 이펙트가 활성화 되어있다면
        if (playedEffect != null && playedEffect.gameObject.activeSelf)
        {
            // 받아온 이펙트 객체를 통해 직접 이펙트 종료를 할 수도 있고
            playedEffect.StopEffect();
            // ※ 이펙트가 종료되면 오브젝트 풀링 주소로 반납되기 때문에 초기화 필수 ※
            // 싱글톤 패턴으로 사용하고, 초기화하는 걸 자꾸 까먹으실 것 같으시면 얘기해주세요~
            // 이펙트 매니저로 이펙트 종료를 요청하면 알아서 이펙트 변수 초기화 하는 것까지 넣어드릴게요(ref)
            playedEffect = null;
        }

        // 이펙트 객체를 넘겨서 이펙트 종료를 요청할 수도 있음
        //EffectManager.Instance.StopEffect(playedEffect);
        //playedEffect = null;

        // 하지만, 파티클 시스템 특징 : 화면에서 이펙트가 바로 사라지는 것이 아닌 새로운 이펙트 입자를 생성하지 않는 것!
        // 화면에서 바로 이펙트가 사라지길 원한다면, bool 값을 true로 설정해서 끄거나 4번을 참고하기!
        //playedEffect.StopEffect(true);
        //EffectManager.Instance.StopEffect(playedEffect, true);

        // 3 - 2. 이펙트들 종료
        effectStopDatas.Clear();        // 종료할 이펙트 정보 리스트 초기화

        // 실행한 이펙트들의 수만큼
        foreach (var effect in playedEffects)
            // 이펙트가 있고 활성화 되어있다면
            if (effect != null && effect.gameObject.activeSelf)
            {
                // 직접 이펙트를 종료할 수도 있고
                //effect.StopEffect();
                // 종료할 이펙트 정보 리스트에 추가해서
                effectStopDatas.Add(new EffectStopData(effect));
            }

        // 이펙트 매니저한테 이펙트 종료를 요청할 수도 있음
        EffectManager.Instance.StopEffects(effectStopDatas);
        // 실행한 이펙트 리스트 초기화
        playedEffects.Clear();
        // 이것도 위와 마찬가지로 즉시 종료는 bool 값을 true로 설정해야 함
        #endregion
        #region 이펙트 초기화
        // 4 - 1. 이펙트 초기화
        // ※ 위에서 실행한 이펙트를 종료하지 않았다는 가정 ※
        // 실제로 동작하는 걸 보실려면 이펙트 종료 부분을 주석 처리하시거나,
        // 이펙트를 다시 실행하는 코드를 종료 후에 넣어주세요!

        // 초기화란? -> 실행 중인 이펙트를 멈추고, 화면을 깨끗하게 만듭니다.
        //             그래서 갑자기 연출이 끊기는 기분이 들 수도 있습니다~
        //             하지만, 특정 상황(플레이어 죽음 등)에서는 사용해야 될 수도 있다고 생각해서 만들었어요!
        // !주의사항! ※ 오브젝트 풀링 주소로 반납하지 않습니다 ※ -> 즉, 활성화 되어있는 상태니, 주의하세요!

        // 이펙트가 있고 이펙트가 활성화 되어있다면
        if (playedEffect != null && playedEffect.gameObject.activeSelf)
            // 이펙트 변수로 직접 초기화를 할 수 있고
            playedEffect.ResetEffect();

        // 이펙트 매니저한테 초기화를 요청할 수 있습니다
        //EffectManager.Instance.ResetEffect(playedEffect);

        // 4 - 2. 이펙트들 초기화
        effectResetDatas.Clear();       // 초기화할 이펙트 정보 리스트 초기화

        // 실행한 이펙트들의 수만큼
        foreach (var effect in playedEffects)
            // 이펙트가 있고 이펙트가 활성화 되어있다면
            if (effect != null && effect.gameObject.activeSelf)
            {
                // 직접 초기화할 수도 있고
                //effect.ResetEffect();
                // 초기화할 이펙트 정보 리스트에 추가해서
                effectResetDatas.Add(new EffectResetData(effect));
            }

        // 이펙트 매니저한테 초기화를 요청할 수도 있음
        EffectManager.Instance.ResetEffects(effectResetDatas);

        // 싱글톤 방법 끝
        #endregion
        #endregion
        #region 이벤트 버스 방법
        #region 이펙트 추가
        // 1 - 1. 이펙트 추가
        if (effectPrefab != null)
            // 이펙트 프리팹의 오브젝트 풀링 생성
            EventBus<EffectAddData>.Publish(new EffectAddData(effectPrefab));
            // 이펙트 최대 생성 개수 지정 가능
            //EventBus<EffectAddData>.Publish(new EffectAddData(effectPrefab, 200));

        // 1 - 2. 이펙트들 추가
        // 저는 이걸 따로 변수 선언을 해놨는데, 가끔 쓰시면 여기서 선언하셔도 상관은 없어요.
        effectAddDatas.Clear();      // 추가할 이펙트 정보 리스트 초기화

        for (int i = 0; i < effectPrefabs.Count; i++)      // foreach여도 상관없음
            // 이펙트 프리팹이 있다면
            if (effectPrefabs[i] != null)
                // 추가할 이펙트 정보 리스트에 추가
                effectAddDatas.Add(new EffectAddData(effectPrefabs[i]));
                // 이펙트 최대 생성 개수 지정 가능
                //effectAddDatas.Add(new EffectAddData(effectPrefab, 200));

        // 이펙트 프리팹마다 오브젝트 풀링 생성
        EventBus<EffectAddDatas>.Publish(new EffectAddDatas(effectAddDatas));
        #endregion
        #region 이펙트 실행
        // 2. 이펙트 실행
        // 이펙트를 실행할 때는 -> 실행할 이펙트 프리팹, 실행할 위치, 실행할 각도를 넣으면 되고
        // 추가로             -> 지속시간, 이펙트가 따라다녀야 되는 오브젝트 설정(= 부모 설정)을 할 수 있음
        //                      └ 지속시간을 넣으면, 지속시간 후에 자동으로 종료됨(3번 참고)
        // 이벤트 버스는 지속시간을 생략하면 이펙트의 재생 시간만큼 실행되고 자동으로 종료됨
        if (effectPrefab != null)
            EventBus<EffectPlayData>.Publish(new EffectPlayData(effectPrefab, Vector3.zero,
                                                                                    Quaternion.identity));
            //EventBus<EffectPlayData>.Publish(new EffectPlayData(effectPrefab, Vector3.zero,
            //                                                          Quaternion.identity, 3f, target));
            //EventBus<EffectPlayData>.Publish(new EffectPlayData(effectPrefab, Vector3.zero,
            //                                                                  Quaternion.identity, 3f));
            //EventBus<EffectPlayData>.Publish(new EffectPlayData(effectPrefab, Vector3.zero,
            //                                                      Quaternion.identity, parent : target));

        // 이벤트 버스 방법 끝
        #endregion
        #endregion
    }
}