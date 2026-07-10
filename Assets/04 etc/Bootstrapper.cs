using UnityEngine;
using System.Linq;
using System;

//각자 스크립트 초기화 순서 정해줄 때 사용
public enum InitOrder
{
    //PlayerUIBridge = -10,
    //Player = 0,
    //Skill = 100,
    //Mob = 200,
    //Boss = 300,
    //UI = 400

    UI = -20,              // 화면 참조 등록, UI 초기화·Reset 완료
    PlayerUIBridge = -10,  // Player 이벤트 → UI 이벤트 구독
    Player = 0,            // 실제 HP·목숨·경험치 초기화 및 이벤트 발행
    Skill = 100,
    Mob = 200,
    Boss = 300
}

public class Bootstrapper : MonoBehaviour
{
    private void Awake()
    {
        //인스펙터로 참조하지 말고 전체적으로 1회 찾아오기
        var managers = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None)
            .OfType<IInitializable>()
            .OrderBy(m => m.Priority)
            .ToArray();

        //Priority로 정렬된 managers순서대로 초기화 및 ServiceLocator 등록
        foreach(var manager in managers)
        {
            //GetType()은 IInitializable같은 껍데기 타입이 아닌 알맹이 진짜 타입을 반환함
            Type concreteType = manager.GetType();
            ServiceLocator.Register(concreteType, manager); //먼저 등록해줘야 Init()에서 사용 가능
            manager.Init();
            //인터페이스로도 호출할 수 있도록 등록해주기
            foreach(var _interface in concreteType.GetInterfaces())
            {
                if (_interface != typeof(IInitializable))
                    ServiceLocator.Register(_interface, manager);
            }
        }
    }
}
