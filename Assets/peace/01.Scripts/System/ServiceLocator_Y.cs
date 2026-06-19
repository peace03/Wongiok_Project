using UnityEngine;
using System.Collections.Generic;
using System;

//각 스크립트 Init()에서 서로 참조가 필요할 경우 사용
public static class ServiceLocator_Y
{
    private static readonly Dictionary<Type, object> _services = new Dictionary<Type, object>();

    public static void Register(Type type, object service)
    {
        if (!_services.ContainsKey(type))
            _services.Add(type, service);
    }

    public static T Get<T>()
    {
        Type type = typeof(T);
        if (_services.TryGetValue(type, out object value))
            return (T)value;
        else Debug.Log($"ServiceLocator: {type}키를 찾을 수 없음");
        return default;
    }
}
