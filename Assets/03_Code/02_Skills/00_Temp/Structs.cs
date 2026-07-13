//using System.Collections.Generic;
//using UnityEngine;

///// <summary>
///// 추가할 이펙트 정보
///// </summary>
//public readonly struct EffectAddData
//{
//    public readonly GameObject prefab;                              // 이펙트 프리팹
//    public readonly int size;                                       // 이펙트 최대 생성 개수

//    /// <summary>
//    /// 추가할 이펙트 정보 생성자
//    /// </summary>
//    /// <param name="prefab">추가할 이펙트 프리팹</param>
//    /// <param name="size">이펙트 최대 생성 개수(생략 가능, 기본값 : 100)</param>
//    public EffectAddData(GameObject prefab, int size = 100)
//    {
//        this.prefab = prefab;
//        this.size = size;
//    }
//}

///// <summary>
///// 추가할 이펙트 정보들
///// </summary>
//public readonly struct EffectAddDatas
//{
//    public readonly List<EffectAddData> datas;                      // 추가할 이펙트 정보들

//    /// <summary>
//    /// 추가할 이펙트 정보들 생성자
//    /// </summary>
//    /// <param name="datas">추가할 이펙트 정보들</param>
//    public EffectAddDatas(List<EffectAddData> datas) => this.datas = datas;
//}

///// <summary>
///// 실행할 이펙트 정보
///// </summary>
//public readonly struct EffectPlayData
//{
//    public readonly GameObject prefab;                              // 이펙트 프리팹
//    public readonly Vector3 position;                               // 이펙트 위치
//    public readonly Quaternion rotation;                            // 이펙트 각도
//    public readonly float? duration;                                // 이펙트 지속 시간
//    public readonly Transform parent;                               // 따라다닐 대상

//    /// <summary>
//    /// 실행할 이펙트 정보 생성자
//    /// </summary>
//    /// <param name="prefab">실행할 이펙트 프리팹</param>
//    /// <param name="worldPosition">실행할 이펙트의 월드 좌표(World Position)<br/>
//    /// ※ 따라다닐 대상(parent)의 상대 좌표(Local Position)로 넣지 말것 ※</param>
//    /// <param name="worldRotation">실행할 이펙트의 월드 각도(World Rotation)<br/>
//    /// ※ 따라다닐 대상(parent)의 상대 각도(Local Rotation)로 넣지 말것 ※</param>
//    /// <param name="duration">이펙트 지속 시간(생략 가능, 기본값 : 무한 or 이펙트 재생 시간)</param>
//    /// <param name="parent">따라다닐 대상(생략 가능, 기본값 : 없음)</param>
//    public EffectPlayData(GameObject prefab, Vector3 worldPosition, Quaternion worldRotation,
//                                                float? duration = null, Transform parent = null)
//    {
//        this.prefab = prefab;
//        position = worldPosition;
//        rotation = worldRotation;
//        this.duration = duration;
//        this.parent = parent;
//    }
//}

///// <summary>
///// 종료할 이펙트 정보
///// </summary>
//public readonly struct EffectStopData
//{
//    public readonly Effect effect;                                  // 종료할 이펙트
//    public readonly bool immediately;                               // 즉시 종료 여부

//    /// <summary>
//    /// 종료할 이펙트 정보 생성자
//    /// </summary>
//    /// <param name="effect">종료할 이펙트</param>
//    /// <param name="immediately">즉시 종료 여부(생략 가능, 기본값 : 즉시 종료 안함)</param>
//    public EffectStopData(Effect effect, bool immediately = false)
//    {
//        this.effect = effect;
//        this.immediately = immediately;
//    }
//}

///// <summary>
///// 초기화할 이펙트 정보
///// </summary>
//public readonly struct EffectResetData
//{
//    public readonly Effect effect;                                  // 초기화할 이펙트

//    /// <summary>
//    /// 초기화할 이펙트 정보 생성자
//    /// </summary>
//    /// <param name="effect">초기화할 이펙트</param>
//    public EffectResetData(Effect effect) => this.effect = effect;
//}