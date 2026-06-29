using UnityEngine;
using System.Linq;
using System;

//각자 스크립트 초기화 순서 정해줄 때 사용
public enum InitOrder
{
    System = 0,
    Player = 100,
    Skill = 200,
    Mob = 300,
    Boss = 400,
    UI = 500,
}

public class Bootstrapper_Y : MonoBehaviour
{
    private void Awake()
    {
        //인스펙터로 참조하지 말고 전체적으로 1회 찾아오기
        var managers = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None)
            .OfType<IInitializable>()
            .OrderBy(m => m.Priority);

        //Priority로 정렬된 managers순서대로 초기화 및 ServiceLocator 등록
        foreach(var manager in managers)
        {
            //GetType()은 IInitializable같은 껍데기 타입이 아닌 알맹이 진짜 타입을 반환함
            Type concreteType = manager.GetType();
            ServiceLocator_Y.Register(concreteType, manager); //먼저 등록해줘야 Init()에서 사용 가능
            manager.Init();
            //인터페이스로도 호출할 수 있도록 등록해주기
            foreach(var _interface in concreteType.GetInterfaces())
            {
                if (_interface == typeof(IBossLogics))
                    ServiceLocator_Y.Register(_interface, manager);
            }
        }
    }
}
