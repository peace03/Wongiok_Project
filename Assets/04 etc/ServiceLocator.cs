using UnityEngine;
using System.Collections.Generic;
using System;

// 각 스크립트 Init()에서 서로 참조가 필요할 경우 사용하는 간단한 전역 서비스 저장소입니다.
// Bootstrapper가 매니저를 초기화한 뒤 실제 타입을 키로 등록합니다.
public static class ServiceLocator
{
    // Type을 키로, 실제 매니저 인스턴스를 값으로 저장합니다.
    private static readonly Dictionary<Type, object> _services = new Dictionary<Type, object>();

    // 특정 타입의 서비스를 등록합니다.
    // 같은 타입이 이미 등록되어 있으면 기존 값을 유지합니다.
    public static void Register(Type type, object service)
    {
        if (!_services.ContainsKey(type))
            _services.Add(type, service);
    }

    // 등록된 서비스를 타입으로 가져옵니다.
    // Bootstrapper의 현재 순서상 아직 Register되지 않은 매니저는 여기서 찾을 수 없습니다.
    public static T Get<T>()
    {
        Type type = typeof(T);
        if (_services.TryGetValue(type, out object value))
            return (T)value;
        else Debug.Log($"ServiceLocator: {type}키를 찾을 수 없음");
        return default;
    }
}
